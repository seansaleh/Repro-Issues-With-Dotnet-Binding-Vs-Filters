using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// There is still an issue regardless of UseExceptionHandler and AddProblemDetails
builder.Services.AddProblemDetails(); 

var app = builder.Build();

// There is still an issue regardless of UseExceptionHandler and AddProblemDetails
app.UseExceptionHandler();

// One theory on why this is different between production and development is that we have a
// Developer Exception Page in development, but not in production.
// This forces it to exist in development too.
app.UseDeveloperExceptionPage();

// Always MapOpenApi() so that we are sure it is not the difference between Development and Production.
app.MapOpenApi();

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
        logger.LogError($"[FILTER] [{testName}] 'id' is null. Simulating a failure inside the filter...");

        // Note, we use Conflict here instead of BadRequest because BadRequest is what naturally happens when the header is missing.
        return Results.Conflict($"[FILTER] [{testName}] 'id' is null. Simulating a failure inside the filter...");
    }

    // This code is never reached if the exception is thrown
    logger.LogInformation($"[FILTER] [{testName}] Filter is about to call next().");
    return await next(context);
}

app.MapGet("/testFromHeader", ([FromHeader(Name = "X-My-Id")] string id, [FromHeader(Name = "X-Test-Name")] string testName) =>
    {
        app.Logger.LogInformation("[HANDLER] [{testName}] Handler is running. ID is: {Id}", testName, id ?? "null");
        return Results.Ok($"[{testName}] Handler ran. ID was: {id ?? "null"}");
    })
    .AddEndpointFilter(TestHandlerFilter);
    
// When the string header is nullable then the filter code works as expected the same between Development and Production.
app.MapGet("/testFromHeaderNullable", ([FromHeader(Name = "X-My-Id")] string? id, [FromHeader(Name = "X-Test-Name")] string testName) =>
    {
        app.Logger.LogInformation("[HANDLER] [{testName}] Handler is running. ID is: {Id}", testName, id ?? "null");
        return Results.Ok($"[{testName}] Handler ran. ID was: {id ?? "null"}");
    })
    .AddEndpointFilter(TestHandlerFilter);
    
app.Run();