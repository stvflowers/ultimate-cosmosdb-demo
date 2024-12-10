using Microsoft.Azure.Cosmos;
using ultimate_cosmosdb_demo.Services;

namespace ultimate_cosmosdb_demo;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration.AddUserSecrets<Program>();

        // Register the Cosmos as a Singleton
        builder.Services.AddSingleton((s) => {
            
            string? entraTenantId = builder.Configuration["entraTenantId"];
            string? cosmosEndpoint = builder.Configuration["cosmosEndpoint"];

            if (cosmosEndpoint == null)
            {
                throw new ArgumentNullException(nameof(cosmosEndpoint));
            }

            CosmosClient cosmosClient = CosmosService.GetCosmosClient(cosmosEndpoint, entraTenantId);

            return cosmosClient;

        });

        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}