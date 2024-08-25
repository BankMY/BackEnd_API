using Microsoft.AspNetCore.Mvc;

namespace APIBankingDiplom.GeneralUtilities.Factories
{
    public static class ResponseFactory
    {
        public static readonly ObjectResult Success = Create(StatusCodes.Status200OK);
        public static ObjectResult Create(int statusCode, object? value = null)
        {
            return new ObjectResult(value)
            {
                StatusCode = statusCode
            };
        }
    }
}
