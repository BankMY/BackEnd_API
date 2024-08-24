using APIBankingDiplom.DBClasses.DBModels;
using Microsoft.AspNetCore.Mvc;

namespace APIBankingDiplom.GeneralUtilities.Models
{
    public class AvailabilityResult<T>
    {
        public ObjectResult Response { get; set; } = new ObjectResult(null);
        public T? Item { get; set; }
        public bool IsSuccess => Response.StatusCode == StatusCodes.Status200OK;
    }
}
