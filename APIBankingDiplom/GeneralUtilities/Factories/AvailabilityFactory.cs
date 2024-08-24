using APIBankingDiplom.GeneralUtilities.Models;
using Microsoft.AspNetCore.Mvc;

namespace APIBankingDiplom.GeneralUtilities.Factories
{
    public static class AvailabilityFactory
    {
        public static AvailabilityResult<T> Create<T>(T? item, ObjectResult result)
        {
            return new AvailabilityResult<T>
            {
                Item = item,
                Response = result
            };
        }
    }
}
