using Microsoft.Azure.Cosmos;
using Azure.Identity;
using ultimate_cosmosdb_demo.Models;
using Microsoft.Extensions.Logging;

namespace ultimate_cosmosdb_demo.Services;

public class CosmosService
{
    // Client Initialization
    public static CosmosClient GetCosmosClient(string cosmosEndpoint, string? entraTenantId)
    {
        DefaultAzureCredentialOptions credOptions = new()
        {
            ExcludeVisualStudioCodeCredential = true,
            ExcludeVisualStudioCredential = true,
            ExcludeEnvironmentCredential = true,
        };

        DefaultAzureCredential cred = new(credOptions);

        CosmosSerializationOptions serializationOptions = new()
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        };

        CosmosClientOptions clientOptions = new()
        {
            SerializerOptions = serializationOptions,
            AllowBulkExecution = true
        };

        try
        {
            CosmosClient cosmosClient = new(cosmosEndpoint, cred, clientOptions);
            return cosmosClient;
        }
        catch
        {
            throw new Exception("An error occurred while creating the CosmosClient.");
        }
    }

    // Item Operations
    public static async Task WriteItem(Container _container, ILogger<Worker> _logger, CancellationToken stoppingToken)
    {

        CosmosPerson person = BogusService.GeneratePerson();

        await _container.CreateItemAsync<CosmosPerson>(person, cancellationToken: stoppingToken)
            .ContinueWith(ItemResponse =>
            {
                try
                {
                    if (ItemResponse.IsCompletedSuccessfully)
                    {
                        _logger.LogInformation($"Created item {ItemResponse.Result.Resource.Id} in container {_container.Id}");
                        _logger.LogInformation($"Person: {person.FirstName} {person.LastName}");
                        _logger.LogInformation("HTTP status code: " + ItemResponse.Result.StatusCode);
                        _logger.LogInformation("Operation request charge: " + ItemResponse.Result.RequestCharge);
                    }
                    else
                    {
                        _logger.LogError($"Failed to create item {ItemResponse.Result.Resource.Id} in container {_container.Id}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Exception occurred: {ex.Message}");
                }
            }, stoppingToken);
    }

    public static async Task BulkWrite(Container _container, ILogger<Worker> _logger, CancellationToken stoppingToken)
    {

        IReadOnlyCollection<CosmosPerson> persons = BogusService.BulkGeneratePersons(1000);
        List<Task> tasks = new();

        foreach (CosmosPerson person in persons)
        {
            tasks.Add(_container.CreateItemAsync<CosmosPerson>(person)
                .ContinueWith(ItemResponse =>
                {
                    try
                    {
                        if (ItemResponse.IsCompletedSuccessfully)
                        {
                            _logger.LogInformation($"Created item {ItemResponse.Result.Resource.Id} in container {_container.Id}");
                        }
                        else
                        {
                            _logger.LogError($"Failed to create item {ItemResponse.Result.Resource.Id} in container {_container.Id}");
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

    public static async Task PatchItem(Container _container, string? id, string? partitionKey, string? newFirstName, ILogger<Worker> _logger, CancellationToken stoppingToken)
    {

        try
        {
            ItemResponse<CosmosPerson> response = await _container.ReadItemAsync<CosmosPerson>(id, new PartitionKey(partitionKey));
            CosmosPerson personToPatch = response.Resource;

            await _container.PatchItemAsync<CosmosPerson>(
                id: personToPatch.Id,
                partitionKey: new PartitionKey(personToPatch.Email),
                patchOperations: new[] { PatchOperation.Replace("/firstName", newFirstName) },
                cancellationToken: stoppingToken)
                .ContinueWith(ItemResponse =>
                {
                    _logger.LogInformation($"Upserted item {ItemResponse.Result.Resource.Id} in container {_container.Id}");
                    _logger.LogInformation("Operation request charge: " + ItemResponse.Result.RequestCharge);
                }, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception occurred: {ex.Message}");
        }
    }

    public static async Task UpdateWithConcurrencyCheck(Container _container, string? id, string? pk, string? newEmail, ILogger<Worker> _logger, CancellationToken stoppingToken)
    {

        ItemResponse<CosmosPerson> response = await _container.ReadItemAsync<CosmosPerson>(id, new PartitionKey(pk));
        CosmosPerson personToPatch = response.Resource;

        personToPatch.Email = newEmail;

        // Checks the e-tag on write. If the e-tag has changed, the write will fail. Refetch the document and try again.
        ItemRequestOptions options = new()
        {
            IfMatchEtag = response.ETag
        };

        try
        {
            await _container.ReplaceItemAsync<CosmosPerson>(personToPatch, personToPatch.Id, new PartitionKey(personToPatch.Email), options);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception occurred: {ex.Message}");
            return;
        }
    }

    public static async Task SetItemTTL(Container _container, string? id, string? pk, int? ttl, ILogger<Worker> _logger, CancellationToken stoppingToken)
    {

        ItemResponse<CosmosPersonTtl> person = await _container.ReadItemAsync<CosmosPersonTtl>(id, new PartitionKey(pk));
        CosmosPersonTtl p = person.Resource;
        p.TTL = ttl;

        try
        {
            ItemResponse<CosmosPersonTtl> response = await _container.ReplaceItemAsync<CosmosPersonTtl>(p, p.Id, new PartitionKey(p.Email));
            _logger.LogInformation($"Item to be deleted: {response.Resource.Id} in {ttl} seconds.");
            _logger.LogInformation("Operation request charge: " + response.RequestCharge);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception occurred: {ex.Message}");
        }
    }

    public static async Task PointReadItem(Container _container, string? id, string? partitionKey, ILogger<Worker> _logger, CancellationToken stoppingToken)
    {

        try
        {
            ItemResponse<CosmosPerson> response = await _container.ReadItemAsync<CosmosPerson>(id, new PartitionKey(partitionKey));
            CosmosPerson person = response.Resource;

            _logger.LogInformation($"Person: {person.FirstName} {person.LastName}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception occurred: {ex.Message}");
        }
    }

    public static async Task QueryItems(Container _container, QueryDefinition query, ILogger<Worker> _logger, CancellationToken stoppingToken)
    {
        try
        {
            FeedIterator<CosmosPerson> resultSet = _container.GetItemQueryIterator<CosmosPerson>(query);
            while (resultSet.HasMoreResults)
            {
                FeedResponse<CosmosPerson> response = await resultSet.ReadNextAsync();
                _logger.LogInformation("Items returned this iteration: " + response.Count);
                _logger.LogInformation($"Request charge: {response.RequestCharge}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception occurred: {ex.Message}");
        }
    }

    // Utility Methods
    public static async Task DemoHotPartition(Container _container, string? partitionKeyPath, ILogger<Worker> _logger, CancellationToken stoppingToken)
    {
        // For this example, we will recreate a hot partition scenario
        // The container should have at least 30k RUs
        // We will seed the container with an initial 1k items
        // Then write additional items with a skewed partition key value

        IReadOnlyCollection<CosmosPerson> personsHot = BogusService.BulkGeneratePersons(10000);
        List<Task> tasks = new();

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
                            _logger.LogInformation($"Created item {ItemResponse.Result.Resource.Id} in container {_container.Id}");
                        }
                        else
                        {
                            _logger.LogError($"Failed to create item {ItemResponse.Result.Resource.Id} in container {_container.Id}");
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
}
