namespace ultimate_cosmosdb_demo.Models;

public class CosmosPerson
{
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public string? Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? UserName { get; set; }
    public string? Avatar { get; set; }
    public string? DateOfBirth { get; set;}
    public Address? Address { get; set; }
    public string? WebSite { get; set; }
    public Company? Company { get; set; }

}
public class CosmosPersonTtl : CosmosPerson
{
    public int TTL { get; set; }
}
public class CosmosPersonHot : CosmosPerson
{
    public string? PartitionKey { get; set; }
}
public class Address
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public Geo? Geo { get; set; }
}
public class Geo
{
    public double? Lat { get; set; }
    public double? Lng { get; set; }
}
public class Company
{
    public string? Name { get; set; }
    public string? CatchPhrase { get; set; }
    public string? Bs { get; set; }
}
