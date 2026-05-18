using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace TechInventory.IntegrationTests.Auth;

public sealed class TestJwtBuilder
{
    private readonly List<Claim> _claims = [];
    private readonly SigningCredentials _signingCredentials;
    private readonly string _issuer;
    private readonly string _audience;
    private DateTime? _expires;

    public TestJwtBuilder(SecurityKey? signingKey = null, string? issuer = null, string? audience = null)
    {
        signingKey ??= CreateTestSigningKey();
        _issuer = issuer ?? "https://login.microsoftonline.com/test-tenant-id/v2.0";
        _audience = audience ?? "api://test-client-id";
        _signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);
    }

    public TestJwtBuilder WithClaim(string type, string value)
    {
        _claims.Add(new Claim(type, value));
        return this;
    }

    public TestJwtBuilder WithOid(string oid)
    {
        _claims.Add(new Claim("oid", oid));
        return this;
    }

    public TestJwtBuilder WithSubject(string sub)
    {
        _claims.Add(new Claim("sub", sub));
        return this;
    }

    public TestJwtBuilder WithName(string name)
    {
        _claims.Add(new Claim("name", name));
        return this;
    }

    public TestJwtBuilder WithEmail(string email)
    {
        _claims.Add(new Claim("email", email));
        return this;
    }

    public TestJwtBuilder WithRoles(params string[] roles)
    {
        var rolesJson = System.Text.Json.JsonSerializer.Serialize(roles);
        _claims.Add(new Claim("roles", rolesJson));
        return this;
    }

    public TestJwtBuilder WithExpiry(DateTime expires)
    {
        _expires = expires;
        return this;
    }

    public TestJwtBuilder Expired()
    {
        _expires = DateTime.UtcNow.AddMinutes(-10);
        return this;
    }

    public string Build()
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(_claims),
            Expires = _expires ?? DateTime.UtcNow.AddHours(1),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = _signingCredentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static RsaSecurityKey CreateTestSigningKey()
    {
        var rsa = RSA.Create(2048);
        return new RsaSecurityKey(rsa);
    }
}
