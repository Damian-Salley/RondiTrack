using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RondiTrack.Tests;

public class NegativePathTests
{
    // Malformed request should return 400
    [Fact]
    public async Task MalformedUser_Returns400ProblemJson()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var client = factory.CreateClient();

        var request = new
        {
            id = -1,
            firstName = "",
            lastName = "",
            email = "not-an-email",
            phoneNumber = ""
        };

        var response = await client.PostAsJsonAsync(
            "/api/users",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    // Missing resource should return 404
    [Fact]
    public async Task MissingUser_Returns404ProblemJson()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/users/99999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    // Business rule violation should return 422
    [Fact]
    public async Task DuplicateUser_Returns422ProblemJson()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var client = factory.CreateClient();

        var user = new
        {
            id = 9001,
            firstName = "Test",
            lastName = "User",
            email = "test@example.com",
            phoneNumber = "0123456789"
        };

        // First request creates the user
        var firstResponse = await client.PostAsJsonAsync(
            "/api/users",
            user);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        // Second request uses the same ID
        var secondResponse = await client.PostAsJsonAsync(
            "/api/users",
            user);

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            secondResponse.StatusCode);

        Assert.Equal(
            "application/problem+json",
            secondResponse.Content.Headers.ContentType?.MediaType);
    }
}