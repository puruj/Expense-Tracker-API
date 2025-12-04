using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ExpenseTracker.API.Models.Auth;
using ExpenseTracker.API.Models.Entities;
using ExpenseTracker.API.Models.Expenses;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ExpenseTracker.Test;

public class AuthAndExpenseIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthAndExpenseIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static string UniqueEmail() => $"user_{Guid.NewGuid():N}@example.com";

    private static WebApplicationFactoryClientOptions ClientOptions => new()
    {
        AllowAutoRedirect = false
    };

    private static HttpClient CreateClient(CustomWebApplicationFactory factory) =>
        factory.CreateClient(ClientOptions);

    private static async Task<(HttpClient Client, AuthResponse Auth)> RegisterAndLoginAsync(
        CustomWebApplicationFactory factory,
        string? password = null)
    {
        var client = CreateClient(factory);
        var pwd = password ?? "Password123!";
        var email = UniqueEmail();

        var register = new RegisterRequest
        {
            FullName = "Test User",
            Email = email,
            Password = pwd
        };

        var registerResp = await client.PostAsJsonAsync("/api/Auth/register", register);
        registerResp.EnsureSuccessStatusCode();

        var login = new LoginRequest
        {
            Email = email,
            Password = pwd
        };

        var loginResp = await client.PostAsJsonAsync("/api/Auth/login", login);
        loginResp.EnsureSuccessStatusCode();

        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));

        return (client, auth);
    }

    [Fact]
    public async Task ExpenseEndpoints_RequireJwt()
    {
        var client = CreateClient(_factory);

        var response = await client.GetAsync("/api/Expense");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Can_Register_Login_And_Create_Expense_With_Jwt()
    {
        var (client, auth) = await RegisterAndLoginAsync(_factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var createRequest = new CreateExpenseRequest
        {
            Amount = 25.50m,
            CurrencyCode = "USD",
            Category = ExpenseCategory.Groceries,
            Description = "Milk and eggs",
            IncurredAtUtc = DateTime.UtcNow
        };

        var createResp = await client.PostAsJsonAsync("/api/Expense", createRequest);
        createResp.EnsureSuccessStatusCode();

        var created = await createResp.Content.ReadFromJsonAsync<ExpenseDto>();
        Assert.NotNull(created);
        Assert.Equal(createRequest.Amount, created!.Amount);
        Assert.Equal(createRequest.CurrencyCode, created.CurrencyCode);
        Assert.Equal(createRequest.Category, created.Category);
    }

    [Fact]
    public async Task Expense_Crud_Flow_Works_For_Authorized_User()
    {
        var (client, auth) = await RegisterAndLoginAsync(_factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var createRequest = new CreateExpenseRequest
        {
            Amount = 100m,
            CurrencyCode = "USD",
            Category = ExpenseCategory.Electronics,
            Description = "Headphones",
            IncurredAtUtc = DateTime.UtcNow.AddHours(-1)
        };

        var createResp = await client.PostAsJsonAsync("/api/Expense", createRequest);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<ExpenseDto>();
        Assert.NotNull(created);

        // GET by id
        var getResp = await client.GetAsync($"/api/Expense/{created!.Id}");
        getResp.EnsureSuccessStatusCode();
        var fetched = await getResp.Content.ReadFromJsonAsync<ExpenseDto>();
        Assert.NotNull(fetched);
        Assert.Equal(createRequest.Amount, fetched!.Amount);

        // Update
        var updateRequest = new CreateExpenseRequest
        {
            Amount = 120m,
            CurrencyCode = "USD",
            Category = ExpenseCategory.Electronics,
            Description = "Headphones updated",
            IncurredAtUtc = DateTime.UtcNow
        };
        var updateResp = await client.PutAsJsonAsync($"/api/Expense/{created.Id}", updateRequest);
        updateResp.EnsureSuccessStatusCode();
        var updated = await updateResp.Content.ReadFromJsonAsync<ExpenseDto>();
        Assert.NotNull(updated);
        Assert.Equal(updateRequest.Amount, updated!.Amount);
        Assert.Equal(updateRequest.Description, updated.Description);

        // Delete
        var deleteResp = await client.DeleteAsync($"/api/Expense/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var getAfterDelete = await client.GetAsync($"/api/Expense/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    [Fact]
    public async Task Expense_Filters_Return_Expected_Periods_And_Custom()
    {
        var (client, auth) = await RegisterAndLoginAsync(_factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var now = DateTime.UtcNow;
        var expenses = new[]
        {
            new CreateExpenseRequest
            {
                Amount = 10m,
                CurrencyCode = "USD",
                Category = ExpenseCategory.Groceries,
                Description = "Today",
                IncurredAtUtc = now
            },
            new CreateExpenseRequest
            {
                Amount = 20m,
                CurrencyCode = "USD",
                Category = ExpenseCategory.Utilities,
                Description = "10 days ago",
                IncurredAtUtc = now.AddDays(-10)
            },
            new CreateExpenseRequest
            {
                Amount = 30m,
                CurrencyCode = "USD",
                Category = ExpenseCategory.Leisure,
                Description = "40 days ago",
                IncurredAtUtc = now.AddDays(-40)
            }
        };

        foreach (var e in expenses)
        {
            var resp = await client.PostAsJsonAsync("/api/Expense", e);
            resp.EnsureSuccessStatusCode();
        }

        async Task<List<ExpenseDto>?> GetAsync(string query) =>
            await client.GetFromJsonAsync<List<ExpenseDto>>($"/api/Expense{query}");

        var week = await GetAsync("?period=week");
        Assert.NotNull(week);
        Assert.Single(week!); // only "Today"

        var month = await GetAsync("?period=month");
        Assert.NotNull(month);
        Assert.Equal(2, month!.Count); // Today + 10 days ago

        var threeMonths = await GetAsync("?period=3months");
        Assert.NotNull(threeMonths);
        Assert.Equal(3, threeMonths!.Count); // all three

        var start = now.AddDays(-15).ToString("o");
        var end = now.AddDays(-5).ToString("o");
        var custom = await GetAsync($"?startDate={Uri.EscapeDataString(start)}&endDate={Uri.EscapeDataString(end)}");
        Assert.NotNull(custom);
        Assert.Single(custom!); // only 10 days ago falls in range
    }
}
