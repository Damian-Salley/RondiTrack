using System.Net;
using System.Net.Http.Json;

namespace RondiTrack.Tests;

public class NegativePathTests
{
    [Fact]
    public async Task MalformedUser_Returns400ProblemJson()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

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
            "/api/v1/users",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task MissingUser_Returns404ProblemJson()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/users/99999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DuplicateUser_Returns422ProblemJson()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        var user = new
        {
            id = 9001,
            firstName = "Test",
            lastName = "User",
            email = "test@example.com",
            phoneNumber = "0123456789"
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/api/v1/users",
            user);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync(
            "/api/v1/users",
            user);

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            secondResponse.StatusCode);

        Assert.Equal(
            "application/problem+json",
            secondResponse.Content.Headers.ContentType?.MediaType);
    }
}