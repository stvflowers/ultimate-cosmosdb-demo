using Microsoft.Azure.Cosmos;
using Azure.Identity;
using ultimate_cosmosdb_demo.Models;
using ultimate_cosmosdb_demo.Services;

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
    
    }
}

