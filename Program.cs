using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace PS.MongoDB.DataMover;

public class Program
{
    public static async Task Main(string[] args)
    {
        //
        // Command line handling
        //
        Console.WriteLine("DataMoverService, -? for help");
        if (CommandLineParameter("-?"))
        {
            Console.WriteLine("\nRun DataMover service.");
            return;
        }

        //
        // Configuration and setup preamble
        //
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
        var sourceConfigurationName = config.GetValue<string>($"SourceConfigurationName") ?? "Source";
        var targetConfigurationName = config.GetValue<string>($"TargetConfigurationName") ?? "Target";
        var databaseName = config.GetValue<string>($"DatabaseName") ?? "test";
        using var loggerFactory = LoggerFactory.Create(b =>
        {
            b.AddConfiguration(config);
            b.AddSimpleConsole(f =>
            {
                f.ColorBehavior = LoggerColorBehavior.Enabled;
                f.TimestampFormat = "u ";
                f.UseUtcTimestamp = true;
                f.IncludeScopes = true;
            });
        });
        var log = loggerFactory.CreateLogger<DataMoverService>();

        //
        // Start moving data
        //
        try
        {
            var sourceClient = new ConnectionBuilder(config, sourceConfigurationName, loggerFactory).Client;
            var targetClient = new ConnectionBuilder(config, targetConfigurationName, loggerFactory).Client;

            var mover = new DataMoverService(log);
            await mover.RunAsync(sourceClient, targetClient, databaseName);
        }
        catch (Exception ex)
        {
            log.LogCritical(ex, "Unexpected error");
        }

        bool CommandLineParameter(string name) => args.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
