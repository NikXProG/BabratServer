using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Babrat.Server.REST.API.Models;
using Babrat.Server.REST.API.Settings;
using com.sun.tools.javac.jvm;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Caching.Distributed;
using org.checkerframework.common.returnsreceiver.qual;

namespace Babrat.Server.REST.API.Controller;

[Authorize]
[ApiController]
[Route("/api/auth")]
[Produces("application/json")]
public sealed class AccountController :
    ControllerBase
{
    
    #region Fields

    private readonly IDistributedCache _cache;
    
    private readonly IOptions<AuthApiSettings> _authSettings;
    
    #endregion

    #region Constructors

    public AccountController(
        IDistributedCache cache,
        IOptions<AuthApiSettings> authSettings)
    {
        _cache = cache;
        _authSettings = authSettings;
    }

    #endregion
    
    #region Endpoints
    
    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<IActionResult> Signup(
        [FromBody] LoginModelRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
          
            var result = Zxcvbn.Core.EvaluatePassword(request.Password);
            
            if (result.Score < 3) 
            {
                throw new Exception("The password is too light");
            }
            
            var accessToken = GenerateAccessToken(request.Username);
            var refreshToken = GenerateSecureToken();
            
            await _cache.SetStringAsync(
                key: $"refresh_token:{refreshToken}",
                value: request.Username,
                options: new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(_authSettings.Value.RefreshTokenLifeTime) 
                }, token: cancellationToken);
            
            return Ok(new TokenResponse
            { 
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
            
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
        }
      

   
    }
    
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginModelRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            
            var accessToken = GenerateAccessToken(request.Username);
            var refreshToken = GenerateSecureToken();
            
            await _cache.SetStringAsync(
                key: $"refresh_token:{refreshToken}",
                value: request.Username,
                options: new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(_authSettings.Value.RefreshTokenLifeTime) 
                }, token: cancellationToken);
            
            return Ok(new TokenResponse
            { 
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
            
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
        }
      

   
    }
    
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
      
        var refreshToken = Request.Headers["X-Refresh-Token"];
        
        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest("Refresh token is required");
        
        // Удаляем refresh-токен из Redis
        await _cache.RemoveAsync($"refresh_token:{refreshToken}", cancellationToken);
        return NoContent();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
  
        var username = await _cache.GetStringAsync($"refresh_token:{request.RefreshToken}");
        if (string.IsNullOrEmpty(username))
            return Unauthorized("Invalid or expired refresh token");

      
        var newAccessToken = GenerateAccessToken(username);
        
        await _cache.SetStringAsync(
            key: $"refresh_token:{request.RefreshToken}",
            value: username,
            options: new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(_authSettings.Value.RefreshTokenLifeTime)
            });

        return Ok(new { AccessToken = newAccessToken });
    }

    #endregion
    
    #region Private Methods

    private string GenerateSecureToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(_authSettings.Value.SecureTokenLength));
    }
    
    private string GenerateAccessToken(string username)
    {
        var settings = _authSettings.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: [new Claim(ClaimTypes.Name, username)],
            expires: DateTime.UtcNow.AddMinutes(settings.ExpireMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


    #endregion
 
}