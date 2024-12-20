using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
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

            
            string? database = _configuration["cosmosDatabase"];
            string? container = _configuration["cosmosContainer"];
            Database _database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(database);
            Container _container = await _database.CreateContainerIfNotExistsAsync(container, "/partitionKey");
            
            QueryDefinition query;
            string? id = null;
            string? partitionKey = null;
            string? userName = null;
            string? newFirstName = null;
            string? newEmail = null;

            
            // Spectre.Console prompt
            var userSelection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What action would you like to perform?")
                .PageSize(20)
                .MoreChoicesText("[grey](Move up and down to reveal more choices)[/]")
                .AddChoices(new[] {
                    "Write a person item",
                    "Run a bulk operation", 
                    "Patch Item",
                    "Item Update with Concurrency Check",
                    "Set Item TTL",
                    "Point read",
                    "Query: SELECT * FROM c",
                    "Query: SELECT * FROM c WHERE c.userName = 'username'",
                    "Query: (cross-partition query) SELECT * FROM c WHERE c.email",
                    "Demo: Hot partition",
                    "Exit Application\n"
                }));


            switch(userSelection)
            {
                case "Run a bulk operation":
                    await CosmosService.BulkWrite(_container, _logger, stoppingToken);
                    break;
                case "Write a person item":
                    await CosmosService.WriteItem(_container, _logger, stoppingToken);
                    break;
                case "Query: SELECT * FROM c":
                    query = new("SELECT * FROM c");
                    await CosmosService.QueryItems(_container, query, _logger, stoppingToken);
                    break;
                case "Query: SELECT * FROM c WHERE c.userName = 'username'":
                    Console.Write("Provide a username to query: ");
                    userName = Console.ReadLine();
                    if(string.IsNullOrEmpty(userName)){
                        _logger.LogError("Username is null");
                    } else {
                        query = new QueryDefinition("SELECT * FROM c WHERE c.userName = @userName")
                            .WithParameter("@userName", userName);
                        await CosmosService.QueryItems(_container, query, _logger, stoppingToken);
                    }
                    break;
                case "Query: (cross-partition query) SELECT * FROM c WHERE c.email":
                    // Non PK Query
                    Console.WriteLine("Provide an email to query: ");
                    string? email = Console.ReadLine();
                    if(string.IsNullOrEmpty(email)){
                        _logger.LogError("Email is null");
                    } else {
                        query = new QueryDefinition("SELECT * FROM c WHERE c.email = @email")
                            .WithParameter("@email", email);
                        await CosmosService.QueryItems(_container, query, _logger, stoppingToken);
                    }
                    break;
                case "Point read":
                    Console.WriteLine("Provide an ID to point read: ");
                    id = Console.ReadLine();
                    Console.WriteLine("Provide a Partition Key to point read: ");
                    partitionKey = Console.ReadLine();
                    if(string.IsNullOrEmpty(id) || string.IsNullOrEmpty(partitionKey)){
                        _logger.LogError("ID or Partition Key is null or empty");
                    } else {
                        await CosmosService.PointReadItem(_container, id, partitionKey, _logger, stoppingToken);
                    }
                    break;
                case "Patch Item":
                    // Fill out these variables with values from an example document in your container
                    Console.WriteLine("Provide an ID to patch: ");
                    id = Console.ReadLine();
                    Console.WriteLine("Provide a Partition Key to patch: ");
                    partitionKey = Console.ReadLine();
                    Console.WriteLine("Provide a new first name: ");
                    newFirstName = Console.ReadLine();
                    await CosmosService.PatchItem(_container, id, partitionKey, newFirstName, _logger, stoppingToken);
                    break;
                case "Demo: Hot partition":
                    // Set a new container instead of the one used for the rest of the demo.
                    // Perferably with at least 3 physical partitions.
                    Console.WriteLine("Provide a Partition Key to demo hot partition: ex. /partitionKey");
                    partitionKey = Console.ReadLine();
                    if(string.IsNullOrEmpty(partitionKey)){
                        _logger.LogError("Partition Key is null or empty");
                    } else {
                        await CosmosService.DemoHotPartition(_container, partitionKey, _logger, stoppingToken);
                    }
                    break;
                case "Set Item TTL":
                    Console.WriteLine("Provide an ID to set TTL: ");
                    id = Console.ReadLine();
                    Console.WriteLine("Provide a Partition Key to set TTL: ");
                    partitionKey = Console.ReadLine();
                    Console.WriteLine("Provide a TTL in seconds: ");
                    string? seconds = Console.ReadLine();
                    int? ttl = null;
                    if (!string.IsNullOrEmpty(seconds))
                    {
                        ttl = int.Parse(seconds);
                    } else {ttl = 60;}
                    await CosmosService.SetItemTTL(_container, id, partitionKey, ttl, _logger, stoppingToken);
                    break;
                case "Item Update with Concurrency Check":
                    Console.WriteLine("Provide an ID to update: ");
                    id = Console.ReadLine();
                    Console.WriteLine("Provide a Partition Key to update: ");
                    partitionKey = Console.ReadLine();
                    Console.WriteLine("Provide a new email: ");
                    newEmail = Console.ReadLine();
                    if(string.IsNullOrEmpty(id) || string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(newEmail)){
                        _logger.LogError("ID, Partition Key, or Email is null or empty");
                    } else {
                        await CosmosService.UpdateWithConcurrencyCheck(_container, id, partitionKey, newEmail, _logger, stoppingToken);
                    }
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
