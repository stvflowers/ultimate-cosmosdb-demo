# Ultimate Cosmosdb Demo
The one demo to rule them all. The goal of this repo is to keep a single demo up to date the evolves with new features.

# Prereqs
Add packages from NuGet.

>dotnet add package Microsoft.Azure.Cosmos

>dotnet add package Azure.Identity

>dotnet add package Bogus

>dotnet add package Spectre.Console

>dotnet add package Newtonsoft.Json

Initialize and set local environment variables using dotnet secrets store.

>dotnet user-secrets init

>dotnet user-secrets set "cosmosEndpoint" ""

>dotnet user-secrets set "cosmosDatabase" ""

>dotnet user-secrets set "cosmosContainer" ""

*Optional: Explicity define Entra ID Tenant Directory*

>dotnet user-secrets set "entraTenantId" ""