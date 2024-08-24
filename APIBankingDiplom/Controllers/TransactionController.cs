using APIBankingDiplom.DBClasses.DBContext;
using APIBankingDiplom.DBClasses.DBModels;
using APIBankingDiplom.DBClasses.Enums;
using APIBankingDiplom.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace APIBankingDiplom.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly BankingContext _bankingContext;
        public TransactionController(BankingContext context)
        {
            _bankingContext = context;
        }
        [HttpPost("CardToCardTransaction")]
        public async Task<IActionResult> TransactionAsync(string senderCardNumber, string receiverCardNumber, BankingEnums.Currency currency, decimal money)
        {
            UserModel? user = HttpContext.Items["ValidatedUser"] as UserModel;
            return await SecurityMeasures.TryExecutingAsync(async () =>
            {
                return Ok();
            }, null, user);
        }
    }
}
