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
                    "Demo: Point read",
                    "Demo: Patch Item",
                    "Demo: Hot partition",
                    "Demo: Set Item TTL",
                    "Demo: Optimistic Concurrency",
                    "Exit Application\n"
                }));

            string? database = _configuration["cosmosDatabase"];
            string? container = _configuration["cosmosContainer"];

            switch(userSelection)
            {
                case "Run a bulk operation":
                    await CosmosService.BulkWrite(_cosmosClient, database, container, _logger, stoppingToken);
                    break;
                case "Write a person item":
                    await CosmosService.WriteItem(_cosmosClient, database, container, _logger, stoppingToken);
                    break;
                case "Query: SELECT * FROM c":
                    QueryDefinition selectStar = new("SELECT * FROM c");
                    await CosmosService.QueryItems(_cosmosClient, database, container, selectStar, _logger, stoppingToken);
                    break;
                case "Query: SELECT * FROM c WHERE c.userName = 'username'":
                    string userName = "";
                    QueryDefinition noPkQuery = new QueryDefinition("SELECT * FROM c WHERE c.userName = @userName")
                        .WithParameter("@userName", userName);
                    await CosmosService.QueryItems(_cosmosClient, database, container, noPkQuery, _logger, stoppingToken);
                    break;
                case "Query: SELECT * FROM c WHERE c.email":
                    string email = "";
                    QueryDefinition pkQuery = new QueryDefinition("SELECT * FROM c WHERE c.email = @email")
                        .WithParameter("@email", email);
                    await CosmosService.QueryItems(_cosmosClient, database, container, pkQuery, _logger, stoppingToken);
                    break;
                case "Demo: Point read":
                    string pointReadId = "ea4df80c-3278-4144-8274-d4f753605da6";
                    string pointReadEmail = "";
                    await CosmosService.PointReadItem(_cosmosClient, database, container, pointReadId, pointReadEmail, _logger, stoppingToken);
                    break;
                case "Demo: Patch Item":
                    // Fill out these variables with values from an example document in your container
                    string patchId = "";
                    string patchEmail = "";
                    string newFirstName = "";
                    await CosmosService.PatchItemNewFirstName(_cosmosClient, database, container, patchId, patchEmail, newFirstName, _logger, stoppingToken);
                    break;
                case "Demo: Hot partition":
                    // Set a new container instead of the one used for the rest of the demo.
                    // Perferably with at least 3 physical partitions.
                    string hotContainer = "HotPartition";
                    string partitionKey = "/partitionKey";
                    await CosmosService.DemoHotPartition(_cosmosClient, database, hotContainer, partitionKey, _logger, stoppingToken);
                    break;
                case "Demo: Set Item TTL":
                    string id = "ea4df80c-3278-4144-8274-d4f753605da6";
                    string pk = "Wilma62@yahoo.com";
                    int ttl = 60;
                    await CosmosService.SetItemTTL(_cosmosClient, database, container, id, pk, ttl, _logger, stoppingToken);
                    break;
                case "Demo: Optimistic Concurrency":
                    string occId = "ea4df80c-3278-4144-8274-d4f753605da6";
                    string occPk = "Wilma62@yahoo.com";
                    string newEmail = "Wilma@yahoo.com";
                    await CosmosService.OptimisticConcurrencyWrite(_cosmosClient, database, container, occId, occPk, newEmail, _logger, stoppingToken);
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
