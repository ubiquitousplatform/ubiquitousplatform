


var builder = DistributedApplication.CreateBuilder(args);


//builder.AddProject("MyApp", "FakePath");

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.Aspirational_ApiService>("apiservice");

builder.AddProject<Projects.Aspirational_Web>("webfrontend")
    .WithReference(cache)
    .WithReference(apiService);


builder.Build().Run();

