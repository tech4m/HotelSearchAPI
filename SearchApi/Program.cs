using Nest;
using SearchApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

var app = builder.Build();

app.MapGet("/search", async (string? city, int? rating) =>
{
    

    var connSettings = new ConnectionSettings(new Uri(host));
    connSettings.BasicAuthentication(userName, password);

    connSettings.DefaultIndex(indexName);
    connSettings.DefaultMappingFor<Hotel>(m => m.IdProperty(p => p.Id));

    var esClient = new Nest.ElasticClient(connSettings);
});

app.Run();