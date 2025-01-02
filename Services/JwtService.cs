using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Services.JwtService;

public interface IJwtService
{
    public string generateJwtToken(string username, string role);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    // inject our project's configuration to access JwtSettings
    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string generateJwtToken(string username, string role)
    {
        var claims = new[] {
            // add username 
            new Claim(ClaimTypes.Name, username),
            // add subject - who the token is about
            new Claim(JwtRegisteredClaimNames.Sub, username),
            // add role
            new Claim(ClaimTypes.Role, role),
            // jti - jwt Id. add unique identifier for the token to avoid the same token being used twice (useful for avoiding replay attacks)
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // add the key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!));

        // create signing credentials to tell the system on how to sign the token, which key to use and algorithm
        var signingCred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // create the token
        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: signingCred
        );

        // generate the jwt string - header-payload-signature
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}