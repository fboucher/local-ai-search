# Skill: HttpClient Test Isolation (No Mocking Library)

## Pattern

Test `HttpClient`-dependent services in .NET without Moq or NSubstitute by injecting a custom `HttpMessageHandler`.

## Implementation

```csharp
// In test file — scoped to file with 'file' modifier (C# 11+)
file sealed class FakeHttpHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

// In test
var handler = new FakeHttpHandler(HttpStatusCode.OK, fakeJsonResponse);
var http = new HttpClient(handler);
var svc = new MyService(dependencies, httpClient: http);
```

## Design requirement

The production service must accept `HttpClient` as an optional constructor parameter:
```csharp
public MyService(..., HttpClient? httpClient = null)
{
    _http = httpClient ?? new HttpClient();
}
```

## Why

- Zero extra NuGet packages
- Full control over status code and response body per test
- `file` modifier prevents test helper leaking into other test classes
- Works with `System.Net.Http.Json` (`PostAsJsonAsync`, etc.)

## When to use

Any service that calls an external HTTP API where you need to test:
- Happy path (200 OK + structured response)
- Error path (4xx/5xx)
- Response parsing logic

## Stack

.NET 6+, C#, xunit (or any test framework)
