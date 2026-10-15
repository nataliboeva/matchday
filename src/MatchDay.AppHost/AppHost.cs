var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var catalogDb = postgres.AddDatabase("catalogdb");
var bookingDb = postgres.AddDatabase("bookingdb");

var redis = builder.AddRedis("redis");

var catalogApi = builder.AddProject<Projects.MatchDay_Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", _ => new() { Url = "/scalar", DisplayText = "API reference (Scalar)" });

builder.AddProject<Projects.MatchDay_Booking_Api>("booking-api")
    .WithReference(bookingDb)
    .WaitFor(bookingDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(catalogApi)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", _ => new() { Url = "/scalar", DisplayText = "API reference (Scalar)" });

builder.Build().Run();
