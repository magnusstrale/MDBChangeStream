using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;

namespace PS.MongoDB.DataMover;

public class DataMoverService(ILogger<DataMoverService> log)
{
    Dictionary<string, ConcurrentQueue<BsonDocument>> _pendingWrites = [];
    IMongoDatabase _targetDatabase;

    public async Task RunAsync(IMongoClient sourceClient, IMongoClient targetClient, string databaseName, BsonDocument? resumeToken = null)
    {
        var backgroundTask = Task.Run(DoInsertAsync);
        var sourceDatabase = sourceClient.GetDatabase(databaseName);
        _targetDatabase = targetClient.GetDatabase(databaseName);
        var options = new ChangeStreamOptions() { ResumeAfter = resumeToken, FullDocument = ChangeStreamFullDocumentOption.WhenAvailable, BatchSize = 1000 };
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
            .Match(x => x.OperationType == ChangeStreamOperationType.Insert);

        // Start to listen for changes
        using var cursor = await sourceDatabase.WatchAsync(pipeline, options);
        await cursor.ForEachAsync(async change =>
        {
            var collectionName = change.CollectionNamespace.CollectionName;
            if (!_pendingWrites.TryGetValue(collectionName, out var queue))
            {
                queue = new();
                _pendingWrites[collectionName] = queue;
            }
            else if (queue.Count > 10_000) // some configurable threshold
            {
                await Task.Delay(10);
            }
            switch (change.OperationType)
            {
                case ChangeStreamOperationType.Insert:
                    queue.Enqueue(change.FullDocument);
                    break;
                    
                // case ChangeStreamOperationType.Update:
                // case ChangeStreamOperationType.Replace:
                // case ChangeStreamOperationType.Delete:
                default:
                    log.LogInformation("Change stream operation type {OperationType} ignored", change.OperationType);
                    break;
            }
        });
    }

    async Task DoInsertAsync()
    {
        while (true)
        {
            var collections = _pendingWrites.Keys.ToArray();
            var dataWritten = false;
            foreach (var collectionName in collections)
            {
                var queue = _pendingWrites[collectionName];
                if (queue.IsEmpty) continue;
                var collection = _targetDatabase.GetCollection<BsonDocument>(collectionName);
                List<BsonDocument> docs = [];
                while (queue.TryDequeue(out var doc))
                {
                    docs.Add(doc);
                }
                log.LogInformation("Writing {DocCount} documents to collection {CollectionName}", docs.Count, collectionName);
                await collection.InsertManyAsync(docs);
                dataWritten = true;
            }

            if (!dataWritten) await Task.Delay(10);
        }
    }
}