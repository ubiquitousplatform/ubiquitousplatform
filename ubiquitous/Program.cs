

// TODO: make ubiquitous.functions runnable either as a library or as a standalone application

using ubiquitous.functions.ExecutionContext.FunctionPool;

FunctionPool pool = new();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

string[] summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    WeatherForecast[] forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

byte[] funcCode = File.ReadAllBytes("../ubiquitous.functions/javy-example.wasm");
app.MapGet("/extismtest", async () =>
{
    // TODO: should function execution be synchronous? probably not, or do we support both direct and queued executions?
    // for performance reasons, direct in vocations ncould get higher priority in the executor engine.
    ubiquitous.functions.ExecutionContext.RuntimeQueue.WasmRuntime? runtime = pool.CheckoutRuntime("ubiquitous_quickjs_v1");
    runtime.LoadFunctionCode("js_user_code_instance", funcCode);
    runtime.InvokeMethod("_start");
    pool.CheckinRuntime(runtime);
    //return await 

});
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

