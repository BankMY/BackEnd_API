using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using APIBankingDiplom.DBClasses.DBModels;
using APIBankingDiplom.DBClasses.DBContext;
using Microsoft.EntityFrameworkCore;
using APIBankingDiplom.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using APIBankingDiplom.JWT;

namespace APIBankingDiplom.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly BankingContext _bankingContext;
        public AuthController(BankingContext context)
        {
            _bankingContext = context;
        }
        [HttpPost("Register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterAsync(string username, string password, string email, string phonenumber)
        {
            if (!SecurityMeasures.ProcessRegistrationData(username, password, email, phonenumber))
                return BadRequest(SecurityMeasures.GenerateErrorObject("All parts of registration must be clarified."));

            if (await _bankingContext.Users.FirstOrDefaultAsync(u => u.Login.Equals(username) || u.Email.Equals(email) || u.PhoneNumber.Equals(phonenumber)) is not null)
                return Conflict(SecurityMeasures.GenerateErrorObject("This username, email or phone number is already taken"));

            await SecurityMeasures.TryExecutingAsync(async () =>
            {
                await _bankingContext.Users.AddAsync(new UserModel()
                {
                    Login = username,
                    PasswordHash = SecurityMeasures.HashString(password),
                    Email = email,
                    PhoneNumber = phonenumber
                });
                await _bankingContext.SaveChangesAsync();

                return Ok();
            }, null, null, SecurityMeasures.DatabaseConnectionIssue);

            return await LoginAsync(username, password);
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginAsync(string username, string password)
        {
            ClaimsIdentity? identity = null;
            UserModel? user = null;

            await SecurityMeasures.TryExecutingAsync(async () =>
            {
                password = SecurityMeasures.HashString(password);
                user = await _bankingContext.Users.FirstOrDefaultAsync(u => u.Login.Equals(username) && u.PasswordHash.Equals(password));

                identity = SecurityMeasures.GenerateIdentity(user!);
            });

            if (identity is null)
                return BadRequest(SecurityMeasures.GenerateErrorObject("Invalid username or password, or troubles with database connectivity."));

            JwtSecurityToken accessToken, refreshToken;
            (accessToken, refreshToken) = SecurityMeasures.GenerateAccessAndRefreshToken(identity.Claims);

            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            string refreshTokenEncodedString = handler.WriteToken(refreshToken);

            bool tokensAddedToDB = await SecurityMeasures.TryExecutingAsync(async () =>
            {
                await _bankingContext.DeleteRefreshToken(user!);
                await _bankingContext.RefreshTokens.AddAsync(new RefreshToken()
                {
                    HashedToken = SecurityMeasures.HashString(refreshTokenEncodedString),
                    ExpireDate = DateTime.FromFileTime(DateTime.UtcNow.Ticks + AuthOptions.GetRefreshTokenLifetime().Ticks),
                    UserId = int.Parse(identity.FindFirst("id")!.Value)
                }); ;
                _bankingContext.ChangeUserAccessToken(user!, accessToken);

                await _bankingContext.SaveChangesAsync();
                return true;
            });

            return tokensAddedToDB ? Ok(new { accessToken = handler.WriteToken(accessToken), refreshToken = refreshTokenEncodedString }) 
                                         : StatusCode(500, SecurityMeasures.GenerateErrorObject("Could not save tokens to database!"));
        }
        [HttpPost("RefreshAccessKey")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshAsync([FromBody] RefreshRequest refresh)
        {
            if (refresh is null || refresh.RefreshToken.IsNullOrEmpty())
                return BadRequest(SecurityMeasures.GenerateErrorObject("Refresh Request is invalid"));

            // Check if Refresh Token exists in database (therefore is valid)
            JwtSecurityToken? accessToken = null;
            bool isRefreshTokenValid = await SecurityMeasures.TryExecutingAsync(async () =>
            {
                string hashedToken = SecurityMeasures.HashString(refresh.RefreshToken);

                RefreshToken? refreshToken = await _bankingContext.RefreshTokens.FirstOrDefaultAsync(t => t.HashedToken.Equals(hashedToken));
                if (refreshToken is null) return false;

                UserModel? user = await _bankingContext.Users.FirstOrDefaultAsync(u => u.Id == refreshToken.UserId);
                if (user is null) return false;

                await _bankingContext.InvalidatedTokens.AddAsync(new InvalidatedToken()
                {
                    TokenHash = user.AccessTokenHash,
                    ExpireDate = user.AccessTokenExpire
                });

                accessToken = SecurityMeasures.GenerateAccessToken(SecurityMeasures.GenerateIdentity(user)!.Claims);
                _bankingContext.ChangeUserAccessToken(user, accessToken);

                await _bankingContext.SaveChangesAsync();
                return true;
            });

            if (!isRefreshTokenValid) 
                return BadRequest(SecurityMeasures.GenerateErrorObject("Refresh Token is invalid"));

            return Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(accessToken) });
        }
        [HttpPost("Logout")]
        [Authorize]
        public async Task<IActionResult> LogoutAsync()
        {
            UserModel? user = HttpContext.Items["ValidatedUser"] as UserModel;
            return await SecurityMeasures.TryExecutingAsync(async () =>
            {
                await _bankingContext.InvalidatedTokens.AddAsync(new InvalidatedToken()
                {
                    TokenHash = user!.AccessTokenHash,
                    ExpireDate = user!.AccessTokenExpire
                });

                await _bankingContext.DeleteRefreshToken(user, true);
                return Ok();
            }, null, user);
        }
    }
}
