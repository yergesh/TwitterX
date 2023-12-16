var builder = WebApplication.CreateBuilder(args);

builder.Host
        .UseOrleans(
            builder => builder
                .UseLocalhostClustering()
                .AddMemoryGrainStorageAsDefault()
                .AddMemoryGrainStorage("AccountState")
                .AddMemoryGrainStorage("PostState"));

var app = builder.Build();
app.Run();
