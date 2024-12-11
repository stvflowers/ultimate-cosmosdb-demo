using Bogus;
using ultimate_cosmosdb_demo.Models;
using System.Collections.Generic;

namespace ultimate_cosmosdb_demo.Services
{
    public class BogusService
    {
        public static CosmosPerson GeneratePerson()
        {

            
            var bogusPerson = new Bogus.Person();
            
            // Partition Key is arbitrary and a random integer from 1 - 3.
            // This is to demonstrate partitioning in Cosmos DB
            // and allow us to control the number of items in each partition.
            Random random = new Random();
            int pk = random.Next(1, 4);
            
            CosmosPerson person = new()
            {
                Id = Guid.NewGuid().ToString(),
                PartitionKey = pk.ToString(),
                FirstName = bogusPerson.FirstName,
                LastName = bogusPerson.LastName,
                UserName = bogusPerson.UserName,
                Email = bogusPerson.Email,
                Phone = bogusPerson.Phone,
                Avatar = bogusPerson.Avatar,
                DateOfBirth = bogusPerson.DateOfBirth.ToString(),
                Address = new Address
                {
                    Street = bogusPerson.Address.Street,
                    City = bogusPerson.Address.City,
                    State = bogusPerson.Address.State,
                    ZipCode = bogusPerson.Address.ZipCode,
                    Geo = new Geo
                    {
                        Lat = bogusPerson.Address.Geo.Lat,
                        Lng = bogusPerson.Address.Geo.Lng
                    }
                },
                WebSite = bogusPerson.Website,
                Company = new Company
                {
                    Name = bogusPerson.Company.Name,
                    CatchPhrase = bogusPerson.Company.CatchPhrase,
                    Bs = bogusPerson.Company.Bs
                }
            };

            return person;
                
        }
        public static IReadOnlyCollection<CosmosPerson> BulkGeneratePersons(int count)
        {

            List<CosmosPerson> persons = [];
            
            for (int i = 0; i < count; i++)
            {
                var bogusPerson = new Bogus.Person();

                // Partition Key is arbitrary and a random integer from 1 - 3.
                // This is to demonstrate partitioning in Cosmos DB
                // and allow us to control the number of items in each partition.
                Random random = new Random();
                int pk = random.Next(1, 4);
                
                CosmosPerson p = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    PartitionKey = pk.ToString(),
                    FirstName = bogusPerson.FirstName,
                    LastName = bogusPerson.LastName,
                    UserName = bogusPerson.UserName,
                    Email = bogusPerson.Email,
                    Phone = bogusPerson.Phone,
                    Avatar = bogusPerson.Avatar,
                    DateOfBirth = bogusPerson.DateOfBirth.ToString(),
                    Address = new Address
                    {
                        Street = bogusPerson.Address.Street,
                        City = bogusPerson.Address.City,
                        State = bogusPerson.Address.State,
                        ZipCode = bogusPerson.Address.ZipCode,
                        Geo = new Geo
                        {
                            Lat = bogusPerson.Address.Geo.Lat,
                            Lng = bogusPerson.Address.Geo.Lng
                        }
                    },
                    WebSite = bogusPerson.Website,
                    Company = new Company
                    {
                        Name = bogusPerson.Company.Name,
                        CatchPhrase = bogusPerson.Company.CatchPhrase,
                        Bs = bogusPerson.Company.Bs
                    }
                };
            
                persons.Add(p);
            
            }

            return persons;
                
        }
    }
}