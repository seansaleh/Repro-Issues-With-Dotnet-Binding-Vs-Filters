The issue: In Production my Filter code runs even when a `FromHeader` parameter is missing. But in `Development` a request would fail in Model Binding. This is unexpected to me that we'd have a different outcome like this!

```csharp
app.MapGet("/testFromHeader", ([FromHeader(Name = "X-My-Id")] string id
```

For posterity, my fix was: Give up on using "Development" and use "Staging" and `IsStaging()` instead. I never tracked down this bug fully


To test this for yourself: Run both projects and run the http requests from [repro-dotnet-null-headers.http](repro-dotnet-null-headers/repro-dotnet-null-headers.http)

Here's the outcomes:
When running in Development with:
```http request
GET {{Development_HostAddress}}/testFromHeader/
Accept: application/json
X-Test-Name: [Development] Without Header
```

we get the response:
```http response
HTTP/1.1 400 Bad Request
Content-Type: text/plain; charset=utf-8
Date: Mon, 27 Oct 2025 17:26:08 GMT
Server: Kestrel
Transfer-Encoding: chunked

Microsoft.AspNetCore.Http.BadHttpRequestException: Required parameter "string id" was not provided from header.
   at lambda_method2(Closure, Object, HttpContext)
   at Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddlewareImpl.Invoke(HttpContext context)
```
And the logs:
```
fail: Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware[1]
An unhandled exception has occurred while executing the request.
Microsoft.AspNetCore.Http.BadHttpRequestException: Required parameter "string id" was not provided from header.
at lambda_method2(Closure, Object, HttpContext)
at Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddlewareImpl.Invoke(HttpContext context)
```

But when running in production we get:
```http request
GET {{Production_HostAddress}}/testFromHeader/
Accept: application/json
X-Test-Name: [Production] Without Header
```

we get the response:
```http response
HTTP/1.1 409 Conflict
Content-Type: application/json; charset=utf-8
Date: Mon, 27 Oct 2025 17:27:06 GMT
Server: Kestrel
Transfer-Encoding: chunked

"[FILTER] [[Production] Without Header] 'id' is null. Simulating a failure inside the filter..."
```
And the logs:
```
warn: Program[0]
      [FILTER] Filter is NOW RUNNING...
fail: Program[0]
      [FILTER] [[Production] Without Header] 'id' is null. Simulating a failure inside the filter...
```


### Additional Details:


Full logs from Development:
```
fail: Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware[1]
An unhandled exception has occurred while executing the request.
Microsoft.AspNetCore.Http.BadHttpRequestException: Required parameter "string id" was not provided from header.
at lambda_method2(Closure, Object, HttpContext)
at Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddlewareImpl.Invoke(HttpContext context)
warn: Program[0]
[FILTER] Filter is NOW RUNNING...
info: Program[0]
[FILTER] [[Development] With Header] Filter is about to call next().
info: repro-dotnet-null-headers[0]
[HANDLER] [[Development] With Header] Handler is running. ID is: "abc-dev"
warn: Program[0]
[FILTER] Filter is NOW RUNNING...
fail: Program[0]
[FILTER] [[Development] Nullable Id: Without Header] 'id' is null. Simulating a failure inside the filter...
warn: Program[0]
[FILTER] Filter is NOW RUNNING...
info: Program[0]
[FILTER] [[Development] Nullable Id: With Header] Filter is about to call next().
info: repro-dotnet-null-headers[0]
[HANDLER] [[Development] Nullable Id: With Header] Handler is running. ID is: "abc-dev"
```

Full logs from Production:
```
warn: Program[0]
      [FILTER] Filter is NOW RUNNING...
fail: Program[0]
      [FILTER] [[Production] Without Header] 'id' is null. Simulating a failure inside the filter...
warn: Program[0]
      [FILTER] Filter is NOW RUNNING...
info: Program[0]
      [FILTER] [[Production] With Header] Filter is about to call next().
info: repro-dotnet-null-headers[0]
      [HANDLER] [[Production] With Header] Handler is running. ID is: "abc-prod"
warn: Program[0]
      [FILTER] Filter is NOW RUNNING...
fail: Program[0]
      [FILTER] [[Production] Nullable Id: Without Header] 'id' is null. Simulating a failure inside the filter...
warn: Program[0]
      [FILTER] Filter is NOW RUNNING...
info: Program[0]
      [FILTER] [[Production] Nullable Id: With Header] Filter is about to call next().
info: repro-dotnet-null-headers[0]
      [HANDLER] [[Production] Nullable Id: With Header] Handler is running. ID is: "abc-prod"
```

