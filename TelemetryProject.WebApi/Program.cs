using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Services;
using Services.MqttService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<MqttClientWorker>();
builder.Services.AddSingleton<IMqttClientPublish, MqttClientPublish>();
builder.Services.AddSingleton<IInfluxDbService, InfluxDbService>();

builder.Services.Configure<MqttSettings>(builder.Configuration.GetSection("MQTT"));
builder.Services.Configure<InfluxDbSettings>(builder.Configuration.GetSection("InfluxDB"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/servo/{position}", async (ushort position, IMqttClientPublish publish) =>
{
    await publish.ServoAsync(position);
});

app.MapPost("/led/{state}", async (bool state, IMqttClientPublish publish) =>
{
    await publish.LedAsync(state);
});

app.MapGet("/humidex", (IInfluxDbService influxDbService) =>
{
    return influxDbService.ReadAllHumidex();
});

app.MapGet("/humidex/{startTime}/{endTime}", (DateTime startTime, DateTime endTime, IInfluxDbService influxDbService) =>
{
    return influxDbService.ReadAllHumidex(startTime, endTime);
});

app.MapGet("/humidex/latest", (IInfluxDbService influxDbService) =>
{
    return Results.Ok(influxDbService.ReadLatestHumidex()) ?? Results.NotFound();
});

app.Run();