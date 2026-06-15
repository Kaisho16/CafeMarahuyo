using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CafeMarahuyo.Shared.Auth
{
    public static class JwtHelper
    {
        // Must match the original Node.js secret
        public const string DefaultSecret = "cafe-marahuyo-secret-key-2024";

        public static string GenerateToken(int userId, string username, string role, string displayName, string secretKey = DefaultSecret)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            // The secret must be at least 256 bits (32 bytes) for HmacSha256 in .NET.
            var paddedKey = secretKey.PadRight(32, '0');
            var key = Encoding.ASCII.GetBytes(paddedKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", userId.ToString()),
                    new Claim("username", username),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("role", role), // Keeping 'role' claim for frontend compatibility
                    new Claim("displayName", displayName)
                }),
                Expires = DateTime.UtcNow.AddHours(24),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public static void AddSharedJwtAuthentication(this IServiceCollection services, string secretKey = DefaultSecret)
        {
            var paddedKey = secretKey.PadRight(32, '0');
            var key = Encoding.ASCII.GetBytes(paddedKey);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
        }
    }
}
