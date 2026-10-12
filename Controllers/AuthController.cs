using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RondiTrack.DTOs.Authorization;
using RondiTrack.Models;
using RondiTrack.Repositories;
using RondiTrack.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RondiTrack.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IRondiTrackRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<User> _passwordHasher = new();

    // Inject the repository and configuration.
    public AuthController(
        IRondiTrackRepository repository,
        IConfiguration configuration)
    {
        _repository = repository;
        _configuration = configuration;
    }

    // Register a new user.
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        // Require passwords of at least 12 characters.
        if (string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 12)
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Invalid password",
                Detail = "Password must contain at least 12 characters."
            });
        }

        // Check for an existing email.
        var existingUser = await _repository.GetUserByEmailAsync(request.Email);

        if (existingUser is not null)
        {
            return Conflict(new ProblemDetails
            {
                Status = 409,
                Title = "Email already registered"
            });
        }

        // Create the user.
        var user = new User(
            request.Id,
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber
        );

        // Hash the password.
        var passwordHash = _passwordHasher.HashPassword(
            user,
            request.Password
        );

        user.SetPasswordHash(passwordHash);

        // Save the user.
        await _repository.AddUserAsync(user);

        // Return user details without the password hash.
        return StatusCode(201, new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email
        });
    }

    // Log in and issue access and refresh tokens.
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        // Find the user.
        var user = await _repository.GetUserByEmailAsync(request.Email);

        // Reject unknown users or missing password hashes.
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = 401,
                Title = "Invalid credentials"
            });
        }

        // Verify the password.
        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        // Reject incorrect passwords.
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = 401,
                Title = "Invalid credentials"
            });
        }

        // Generate a 15-minute access token.
        var accessToken = GenerateAccessToken(user);

        // Generate a secure refresh token.
        var refreshToken = RefreshTokenHelper.GenerateToken();

        // Store only the refresh-token hash.
        var storedToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = RefreshTokenHelper.HashToken(refreshToken),
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            IsUsed = false
        };

        // Save the refresh token.
        await _repository.AddRefreshTokenAsync(storedToken);

        // Store the refresh token in an HttpOnly, Secure cookie.
        Response.Cookies.Append(
            "refreshToken",
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = storedToken.ExpiresAt,
                Path = "/api/auth"
            });

        // Return the access token.
        return Ok(new
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = 900
        });
    }

    // Rotate a refresh token and issue a new access token.
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        // Read the refresh-token cookie.
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken) ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = 401,
                Title = "Invalid refresh token"
            });
        }

        // Find the stored token using its hash.
        var tokenHash = RefreshTokenHelper.HashToken(refreshToken);
        var storedToken = await _repository.GetRefreshTokenByHashAsync(tokenHash);

        // Reject unknown tokens.
        if (storedToken is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = 401,
                Title = "Invalid refresh token"
            });
        }

        // Detect reuse of an already consumed or revoked token.
        if (storedToken.IsUsed || storedToken.IsRevoked)
        {
            // Revoke the entire refresh-token family.
            var familyTokens = await _repository.GetRefreshTokensByFamilyAsync(
                storedToken.FamilyId);

            foreach (var token in familyTokens)
            {
                token.IsRevoked = true;
            }

            await _repository.SaveChangesAsync();

            return Unauthorized(new ProblemDetails
            {
                Status = 401,
                Title = "Invalid refresh token"
            });
        }

        // Reject expired tokens.
        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = 401,
                Title = "Invalid refresh token"
            });
        }

        // Find the token's owner.
        var user = await _repository.GetUserByIdAsync(storedToken.UserId);

        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = 401,
                Title = "Invalid refresh token"
            });
        }

        // Generate a new refresh token.
        var newRefreshToken = RefreshTokenHelper.GenerateToken();

        // Create the replacement within the same family.
        var replacementToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = RefreshTokenHelper.HashToken(newRefreshToken),
            FamilyId = storedToken.FamilyId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            IsUsed = false
        };

        // Consume the old token and save its replacement atomically.
        var rotated = await _repository.RotateRefreshTokenAsync(
            tokenHash,
            replacementToken);

        // Reject a token that another request already consumed.
        if (!rotated)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = 401,
                Title = "Invalid refresh token"
            });
        }

        // Replace the refresh-token cookie.
        Response.Cookies.Append(
            "refreshToken",
            newRefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = replacementToken.ExpiresAt,
                Path = "/api/auth"
            });

        // Return a new 15-minute access token.
        return Ok(new
        {
            AccessToken = GenerateAccessToken(user),
            TokenType = "Bearer",
            ExpiresIn = 900
        });
    }

    // Log out and revoke the entire refresh-token family.
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Read the refresh-token cookie.
        if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken) &&
            !string.IsNullOrWhiteSpace(refreshToken))
        {
            // Find the stored token using its hash.
            var tokenHash = RefreshTokenHelper.HashToken(refreshToken);
            var storedToken = await _repository.GetRefreshTokenByHashAsync(tokenHash);

            if (storedToken is not null)
            {
                // Retrieve all tokens in the same family.
                var familyTokens = await _repository.GetRefreshTokensByFamilyAsync(
                    storedToken.FamilyId);

                // Revoke every token in the family.
                foreach (var token in familyTokens)
                {
                    token.IsRevoked = true;
                }

                // Save the revoked tokens.
                await _repository.SaveChangesAsync();
            }
        }

        // Delete the refresh-token cookie.
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth"
        });

        // Return 204 even if the user was already logged out.
        return NoContent();
    }

    // Generate a signed JWT access token.
    private string GenerateAccessToken(User user)
    {
        // Read the Base64-encoded signing key.
        var key = Convert.FromBase64String(
            _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT signing key is missing."));

        // Identify the authenticated user.
        var claims = new[]
    {
        new Claim(
            JwtRegisteredClaimNames.Sub,
            user.Id.ToString()),

        new Claim(
            JwtRegisteredClaimNames.Jti,
            Guid.NewGuid().ToString()),

        new Claim(
            ClaimTypes.Role,
            user.PlatformRole)
    };

        // Sign the JWT using HMAC-SHA256.
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256);

        // Set the access-token lifetime to 15 minutes.
        var token = new JwtSecurityToken(
            issuer: "RondiTrack",
            audience: "RondiTrack",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        // Convert the JWT into a string.
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}