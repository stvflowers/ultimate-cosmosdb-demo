using Microsoft.Azure.Cosmos;
using Azure.Identity;
using ultimate_cosmosdb_demo.Models;

namespace ultimate_cosmosdb_demo.Services
{
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

            CosmosClient cosmosClient = new(cosmosEndpoint, cred, clientOptions);

            return cosmosClient;
        }
        public static async Task BulkWrite(CosmosClient _client, string? database, string? container, CancellationToken stoppingToken)
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
                                Console.WriteLine($"Created item {ItemResponse.Result.Resource.Id} in database {_database.Id} container {_container.Id}");
                            }
                            else
                            {
                                Console.WriteLine($"Failed to create item {ItemResponse.Result.Resource.Id} in database {_database.Id} container {_container.Id}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception occurred: {ex.Message}");
                        }
                    }));
            }

            await Task.WhenAll(tasks);

        }    
        public static async Task WriteItem(CosmosClient _client, string? database, string? container, CancellationToken stoppingToken)
        {
            Database _database = _client.GetDatabase(database);
            Container _container = _database.GetContainer(container);

            CosmosPerson person = BogusService.GeneratePerson();

            await _container.CreateItemAsync<CosmosPerson>(person)
                .ContinueWith(ItemResponse =>
                {
                    try
                    {
                        if (ItemResponse.IsCompletedSuccessfully)
                        {
                            Console.WriteLine($"Created item {ItemResponse.Result.Resource.Id} in database {_database.Id} container {_container.Id}");
                            Console.WriteLine($"Person: {person.FirstName} {person.LastName}");
                            Console.WriteLine("HTTP status code: " + ItemResponse.Result.StatusCode);
                            Console.WriteLine("Operation request charge: " + ItemResponse.Result.RequestCharge);
                        }
                        else
                        {
                            Console.WriteLine($"Failed to create item {ItemResponse.Result.Resource.Id} in database {_database.Id} container {_container.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception occurred: {ex.Message}");
                    }
                });            
        }
        public static async Task PatchItemNewFirstName(CosmosClient _client, string? database, string? container, CosmosPerson person, string newFirstName, CancellationToken stoppingToken)
        {
            Database _database = _client.GetDatabase(database);
            Container _container = _database.GetContainer(container);

            await _container.PatchItemAsync<CosmosPerson>(
                id: person.Id,
                partitionKey: new PartitionKey(person.Email),
                patchOperations: new[] { PatchOperation.Replace("/firstName", newFirstName) })
                .ContinueWith(ItemResponse =>
                {
                    try
                    {
                        if (ItemResponse.IsCompletedSuccessfully)
                        {
                            Console.WriteLine($"Upserted item {ItemResponse.Result.Resource.Id} in database {_database.Id} container {_container.Id}");
                            Console.WriteLine("Operation request charge: " + ItemResponse.Result.RequestCharge);
                        }
                        else
                        {
                            Console.WriteLine($"Failed to upsert item {ItemResponse.Result.Resource.Id} in database {_database.Id} container {_container.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception occurred: {ex.Message}");
                    }
                });
        }
        public static async Task QueryItems(CosmosClient _client, string? database, string? container, CancellationToken stoppingToken)
        {
            Database _database = _client.GetDatabase(database);
            Container _container = _database.GetContainer(container);

            QueryDefinition query = new("SELECT * FROM c");
            FeedIterator<CosmosPerson> resultSet = _container.GetItemQueryIterator<CosmosPerson>(query);

            while (resultSet.HasMoreResults)
            {
                FeedResponse<CosmosPerson> response = await resultSet.ReadNextAsync();
                Console.WriteLine("Items returned this iteration: " + response.Count);
                Console.WriteLine($"Request charge: {response.RequestCharge}");
                // foreach (CosmosPerson person in response)
                // {
                //     Console.WriteLine($"Person: {person.FirstName} {person.LastName}");
                // }
            }
        }
        public static async Task PointReadItem(CosmosClient _client, string? database, string? container, string id, string partitionKey, CancellationToken stoppingToken)
        {
            Database _database = _client.GetDatabase(database);
            Container _container = _database.GetContainer(container);

            ItemResponse<CosmosPerson> response = await _container.ReadItemAsync<CosmosPerson>(id, new PartitionKey(partitionKey));
            CosmosPerson person = response.Resource;

            Console.WriteLine($"Person: {person.FirstName} {person.LastName}");
        }
    
    }
}

