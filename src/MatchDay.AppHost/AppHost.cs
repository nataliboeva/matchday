var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var catalogDb = postgres.AddDatabase("catalogdb");

builder.AddProject<Projects.MatchDay_Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", _ => new() { Url = "/scalar", DisplayText = "API reference (Scalar)" });

builder.Build().Run();
