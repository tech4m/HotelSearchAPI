using Carter;
using Nest;
using SearchApi.Models;
using Polly;
using System.Text.Json;
using Polly.CircuitBreaker;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCarter();
Console.WriteLine("CreateBuilder-->");
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
Console.WriteLine("AddSwaggerGen-->");
var app = builder.Build();
app.MapCarter();

Console.WriteLine("Build-->");

var circuitBreakerPolicy = Polly.Policy<List<Hotel>>
                           .Handle<Exception>()
                           .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 3, durationOfBreak: TimeSpan.FromSeconds(30));



app.MapGet("/search", async (string? city, int? rating) =>
{
    var result = new HttpResponseMessage();
    try
    {
        var hotels = circuitBreakerPolicy.ExecuteAsync(async () =>
        {
            return await SearchHotels(city, rating);
        });
        result.StatusCode = System.Net.HttpStatusCode.OK;
        result.Content = new StringContent(JsonSerializer.Serialize(hotels));
        result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        return result;
    }
    catch (BrokenCircuitException)
    {
        result.StatusCode = System.Net.HttpStatusCode.NotAcceptable;
        result.ReasonPhrase = "Cirtcuit is Open";
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        throw;
    }
    return result; 
});

async Task<List<Hotel>> SearchHotels(string? city, int? rating)
{
    Console.WriteLine("host-->");
    var host = "https://507b3c520fed4264a49694ead351d6d4.us-central1.gcp.cloud.es.io";
    var userName = "elastic";
    var password = "UrZfevHAhpsBRH04HMZZnkq5";
    var indexName = "event";
    Console.WriteLine("indexName-->");
    //var host = Environment.GetEnvironmentVariable("host");// "https://507b3c520fed4264a49694ead351d6d4.us-central1.gcp.cloud.es.io";
    //var userName = Environment.GetEnvironmentVariable("userName");//"elastic";
    //var password = Environment.GetEnvironmentVariable("password");//"UrZfevHAhpsBRH04HMZZnkq5";
    //var indexName = Environment.GetEnvironmentVariable("event");//"event";
    //var AWS_REGION = Environment.GetEnvironmentVariable("AWS_REGION");//"us-east-1";

    var connSettings = new ConnectionSettings(new Uri(host));
    connSettings.BasicAuthentication(userName, password);

    Console.WriteLine("BasicAuthentication-->");
    connSettings.DefaultIndex(indexName);
    connSettings.DefaultMappingFor<Hotel>(m => m.IdProperty(p => p.Id));

    Console.WriteLine("DefaultMappingFor-->");
    var esClient = new Nest.ElasticClient(connSettings);

    if (rating is null)
    {
        rating = 1;
    }

    Console.WriteLine("rating-->");
    //Match
    //Prefix
    //Range
    //Fuzzy match
    //
    ISearchResponse<Hotel> result;
    Console.WriteLine("ISearchResponse-->");
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

        Console.WriteLine("city is null-->");
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
        Console.WriteLine("city is not null-->");
    }

    return result.Hits.Select(x => x.Source).ToList();
}

Console.WriteLine("app.Environment.IsDevelopment()-->");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.Run();