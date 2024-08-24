using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace APIBankingDiplom.JWT
{
    public class AuthOptions
    {
        private AuthOptions() { }

        public static readonly string ISSUER = "VeridionBank";
        public static readonly string AUDIENCE = "VeridionUsers";
        private static readonly string KEY = Environment.GetEnvironmentVariable("API_JWT_SECRETKEY") ?? throw new InvalidOperationException("API_JWT_SECRETKEY is missing!");
        public static readonly int LIFETIME = 5; // In minutes
        public static readonly TokenValidationParameters AccessTokenValidationParams = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidIssuer = ISSUER,

            ValidateAudience = true,
            ValidAudience = AUDIENCE,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GetSymmetricSecurityKey(),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        // No need to add Token Validation Params for these, since they are validated by existing in database
        private static readonly string REFRESHKEY = Environment.GetEnvironmentVariable("API_JWT_SECRETKEY_REFRESH") ?? throw new InvalidOperationException("API_JWT_SECRETKEY_REFRESH is missing!");
        public static readonly int LIFETIME_REFRESHTOKEN = 90; // In days
        public static SymmetricSecurityKey GetSymmetricSecurityKey() // Should change to Asymmetric later
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
        public static SymmetricSecurityKey GetSymmetricRefreshSecurityKey() // Should change to Asymmetric later
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(REFRESHKEY));
        }
        public static TimeSpan GetJWTTokenLifeTime()
        {
            return TimeSpan.FromMinutes(LIFETIME);
        }
        public static TimeSpan GetRefreshTokenLifetime() 
        {
            return TimeSpan.FromDays(LIFETIME_REFRESHTOKEN);
        }
    }
}
