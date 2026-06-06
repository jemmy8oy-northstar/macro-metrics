# Testing Strategy

This document covers the testing approach across the full stack. The strategy is designed to give fast feedback at every level without the overhead of real infrastructure.

For the SRP/orchestrator design that makes this testable, see `docs/specs/backend-srp.md`.
For when tests are written relative to the development workflow, see `docs/specs/sdd-workflow.md`.

---

## Test Pyramid

```
         [ Frontend: Vitest + RTL ]     ← component behaviour
        [  Unit: xUnit + Moq      ]     ← single service in isolation
     [ In-Process Integration     ]     ← API layer only: DI-wired HTTP services, HTTP stubbed
```

No test database. No containers. No infrastructure dependencies.

---

## Backend Unit Tests (xUnit + Moq)

Unit tests cover a single service class with all dependencies mocked. They verify that the class behaves correctly in isolation.

```csharp
public class MonthlyReportOrchestratorTests
{
    private readonly Mock<IReportDataService> _reportDataService = new();
    private readonly Mock<IContactsService> _contactsService = new();
    private readonly Mock<IReportGenerator> _reportGenerator = new();
    private readonly Mock<IEmailBuilder> _emailBuilder = new();
    private readonly Mock<IEmailService> _emailService = new();

    private MonthlyReportOrchestrator CreateSut() => new(
        _reportDataService.Object,
        _contactsService.Object,
        _reportGenerator.Object,
        _emailBuilder.Object,
        _emailService.Object
    );

    [Fact]
    public async Task ProcessMonthlyReport_SendsEmailToAllRecipients()
    {
        // Arrange
        var orgId      = Guid.NewGuid();
        var data       = new ReportData();
        var recipients = new List<Recipient> { new("a@b.com") };
        var report     = new Report();
        var email      = new Email();

        _reportDataService.Setup(x => x.FetchAsync(orgId)).ReturnsAsync(data);
        _contactsService.Setup(x => x.GetRecipientsAsync(orgId)).ReturnsAsync(recipients);
        _reportGenerator.Setup(x => x.Generate(data)).Returns(report);
        _emailBuilder.Setup(x => x.BuildReportEmail(report, recipients)).Returns(email);

        // Act
        await CreateSut().ProcessMonthlyReportAsync(orgId);

        // Assert
        _emailService.Verify(x => x.SendAsync(email), Times.Once);
    }
}
```

**Conventions:**
- One test class per service
- `CreateSut()` factory keeps construction in one place
- Each test verifies one behaviour
- Mock field names match the injected interface names

---

## In-Process Integration Tests

In-process integration tests are used **exclusively for API layer services** — services that wrap external HTTP APIs (e.g. ONS, yfinance). They are not used for orchestration logic; orchestration is covered by unit tests with mocked dependencies.

These tests wire the **real service implementation** through the production DI registration (`AddHttpClient<IInterface, Implementation>`), stubbing only the HTTP transport via a fake `HttpMessageHandler`. No real network calls are made.

This is distinct from unit tests (which instantiate the service directly with `new`) because they verify the **DI wiring** is correct — i.e. that `AddHttpClient` resolves the expected concrete type — as well as the full HTTP → deserialise → map pipeline when resolved from the container.

```csharp
public class OnsFetcherServiceIntegrationTests
{
    private static ServiceProvider BuildProvider(HttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddHttpClient<IOnsFetcherService, OnsFetcherService>()
                .ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task FetchRawAsync_DiWired_ParsesMonthsAndReturnsCorrectPoints()
    {
        // Arrange — stub HTTP boundary only
        var json = """{"months":[{"date":"2024 JAN","value":"285.3"},{"date":"2024 FEB","value":"287.1"}]}""";
        using var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        await using var provider = BuildProvider(handler);

        var sut = provider.GetRequiredService<IOnsFetcherService>();

        // Act
        var result = (await sut.FetchRawAsync("uk-house-prices")).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(new DateOnly(2024, 1, 1), result[0].Date);
        Assert.Equal(285.3m, result[0].Value);
    }
}
```

**What these tests catch:**
- DI wiring is correct — `AddHttpClient<IInterface, Implementation>` resolves the expected concrete type
- URL routing — the service calls the correct API path for each metric ID
- Response parsing — the external JSON/XML shape is correctly deserialised to domain types
- Error propagation — non-200 HTTP responses produce the correct typed exception

**What these tests do not cover:**
- Individual service logic (that's unit tests)
- Orchestration between multiple services (use unit tests with mocked dependencies)
- Database persistence (out of scope — no test DB)
- UI behaviour (that's frontend tests)
- Real HTTP network calls (stub at the `HttpMessageHandler` boundary — no live API calls in CI)

**Rule: when to use integration tests vs unit tests**

| Scenario | Test type |
|---|---|
| Service wraps an external HTTP API (fetcher, client) | **Integration test** (DI-wired, HTTP stubbed) |
| Service orchestrates other services | **Unit test** (all deps mocked) |
| Service contains pure calculation/mapping logic | **Unit test** |

### Scenario-First, Implement Last

In-process integration test **scenarios** for API layer services are defined during architecture design (SDD Phase 5) — before any implementation. The scenarios capture what the DI-wired HTTP pipeline must do. The actual test implementation is deferred to the end of Phase 6, once the service and its DI registration are in place.

**Scenario (defined in Phase 5):**
```
Given the ONS fetcher service is resolved from the DI container
When FetchRawAsync("uk-house-prices") is called
Then the service calls the correct ONS API path
And the stubbed JSON "months" array is parsed into RawDataPoint records with correct dates and values
And a non-200 HTTP response propagates a FetcherException
```

**Test implementation (end of Phase 6):** — as shown above.

---

## Top-Down TDD Flow

```
Phase 5:  Define integration test scenarios for API layer services (what, not how)
          Define unit test scenarios for orchestrators and logic services
Phase 6:  ┌─ Write unit test for the orchestrator (all deps mocked)
          │  Implement orchestrator → tests pass
          │
          ├─ Write unit tests for each called service
          │  Implement each service → tests pass
          │
          ├─ Continue down to leaves (data services, builders, etc.)
          │
          └─ Implement in-process integration tests for HTTP API layer services
             Wire via AddHttpClient DI, stub HttpMessageHandler → tests pass
```

**Why top-down?**
- You define the contract at each level before worrying about implementation
- Orchestrator-level unit tests pass immediately — mocks return expected data, so you get green feedback before any dependent service exists
- Each level is testable in isolation regardless of what is below it
- In-process integration tests verify the DI wiring and HTTP pipeline for API layer services and serve as a regression safety net

---

## Frontend Tests (Vitest + React Testing Library)

Frontend tests follow the same spec-first philosophy: write the test before the component, describe behaviour from the user's perspective, not implementation details.

```tsx
// Navbar.test.tsx
describe('Navbar', () => {
  it('highlights the active route link', () => {
    render(<Navbar />, { wrapper: routerWrapper('/boards') });
    expect(screen.getByRole('link', { name: 'Boards' })).toHaveAttribute('data-active', 'true');
  });
});
```

**Use Vitest** — it is native to Vite and requires no additional configuration.

A frontend unit test is an executable spec step. Writing it first forces you to define what "done" looks like for the component before writing any JSX.
