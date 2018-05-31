## Community.AspNetCore.JsonRpc

[JSON-RPC 2.0](http://www.jsonrpc.org/specification) middleware for ASP.NET Core 2 based on the [JSON-RPC 2.0 Transport: HTTP](https://www.simple-is-better.org/json-rpc/transport_http.html) specification.

[![NuGet package](https://img.shields.io/nuget/v/Community.AspNetCore.JsonRpc.svg?style=flat-square)](https://www.nuget.org/packages/Community.AspNetCore.JsonRpc)

### Features

- The middleware transparently handles batch JSON-RPC requests.
- The middleware automatically handles common JSON-RPC issues.
- The middleware supports for maximum string identifier length (`128` by default) and maximum batch size (`1024` by default).
- The middleware supports JSON-RPC errors diagnostic via an implementation of the `IJsonRpcDiagnosticProvider` interface.
- A handler / service can be acquired from a service provider or instantiated directly for a request scope.
- A handler / service which is disposable will be automatically disposed on request scope exit.
- A service supports `JsonRpcNameAttribute` attributes defined on class and interface members.
- A service supports default method parameter values for named parameters if they are not provided in a request.

### Specifics

- In addition to the standard JSON-RPC error codes the middleware may return the following JSON-RPC errors (which are also defined in the `JsonRpcTransportErrorCodes` type):

Code | Reason
:---: | ---
`-32000` | The provided batch contains requests with duplicate identifiers
`-32010` | The provided message identifier exceeds length limit
`-32020` | The provided batch exceeds size limit

- In addition to the JSON-RPC HTTP transport specification the middleware may return the following HTTP status codes:

Code | Reason
:---: | ---
`400` | The `Content-Length` header has a value that differs from the actual content length
`415` | The `Content-Encoding` header is specified

- With logger factory availability, the following events may appear in a journal:

ID | Level | Reason
:---: | --- | ---
`1000` | Debug | A JSON-RPC request accepted for processing as a single item
`1010` | Debug | A JSON-RPC request accepted for processing as a batch
`2000` | Information | A JSON-RPC request processed as notification
`2010` | Information | A JSON-RPC request processed with result
`2020` | Information | A JSON-RPC request processed with error
`2030` | Information | A JSON-RPC request processed with result as notification due to client demand
`2040` | Information | A JSON-RPC request processed with error as notification due to client demand
`3000` | Warning | A JSON-RPC request processed as notification due to server configuration
`4000` | Error | An error occurred during deserialization of a JSON-RPC request
`4010` | Error | A JSON-RPC request is not considered as a valid JSON-RPC message
`4020` | Error | A JSON-RPC batch contains requests with duplicate identifiers
`4030` | Error | A JSON-RPC message identifier exceeds length limit
`4040` | Error | A JSON-RPC batch exceeds size limit

### Examples

```cs
public class MyJsonRpcService : IJsonRpcService
{
    [JsonRpcName("m1")]
    public Task<long> Method1([JsonRpcName("p1")] long p1, [JsonRpcName("p2")] long p2)
    {
        if (p2 == 0L)
        {
            throw new JsonRpcServiceException(100L);
        }

        return Task.FromResult(p1 / p2);
    }

    [JsonRpcName("m2")]
    public Task<long> Method2(long p1, long p2)
    {
        return Task.FromResult(p1 + p2);
    }
}
```
```cs
builder
    .ConfigureServices(sc => sc.AddJsonRpcService<MyJsonRpcService>())
    .Configure(ab => ab.UseJsonRpcService<MyJsonRpcService>("/api"))
```
or
```cs
public class MyJsonRpcHandler : IJsonRpcHandler
{
    public IReadOnlyDictionary<string, JsonRpcRequestContract> CreateScheme()
    {
        return new Dictionary<string, JsonRpcRequestContract>
        {
            ["m1"] = new JsonRpcRequestContract(new Dictionary<string, Type>
                {
                    ["p1"] = typeof(long),
                    ["p2"] = typeof(long)
                }),
            ["m2"] = new JsonRpcRequestContract(new[]
                {
                    typeof(long),
                    typeof(long)
                })
        };
    }

    public Task<JsonRpcResponse> HandleAsync(JsonRpcRequest request)
    {
        var response = default(JsonRpcResponse);

        switch (request.Method)
        {
            case "m1":
                {
                    var p1 = (long)request.ParametersByName["p1"];
                    var p2 = (long)request.ParametersByName["p2"];

                    response = p2 != 0L ?
                        new JsonRpcResponse(p1 / p2, request.Id) :
                        new JsonRpcResponse(new JsonRpcError(100L), request.Id);
                }
                break;
            case "m2":
                {
                    var p1 = (long)request.ParametersByPosition[0];
                    var p2 = (long)request.ParametersByPosition[1];

                    response = new JsonRpcResponse(p1 + p2, request.Id);
                }
                break;
        }

        return Task.FromResult(response);
    }
}
```
```cs
builder
    .ConfigureServices(sc => sc.AddJsonRpcHandler<MyJsonRpcHandler>())
    .Configure(ab => ab.UseJsonRpcHandler<MyJsonRpcHandler>("/api"))
```