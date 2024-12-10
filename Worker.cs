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
                .PageSize(20)
                .MoreChoicesText("[grey](Move up and down to reveal more choices)[/]")
                .AddChoices(new[] {
                    "Run a bulk operation", 
                    "Write a person item",
                    "Query: SELECT * FROM c",
                    "Demo: Hot partition",
                    "Demo: Set Item TTL",
                    "Exit Application\n"
                }));

            string? database = _configuration["cosmosDatabase"];
            string? container = _configuration["cosmosContainer"];

            switch(userSelection)
            {
                case "Run a bulk operation":
                    await CosmosService.BulkWrite(_cosmosClient, database, container, stoppingToken);
                    break;
                case "Write a person item":
                    await CosmosService.WriteItem(_cosmosClient, database, container, stoppingToken);
                    break;
                case "Query: SELECT * FROM c":
                    QueryDefinition selectStar = new("SELECT * FROM c");
                    await CosmosService.QueryItems(_cosmosClient, database, container, selectStar, stoppingToken);
                    break;
                case "Query: SELECT * FROM c WHERE c.userName = 'username'":
                    string userName = "";
                    QueryDefinition noPkQuery = new QueryDefinition("SELECT * FROM c WHERE c.userName = @userName")
                        .WithParameter("@userName", userName);
                    await CosmosService.QueryItems(_cosmosClient, database, container, noPkQuery, stoppingToken);
                    break;
                case "Query: SELECT * FROM c WHERE c.email":
                    string email = "";
                    QueryDefinition pkQuery = new QueryDefinition("SELECT * FROM c WHERE c.email = @email")
                        .WithParameter("@email", email);
                    await CosmosService.QueryItems(_cosmosClient, database, container, pkQuery, stoppingToken);
                    break;
                case "Demo: Hot partition":
                    string hotContainer = "HotPartition";
                    string partitionKey = "/partitionKey";
                    await CosmosService.DemoHotPartition(_cosmosClient, database, hotContainer, partitionKey, stoppingToken);
                    break;
                case "Demo: Set Item TTL":
                    string id = "ea4df80c-3278-4144-8274-d4f753605da6";
                    string pk = "Wilma62@yahoo.com";
                    int ttl = 60;
                    await CosmosService.SetItemTTL(_cosmosClient, database, container, id, pk, ttl, stoppingToken);
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
