using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Helpdesk.ServiceDefaults;

/// <summary>
/// One JWT bearer scheme, shared by every host in the system (User.API issues the tokens; the
/// Gateway and every backend validate them independently rather than trusting the Gateway blindly).
/// The FallbackPolicy means every endpoint requires a valid token unless explicitly marked
/// [AllowAnonymous] - the only one of those in the whole system is User.API's Login RPC.
/// </summary>
public static class JwtAuthExtensions
{
    public static IHostApplicationBuilder AddHelpdeskJwtAuth(this IHostApplicationBuilder builder)
    {
        var signingKey = builder.Configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var issuer = builder.Configuration["Jwt:Issuer"] ?? "helpdesk";
        var audience = builder.Configuration["Jwt:Audience"] ?? "helpdesk";

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return builder;
    }
}
