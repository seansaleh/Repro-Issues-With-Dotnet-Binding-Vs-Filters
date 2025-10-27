using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

async ValueTask<object?> TestHandlerFilter(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
{
    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("[FILTER] Filter is NOW RUNNING...");

    // If the "X-My-Id" header is missing, 'idArgument' will be null.
    var idArgument = context.GetArgument<string>(0);
    var testName = context.GetArgument<string>(1);

    if (string.IsNullOrEmpty(testName))
    {
        throw new Exception("[FILTER] Unable to find 'testName' in the request.");
    }

    if (String.IsNullOrEmpty(idArgument))
    {
        logger.LogError($"[FILTER] [${testName}] 'id' is null. Simulating a failure inside the filter...");

        throw new Exception($"[FILTER] [${testName}] 'id' is null. Simulating a failure inside the filter...");
    }

    // This code is never reached if the exception is thrown
    logger.LogInformation($"[FILTER] [${testName}] Filter is about to call next().");
    return await next(context);
}

app.MapGet("/testFromHeader", ([FromHeader(Name = "X-My-Id")] string id, [FromHeader(Name = "X-Test-Name")] string testName) =>
    {
        app.Logger.LogInformation("[HANDLER] [${testName}] Handler is running. ID is: {Id}", testName, id ?? "null");
        return Results.Ok($"[${testName}] Handler ran. ID was: {id ?? "null"}");
    })
    .AddEndpointFilter(TestHandlerFilter);
    
app.MapGet("/testFromHeaderNullable", ([FromHeader(Name = "X-My-Id")] string? id, [FromHeader(Name = "X-Test-Name")] string testName) =>
    {
        app.Logger.LogInformation("[HANDLER] [${testName}] Handler is running. ID is: {Id}", testName, id ?? "null");
        return Results.Ok($"[${testName}] Handler ran. ID was: {id ?? "null"}");
    })
    .AddEndpointFilter(TestHandlerFilter);
    
app.Run();