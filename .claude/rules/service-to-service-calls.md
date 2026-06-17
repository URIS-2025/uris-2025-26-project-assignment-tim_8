# Service-to-Service Calls

How one ASP.NET Core service calls another (logging, notifications, cross-service lookups).

## Side-effect calls must be non-fatal (never throw into the caller)

Logging and notification calls are **side effects**: if the downstream is down, the user's
underlying operation (create/update/delete) must still succeed. Wrap them in try/catch-swallow
with a short timeout and forward the caller's bearer. Modeled on `LoggerServiceClient.TryLogAsync`,
which all 10 services already have.

| Pattern | Evidence |
|---------|----------|
| Non-fatal client: try/catch-swallow + 800ms timeout + forward Authorization | ProblemService/Clients/LoggerServiceClient.cs:17-35 |
| Notification producers follow the same shape | {Problem,Suggestion,Organization}Service/Clients/SystemNotificationServiceClient.cs |
| Cross-service lookup returns null on failure (non-throwing) | {Problem,Suggestion}Service/Clients/*BoxServiceClient.cs |

```csharp
// WRONG — throws on non-2xx, breaks the caller's create when the downstream is down
public async Task CreateBillingNotificationAsync(BillingNotificationCreateDTO dto)
{
    var response = await _httpClient.PostAsJsonAsync("/api/billingnotification", dto);
    if (!response.IsSuccessStatusCode)
        throw new Exception($"BillingService error: {response.StatusCode}");
}

// CORRECT — swallow + timeout + forward bearer; a side-effect failure never breaks the op
public virtual async Task TryNotifyAsync(SystemNotificationCreationDTO dto, string? bearerHeader, CancellationToken requestCt)
{
    try
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/SystemNotification");
        if (!string.IsNullOrWhiteSpace(bearerHeader))
            req.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerHeader);
        req.Content = JsonContent.Create(dto, options: new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(800));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(requestCt, timeoutCts.Token);
        using var res = await _http.SendAsync(req, linked.Token);
    }
    catch { }   // swallow — failure must not break the underlying op
}
```

## Always forward the caller's bearer token

Downstream endpoints may be `[Authorize]`d. Pass `Request.Headers["Authorization"]` from the
controller into every service-client call (the same value already used for `TryLogAsync`). A
producer that forgets gets a silent 401 and the side effect is dropped.

## Make clients mockable

| Requirement | Why |
|---|---|
| Client method is `virtual` | Moq can only verify/override virtual members on a concrete class |
| Public parameterless ctor alongside the `IHttpClientFactory` ctor | lets `new Mock<TheClient>()` work without a real HttpClient (LoggerServiceClient.cs:10-12) |
| Controller tests set `ControllerContext = new() { HttpContext = new DefaultHttpContext() }` | controllers read `Request.Headers`/`HttpContext.RequestAborted` — null without it |

## Registration idiom

Register a named `HttpClient` (base URL from `appsettings.json` `Services:<Name>`) + the wrapper
as scoped, mirroring the existing `LoggerService` registration in each `Program.cs`. Docker base
URLs resolve by container name on port 8080, e.g. `http://system-notification-service:8080/`.
