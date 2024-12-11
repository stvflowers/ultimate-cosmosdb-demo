using Microsoft.Azure.Cosmos;
using Azure.Identity;
using ultimate_cosmosdb_demo.Models;

namespace ultimate_cosmosdb_demo.Services;

public class CosmosService
{
    public static CosmosClient GetCosmosClient(string cosmosEndpoint, string? entraTenantId)
    {
        
        DefaultAzureCredentialOptions credOptions = new()
        {
            ExcludeVisualStudioCodeCredential = true,
            ExcludeVisualStudioCredential = true,
            ExcludeEnvironmentCredential = true,
        };

        DefaultAzureCredential cred = new(credOptions);

        // Use InteractiveBrowser credential if chain of resolution
        // for Default credential is failing and you don't want to troubleshoot.
        
        // InteractiveBrowserCredentialOptions credOpt = new()
        // {
        //     TenantId = entraTenantId
        // };

        // InteractiveBrowserCredential cred = new(credOpt);
        
        CosmosSerializationOptions serializationOptions = new()
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        };


        CosmosClientOptions clientOptions = new()
        {
            SerializerOptions = serializationOptions,
            AllowBulkExecution = true
        };


        // Advanced client options
        //
        // CosmosClientOptions clientOptionsAdvanced = new()
        // {
        //     SerializerOptions = serializationOptions,
        //     AllowBulkExecution = true,
        //     ConnectionMode = ConnectionMode.Gateway,
        //     ConsistencyLevel = ConsistencyLevel.ConsistentPrefix,
        //     ApplicationRegion = Regions.EastUS2,
        //     ApplicationPreferredRegions = ["East US 2", "West US 2", "Central US"]
        // };


