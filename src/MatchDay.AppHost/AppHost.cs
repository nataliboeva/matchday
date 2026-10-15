var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure: one PostgreSQL server with a database per service, Redis for seat holds,
// RabbitMQ (with the management UI) for integration events.
var postgres = builder.AddPostgres("postgres");
var catalogDb = postgres.AddDatabase("catalogdb");
var bookingDb = postgres.AddDatabase("bookingdb");
var notificationsDb = postgres.AddDatabase("notificationsdb");

var redis = builder.AddRedis("redis");

var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

// Services
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
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithReference(catalogApi)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", _ => new() { Url = "/scalar", DisplayText = "API reference (Scalar)" });

builder.AddProject<Projects.MatchDay_Notifications_Worker>("notifications-worker")
    .WithReference(notificationsDb)
    .WaitFor(notificationsDb)
    .WithReference(messaging)
    .WaitFor(messaging);

builder.Build().Run();
