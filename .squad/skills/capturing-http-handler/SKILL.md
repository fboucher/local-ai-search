# Skill: Capturing HttpMessageHandler (Request Body Assertion)

## Pattern

When you need to assert what was *sent* to an HTTP API (not just what the API returns), use a `CapturingHttpHandler` that records the request body for inspection in tests.

## Implementation

```csharp
file sealed class CapturingHttpHandler(string responseBody) : HttpMessageHandler
{
    public string? CapturedRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content != null)
            CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };
    }
}

// In test
var handler = new CapturingHttpHandler(fakeJsonResponse);
var svc = new MyService(dependencies, httpClient: new HttpClient(handler));

await svc.DoSomethingAsync();

Assert.NotNull(handler.CapturedRequestBody);
using var doc = JsonDocument.Parse(handler.CapturedRequestBody!);
var field = doc.RootElement.GetProperty("expectedField").GetString();
Assert.Equal("expectedValue", field);
```

## When to use

Use this instead of the simpler `FakeHttpHandler` when you need to verify:
- The correct payload structure was sent (e.g., Base64 data URL format)
- A specific field value appears in the request JSON
- The right model/prompt/parameter was included in the API call

## Complement

Pair with `FakeHttpHandler` (see `httpclient-test-isolation` skill) for response-only tests. Use `CapturingHttpHandler` only when the *request* body itself is under test.

## Stack

.NET 6+, C#, xunit, `System.Text.Json`
