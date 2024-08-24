using APIBankingDiplom.Security;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using APIBankingDiplom.DBClasses.DBContext;
using Microsoft.EntityFrameworkCore;
using APIBankingDiplom.DBClasses.DBModels;
using APIBankingDiplom.GeneralUtilities.Factories;

namespace APIBankingDiplom.Filters
{
    public class ValidateIdentityFilter : IAsyncActionFilter
    {
        private readonly BankingContext _bankingContext;
        private const string BearerAuthSuffix = "Bearer ";
        private const string ValidatedUserKey = "ValidatedUser";
        public ValidateIdentityFilter(BankingContext bankingContext)
        {
            _bankingContext = bankingContext;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
            {
                await next();
                return;
            }

            string? authHeader = context.HttpContext.Request.Headers["Authorization"];
            if (authHeader?.StartsWith(BearerAuthSuffix) is true)
            {
                // Validation that the JWT token sent is not invalidated in database
                authHeader = SecurityMeasures.HashString(authHeader.Substring(BearerAuthSuffix.Length).Trim());

                UserValidationResult validationResult = UserValidationResult.Failure(SecurityMeasures.UserIsNull);
                InvalidatedToken? token = null;
                await SecurityMeasures.TryExecutingAsync(async () =>
                {
                    validationResult = await SecurityMeasures.ValidateIdentityAsync(_bankingContext, context.HttpContext.User);
                    token = await _bankingContext.InvalidatedTokens.FirstOrDefaultAsync(t => t.TokenHash.Equals(authHeader));
                    return true;
                }, 
                async (Exception e) => {
                    // add logging later.. generally this whole thing needs a lot of logging

                    context.Result = ResponseFactory.Create(StatusCodes.Status500InternalServerError, SecurityMeasures.InternalServerError);
                });
                
                if (token != null)
                {
                    context.Result = new UnauthorizedObjectResult(SecurityMeasures.GenerateErrorObject("This token has been invalidated"));
                    return;
                }

                if (!validationResult.IsValid)
                {
                    context.Result = new UnauthorizedObjectResult(validationResult.ErrorObject);
                    return;
                }

                context.HttpContext.Items[ValidatedUserKey] = validationResult.User;
            }
            await next();
        }
    }
}
