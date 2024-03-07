using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace PS.MongoDB.DataMover;

public class ConnectionBuilder
{
    readonly MongoClient _client;

    /// <summary>
    /// Constructor for the ConnectionBuilder class
    /// </summary>
    /// <param name="config">An instance of IConfigurationRoot</param>
    /// <param name="configurationName">The name of the connection string to use</param>
    /// <remarks>
    /// It is recommended to set up <paramref name="config"/> with a JSON file as well as environment variables config provider. This allows you to use environment
    /// variables for username / password in order to avoid exposing those in source code / config files. If <paramref name="configurationName"/> is set to "Default"
    /// then the environment variables should be set as follows (example for Linux - note double underscore as separator):
    /// <para>
    /// export Default__Username=mdbuser
    /// </para>
    /// <para>
    /// export Default__Password=mysecretpassword
    /// </para>
    /// </remarks>
    public ConnectionBuilder(
        IConfigurationRoot config,
        string configurationName,
        ILoggerFactory? loggerFactory = null)
        : this(GetSettings(config, configurationName, loggerFactory))
    {
    }

    public ConnectionBuilder(MongoClientSettings settings)
    {
        _client = new(settings);
    }

    public static MongoClientSettings GetSettings(IConfigurationRoot config, string configurationName, ILoggerFactory? loggerFactory = null)
    {
        var connectionString = config.GetConnectionString(configurationName);
        var settings = connectionString == null ? new MongoClientSettings() : MongoClientSettings.FromConnectionString(connectionString);
        var username = config.GetValue<string>($"{configurationName}:Username");
        var password = config.GetValue<string>($"{configurationName}:Password");
        if (username != null && password != null)
        {
            settings.Credential = MongoCredential.CreateCredential("admin", username, password);
        }
        if (loggerFactory != null)
        {
            settings.LoggingSettings = new LoggingSettings(loggerFactory);
        }
        return settings;
    }

    /// <summary>
    /// Get a MongoClient configured with settings for this instance
    /// </summary>
    /// <returns>A MongoClient</returns>
    public MongoClient Client => _client;
}