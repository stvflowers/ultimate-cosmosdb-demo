using Microsoft.Azure.Cosmos;
using Spectre.Console;
using ultimate_cosmosdb_demo.Services;

namespace ultimate_cosmosdb_demo;

public class Worker : BackgroundService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly CosmosClient _cosmosClient;

    // Inject them into the worker class
    public Worker(IHostApplicationLifetime hostApplicationLifetime, ILogger<Worker> logger, IConfiguration configuration, CosmosClient cosmosClient)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
        _configuration = configuration;
        _cosmosClient = cosmosClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            
            // Spectre.Console prompt
            var userSelection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What action would you like to perform?")
                .PageSize(10)
                .AddChoices(new[] {
                    "Run a bulk operation", 
                    "Write a person item",
                    "Query: SELECT * FROM c",
                    "Exit Application\n"
                }));

            switch(userSelection)
            {
                case "Run a bulk operation":
                    await CosmosService.BulkWrite(_cosmosClient, _configuration["cosmosDatabase"], _configuration["cosmosContainer"], stoppingToken);
                    break;
                case "Write a person item":
                    await CosmosService.WriteItem(_cosmosClient, _configuration["cosmosDatabase"], _configuration["cosmosContainer"], stoppingToken);
                    break;
                case "Query: SELECT * FROM c":
                    await CosmosService.QueryItems(_cosmosClient, _configuration["cosmosDatabase"], _configuration["cosmosContainer"], stoppingToken);
                    break;
                case "Exit Application\n":
                    _logger.LogInformation("Shutting down...");
                    _hostApplicationLifetime.StopApplication(); // Trigger application shutdown
                    return;
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}
