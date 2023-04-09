

// TODO: make ubiquitous.functions runnable either as a library or as a standalone application

using ubiquitous.functions;
using Wasmtime;

var pool = new FunctionPool();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};


int invocationCounter = 0;

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
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


app.MapGet("/extismtest", async () =>
{
    // TODO: should function execution be synchronous? probably not, or do we support both direct and queued executions?
    // for performance reasons, direct in vocations ncould get higher priority in the executor engine.
    return await pool.ExecuteFunction("a", "b");

});
app.MapGet("/wasmtimetest", async () =>
{
    using var engine = new Engine();
    using var module = Module.FromTextFile(engine, "say_hello.wat");
    Console.WriteLine("wasmtime-test");
    // using var module = Module.FromText(
    //     engine,
    //     "hello",
    //     "(module (func $hello (import \"\" \"hello\")) (func (export \"run\") (call $hello)))"
    // );

    using var linker = new Linker(engine);
    using var store = new Store(engine);

    linker.Define(
        "ubiquitous",
        "host",
        Function.FromCallback(store, (string action, string payload) =>
        {
            Console.WriteLine("Host function executed.");
            Console.WriteLine(action);
            Console.WriteLine(payload);
            invocationCounter++;
        })
    );

    var instance = linker.Instantiate(store, module);
    var run = instance.GetAction("run")!;
    run();
    return invocationCounter;
});


app.Run();


record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

