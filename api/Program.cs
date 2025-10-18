using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication() // this line satisfies AZFW0014
    .ConfigureServices((context, services) =>
    {
        // bind DB connection string from configuration/environment
        services.AddOptions<DbOptions>()
            .Configure<IConfiguration>((opts, cfg) =>
            {
                opts.ConnectionString = cfg["DB"]
                    ?? throw new InvalidOperationException("DB connection string missing (expecting 'DB' setting)");
            });

        services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<DbOptions>>().Value);
    })
    .Build();

var dbOptions = host.Services.GetRequiredService<DbOptions>();
await Data.EnsureSchema(dbOptions.ConnectionString);

host.Run();
