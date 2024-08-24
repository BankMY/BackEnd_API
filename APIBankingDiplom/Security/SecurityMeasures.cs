using APIBankingDiplom.DBClasses.DBContext;
using APIBankingDiplom.DBClasses.DBModels;
using APIBankingDiplom.GeneralUtilities.Factories;
using APIBankingDiplom.JWT;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace APIBankingDiplom.Security
{
    public static class SecurityMeasures
    {
        #region General User Data methods
        private const int _maxLoginLength = 25;
        private const int _maxPasswordLength = 100;
        public static bool ProcessRegistrationData(string login, string password, string email, string phonenumber)
        {
            if (login.IsNullOrEmpty()
                || password.IsNullOrEmpty()
                || email.IsNullOrEmpty()
                || phonenumber.IsNullOrEmpty()
                || !IsPhoneNumber(phonenumber))
                return false;

            login = login.Substring(0, Math.Min(login.Length, _maxLoginLength));
            password = password.Substring(0, Math.Min(password.Length, _maxPasswordLength));

            return true;
        }
        public static bool IsPhoneNumber(string number)
        {
            return Regex.Match(number, @"^(\+?\d{1,4}[\s-]?)?\(?\d{1,4}\)?[\s-]?\d{1,4}[\s-]?\d{1,9}$").Success;
        }
        #endregion
        #region Hashing and Encrypting data
        // TODO: replace with more robust encryption and implement encrypting and decrypting using AES..
        public static string HashString(string stringToHash)
        {
            var sha512 = System.Security.Cryptography.SHA512.Create();
            byte[] bytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));

            return Convert.ToBase64String(bytes);
        }
        #endregion
        #region Validation of User Identity
        private static readonly object Validation_NotAuthenticated = GenerateErrorObject("User is not authenticated");
        private static readonly object Validation_InvalidIDFormat = GenerateErrorObject("Invalid ID Format");
        private static readonly object Validation_UserIsNull = GenerateErrorObject("User is NULL");
        public static async Task<UserValidationResult> ValidateIdentityAsync(BankingContext _bankingContext, ClaimsPrincipal InvalidatedUser)
        {
            if (InvalidatedUser.Identity is null || !InvalidatedUser.Identity.IsAuthenticated)
                return UserValidationResult.Failure(Validation_NotAuthenticated);

            int id;
            if (!int.TryParse(InvalidatedUser.FindFirstValue("id"), out id))
                return UserValidationResult.Failure(Validation_InvalidIDFormat);

            UserModel? user = await _bankingContext.Users.FirstOrDefaultAsync(v => v.Id == id);
            if (user is null)
                return UserValidationResult.Failure(Validation_UserIsNull);

            return UserValidationResult.Success(user);
        }
        #endregion
        #region Generate Error Object
        public static readonly object InternalServerError = GenerateErrorObject("Internal server error has occured.");
        public static readonly object UserIsNull = GenerateErrorObject("User does not exist.");
        public static readonly object DatabaseConnectionIssue = GenerateErrorObject("Could not connect to database.");
        public static readonly object CardNotOwnedOrNull = GenerateErrorObject("Requested card does not exist or is not owned by User.");
        public static readonly object CardLimitHit = GenerateErrorObject("Operation exceeds card daily limit.");
        public static readonly object CardIsBlocked = GenerateErrorObject("Card is blocked.");
        public static readonly object CardIsExpired = GenerateErrorObject("Card has expired.");
        public static readonly object CardBalanceDoesNotExist = GenerateErrorObject("Card balance does not exist.");
        public static readonly object CardBalanceMaxTwoDecimals = GenerateErrorObject("Amount of money must be rounded to 2 decimals.");
        public static readonly object CardBalanceMoneyMustBePositive = GenerateErrorObject("Amout of money cannot be negative nor zero.");
        public static readonly object CardBalanceInsufficientFunds = GenerateErrorObject("Insuffient funds for this operation on chosen card balance.");
        public static object GenerateErrorObject(string errorMessage)
        {
            return new { errorText = errorMessage };
        }
        #endregion
        #region General Exception Processing
        public static void LogException(Exception e)
        {
            Console.WriteLine("Exception Message:\n" + e.Message + "\n");
            Console.WriteLine("Inner Exception Message:\n" + e.InnerException + "\n");
            Console.WriteLine("Stack Trace:\n" + e.StackTrace + "\n");
        }
        public static object GeneralExceptionReply(Exception e, UserModel? user)
        {
            return user is null ? UserIsNull : DatabaseConnectionIssue;
        }
        public static async Task<IActionResult> TryExecutingAsync(Func<Task<IActionResult>> action, Func<Exception, object?, Task>? onException, UserModel? user, object? errorObject = null)
        {
            try
            {
                return await action();
            }
            catch (Exception e)
            {
                if (onException is not null)
                    await onException(e, errorObject);

                LogException(e);
                return ResponseFactory.Create(StatusCodes.Status500InternalServerError, errorObject is null ? GeneralExceptionReply(e, user) : errorObject);
            }
        }
        public static async Task<bool> TryExecutingAsync(Func<Task<bool>> action, Func<Exception, Task>? onException = null)
        {
            try
            {
                return await action();
            }
            catch (Exception e)
            {
                if (onException is not null)
                    await onException(e);

                LogException(e);
                return false;
            }
        }
        public static async Task TryExecutingAsync(Func<Task> action, Func<Exception, Task>? onException = null)
        {
            try
            {
                await action();
            }
            catch (Exception e)
            {
                if (onException is not null)
                    await onException(e);

                LogException(e);
            }
        }
        #endregion
        #region JWT Token workings
        private static JwtSecurityToken GenerateJWTToken(SymmetricSecurityKey secretKey, string issuer, string audience, TimeSpan expiration, IEnumerable<Claim>? identity = null)
        {
            DateTime now = DateTime.UtcNow;
            return new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    notBefore: now,
                    claims: identity,
                    expires: now.Add(expiration),
                    signingCredentials: new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha512));
        }
        public static JwtSecurityToken GenerateAccessToken(IEnumerable<Claim> identity)
        {
            return GenerateJWTToken(AuthOptions.GetSymmetricSecurityKey(), AuthOptions.ISSUER, AuthOptions.AUDIENCE, AuthOptions.GetJWTTokenLifeTime(), identity);
        }
        public static (JwtSecurityToken, JwtSecurityToken) GenerateAccessAndRefreshToken(IEnumerable<Claim> identity)
        {
            return (GenerateAccessToken(identity),
                    GenerateJWTToken(AuthOptions.GetSymmetricRefreshSecurityKey(), AuthOptions.ISSUER, AuthOptions.AUDIENCE, AuthOptions.GetRefreshTokenLifetime()));
        }
        public static DateTime GetJWTExpireTime(string jwtToken)
        {
            return new JwtSecurityTokenHandler().ReadToken(jwtToken).ValidTo;
        }
        public static ClaimsIdentity? GenerateIdentity(UserModel user)
        {
            if (user is null)
                return null;

            List<Claim> claims = new List<Claim>
                {
                    new Claim("id", user.Id.ToString()),
                    new Claim("login", user.Login),
                    new Claim("passwordhash", user.PasswordHash),
                    new Claim("email", user.Email),
                    new Claim("phonenumber", user.PhoneNumber)
                };

            return new ClaimsIdentity(claims, "Token");
        }
        #endregion
        #region Sending Emails
        private static string APIEmail_Address = Environment.GetEnvironmentVariable("API_EMAIL_ADDRESS") ?? throw new InvalidOperationException("API_EMAIL_ADDRESS is missing!");
        private static string APIEmail_Password = Environment.GetEnvironmentVariable("API_EMAIL_PASSWORD") ?? throw new InvalidOperationException("API_EMAIL_PASSWORD is missing!");
        private static string APIEmail_SubjectPrefix = "Veridion Bank: ";
        public static async Task SendEmailAsync(string toEmailAddress, string subject, string body)
        {
            // Let it be like this for now
            await TryExecutingAsync(async () =>
                {
                MailMessage mailMessage = new MailMessage()
                {
                    From = new MailAddress(APIEmail_Address),
                    Subject = APIEmail_SubjectPrefix + subject,
                    Body = body,
                };
                mailMessage.To.Add(toEmailAddress);

                SmtpClient client = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(APIEmail_Address, APIEmail_Password),
                    EnableSsl = true
                };

                await client.SendMailAsync(mailMessage);
            });
        }
        #endregion
    }
}
