using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Helpdesk.ServiceDefaults;
using Helpdesk.Users.Domain.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Helpdesk.Users.Api.Security;

public class JwtTokenIssuer(IConfiguration configuration)
{
    public (string Token, DateTime ExpiresAtUtc) Issue(User user, string roleName)
    {
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var issuer = configuration["Jwt:Issuer"] ?? "helpdesk";
        var audience = configuration["Jwt:Audience"] ?? "helpdesk";

        var expiresAtUtc = DateTime.UtcNow.AddHours(8);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(HelpdeskClaimTypes.Username, user.Username),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, roleName)
        };

        if (user.TechnicianId is { } technicianId)
        {
            claims.Add(new Claim(HelpdeskClaimTypes.TechnicianId, technicianId.ToString()));
        }

        if (user.ClientId is { } clientId)
        {
            claims.Add(new Claim(HelpdeskClaimTypes.ClientId, clientId.ToString()));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
