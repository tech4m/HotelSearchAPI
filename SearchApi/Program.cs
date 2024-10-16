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

    if (rating is null)
    {
        rating = 1;
    }

    //Match
    //Prefix
    //Range
    //Fuzzy match
    //
    ISearchResponse<Hotel> result;

    if (city is null)
    {
        result = await esClient.SearchAsync<Hotel>(
            s => s.Query
                (
                    q => q.MatchAll()
                    &&
                    q.Range(r => r.Field(f => f.Rating).GreaterThanOrEquals(rating))
                )
            );
    }
    else
    {
        result = await esClient.SearchAsync<Hotel>(
            s => s.Query
                (
                    q => q.Prefix(p => p.Field(f => f.City).Value(city).CaseInsensitive())
                    &&
                    q.Range(r => r.Field(f => f.Rating).GreaterThanOrEquals(rating))
                )
            );
    }

    return result.Hits.Select(x=>x.Source).ToList();
});

app.Run();