var builder = DistributedApplication.CreateBuilder(args);

var pgUsername = builder.AddParameter("pg-username", secret: true);
var pgPassword = builder.AddParameter("pg-password", secret: true);

// ── Postgres ──────────────────────────────────────────────────────────────────
var pg =
    builder.AddPostgres("postgres", pgUsername, pgPassword)
           .WithHostPort(5432)
           .WithDataVolume();

var data = pg.AddDatabase("dp2-local-data");
var log = pg.AddDatabase("dp2-local-log");

builder.AddProject<Projects.GHB_DP2_Api>("ghb-dp2-api")
       .WithEnvironment("ConnectionStrings__DefaultConnection", data)
       .WithEnvironment("ConnectionStrings__LogDatabase", log)
       .WithReference(pg)
       .WaitFor(pg);

await builder.Build().RunAsync();