        try
        {
            CosmosClient cosmosClient = new(cosmosEndpoint, cred, clientOptions);
            return cosmosClient;

        } catch {
            throw new Exception("An error occurred while creating the CosmosClient.");
        }        
    }
    public static async Task BulkWrite(CosmosClient _client, string? database, string? container, ILogger<Worker> _logger, CancellationToken stoppingToken)
    {
        Database _database = _client.GetDatabase(database);
        Container _container = _database.GetContainer(container);

        IReadOnlyCollection<CosmosPerson> persons = BogusService.BulkGeneratePersons(1000);
        List<Task> tasks = [];

        foreach (CosmosPerson person in persons)
        {
            tasks.Add(_container.CreateItemAsync<CosmosPerson>(person)
                .ContinueWith(ItemResponse =>
                {
                    try
                    {
                        if (ItemResponse.IsCompletedSuccessfully)
                        {
                            _logger.LogInformation($"Created item {ItemResponse.Result.Resource.Id} in database {_database.Id} container {_container.Id}");
                        }
                        else
                        {
                            _logger.LogError($"Failed to create item {ItemResponse.Result.Resource.Id} in database {_database.Id} container {_container.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Exception occurred: {ex.Message}");
                    }
                }));
        }

        await Task.WhenAll(tasks);

    }    
    public static async Task WriteItem(CosmosClient _client, string? database, string? container, ILogger<Worker> _logger, CancellationToken stoppingToken)
    {
        Database _database = _client.GetDatabase(database);
        Container _container = _database.GetContainer(container);

        CosmosPerson person = BogusService.GeneratePerson();

        await _container.CreateItemAsync<CosmosPerson>(person, cancellationToken: stoppingToken)
            .ContinueWith(ItemResponse =>
            {
                try
                {
                    if (ItemResponse.IsCompletedSuccessfully)
                    {
                        _logger.LogInformation($"Created item {ItemResponse.Result.Resource.Id} in database {_database.Id} container {_container.Id}");
                        _logger.LogInformation($"Person: {person.FirstName} {person.LastName}");
                        _logger.LogInformation("HTTP status code: " + ItemResponse.Result.StatusCode);
                        _logger.LogInformation("Operation request charge: " + ItemResponse.Result.RequestCharge);
                    }
                    else
                    {
                        _logger.LogError($"Failed to create item {ItemResponse.Result.Resource.Id} in database {_database.Id} container {_container.Id}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Exception occurred: {ex.Message}");
                }
            }, stoppingToken);            
    }
    public static async Task PatchItemNewFirstName(CosmosClient _client, string? database, string? container, string? id, string? partitionKey, string? newFirstName, ILogger<Worker> _logger, CancellationToken stoppingToken)
    {
        Database _database = _client.GetDatabase(database);
        Container _container = _database.GetContainer(container);      

        try
        {
            ItemResponse<CosmosPerson> response = await _container.ReadItemAsync<CosmosPerson>(id, new PartitionKey(partitionKey));
            
            CosmosPerson personToPatch = response;
            
            await _container.PatchItemAsync<CosmosPerson>(
                id: personToPatch.Id,
                partitionKey: new PartitionKey(personToPatch.Email),
                patchOperations: new[] { PatchOperation.Replace("/firstName", newFirstName) },
                cancellationToken: stoppingToken)
                .ContinueWith(ItemResponse =>
                {
                    _logger.LogInformation($"Upserted item {ItemResponse.Result.Resource.Id} in database {_database.Id} container {_container.Id}");
                    _logger.LogInformation("Operation request charge: " + ItemResponse.Result.RequestCharge);

                }, stoppingToken);
    
        } catch (Exception ex) {
            _logger.LogError($"Exception occurred: {ex.Message}");
        }
}
    public static async Task QueryItems(CosmosClient _client, string? database, string? container, QueryDefinition query, ILogger<Worker> _logger, CancellationToken stoppingToken)
    {
        Database _database = _client.GetDatabase(database);
        Container _container = _database.GetContainer(container);

        try {
            FeedIterator<CosmosPerson> resultSet = _container.GetItemQueryIterator<CosmosPerson>(query);
            while (resultSet.HasMoreResults)
            {
                FeedResponse<CosmosPerson> response = await resultSet.ReadNextAsync();
                _logger.LogInformation("Items returned this iteration: " + response.Count);
                _logger.LogInformation($"Request charge: {response.RequestCharge}");

            }
        } catch (Exception ex)
        {
            _logger.LogError($"Exception occurred: {ex.Message}");
        }
    }
    public static async Task PointReadItem(CosmosClient _client, string? database, string? container, string? id, string? partitionKey, ILogger<Worker> _logger, CancellationToken stoppingToken)
    {
        Database _database = _client.GetDatabase(database);
        Container _container = _database.GetContainer(container);
        
        try
        {
            ItemResponse<CosmosPerson> response = await _container.ReadItemAsync<CosmosPerson>(id, new PartitionKey(partitionKey));
        
            CosmosPerson person = response.Resource;
            
            _logger.LogInformation($"Person: {person.FirstName} {person.LastName}");
        
        } catch (Exception ex) {
            
            _logger.LogError($"Exception occurred: {ex.Message}");
        }
    
    }
    public static async Task DemoHotPartition(CosmosClient _client, string? database, string? container, string? partitionKeyPath, ILogger<Worker> _logger, CancellationToken stoppingToken)
    {
        // For this example, we will recreate a hot partition scenario
        // The container should have at least 30k RUs
        // We will seed the container with an initial 1k items
        // Then write additional items with a skewed partition key value
        
        Database _database = _client.GetDatabase(database);
        Container _container = _database.GetContainer(container);

        IReadOnlyCollection<CosmosPerson> personsHot =  BogusService.BulkGeneratePersons(10000);
        List<Task> tasks = [];

        foreach (CosmosPerson person in personsHot)
        {

            // Overwrite the random PK with a static integer to force a hot partition.
            person.PartitionKey = 3.ToString();

            tasks.Add(_container.CreateItemAsync<CosmosPerson>(person)
                .ContinueWith(ItemResponse =>
                {
                    try
                    {
                        if (ItemResponse.IsCompletedSuccessfully)
                        {
                            _logger.LogInformation($"Created item {ItemResponse.Result.Resource.Id} in database {_database.Id} container {_container.Id}");
                        }
                        else
                        {
                            _logger.LogError($"Failed to create item {ItemResponse.Result.Resource.Id} in database {_database.Id} container {_container.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Exception occurred: {ex.Message}");
                    }
                }));
        }

    }
    public static async Task OptimisticConcurrencyWrite(CosmosClient _client, string? database, string? container, string? id, string? pk, string? newEmail, ILogger<Worker> _logger,  CancellationToken stoppingToken)
    {
        Database _database = _client.GetDatabase(database);
        Container _container = _database.GetContainer(container);

        ItemResponse<CosmosPerson> response = await _container.ReadItemAsync<CosmosPerson>(id, new PartitionKey(pk));
        CosmosPerson personToPatch = response.Resource;

        personToPatch.Email = newEmail;

        // Checks the e-tag on write. If the e-tag has changed, the write will fail. Refetch the document and try again.
        ItemRequestOptions options = new()
        {
            IfMatchEtag = response.ETag
        };

        try {
            await _container.ReplaceItemAsync<CosmosPerson>(personToPatch, personToPatch.Id, new PartitionKey(personToPatch.Email), options);

        } catch (Exception ex)
        {
            _logger.LogError($"Exception occurred: {ex.Message}");
            return;
        }

    }
    public static async Task SetItemTTL(CosmosClient _client, string? database, string? container, string? id, string? pk, int? ttl, ILogger<Worker> _logger, CancellationToken stoppingToken)
    {
        Database _database = _client.GetDatabase(database);
        Container _container = _database.GetContainer(container);

        ItemResponse<CosmosPersonTtl> person = await _container.ReadItemAsync<CosmosPersonTtl>(id, new PartitionKey(pk));
        CosmosPersonTtl p = person.Resource;
        p.TTL = ttl;

        try{
            ItemResponse<CosmosPersonTtl> response = await _container.ReplaceItemAsync<CosmosPersonTtl>(p, p.Id, new PartitionKey(p.Email));
            _logger.LogInformation($"Item to be deleted: {response.Resource.Id} in {ttl} seconds.");
            _logger.LogInformation("Operation request charge: " + response.RequestCharge);
        }
        catch (Exception ex) {
            _logger.LogError($"Exception occurred: {ex.Message}");
        }


    }
}

