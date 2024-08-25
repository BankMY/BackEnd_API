using APIBankingDiplom.DBClasses.DBContext;
using APIBankingDiplom.DBClasses.DBModels;
using APIBankingDiplom.DBClasses.Enums;
using APIBankingDiplom.GeneralUtilities.Models;
using APIBankingDiplom.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

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
        [HttpPost("TransferMoney")]
        // TODO: Implement multicurrency transactions using https://www.exchangerate-api.com/
        public async Task<IActionResult> TransferMoney(string senderCardNumber, string receiverCardNumber, BankingEnums.Currency currency, decimal money)
        {
            if (senderCardNumber.Equals(receiverCardNumber))
                return BadRequest(SecurityMeasures.GenerateErrorObject("Cannot transfer money to the same card"));

            UserModel? user = HttpContext.Items["ValidatedUser"] as UserModel;
            return await SecurityMeasures.TryExecutingAsync(async () =>
            {
                AvailabilityResult<CardBalanceModel> withdrawalResult = await _bankingContext.WithdrawAsync(senderCardNumber, user!.Id, currency, money);
                if (!withdrawalResult.IsSuccess)
                    return withdrawalResult.Response;

                int? receiverCardUserId = await _bankingContext.Cards.Where(c => c.CardNumber == receiverCardNumber)
                                                                     .Select(c => (int?)c.UserId)
                                                                     .FirstOrDefaultAsync();
                if (!receiverCardUserId.HasValue)
                    return BadRequest(SecurityMeasures.GenerateErrorObject("Could not find receiver card"));

                AvailabilityResult<CardBalanceModel> depositResult = await _bankingContext.DepositAsync(receiverCardNumber, receiverCardUserId.Value, currency, money);
                if (!depositResult.IsSuccess)
                    return depositResult.Response;

                // Remember this transaction for security reasons
                await _bankingContext.Transactions.AddAsync(new TransactionModel()
                {
                    TransactionType = "CardToCard",
                    Amount = money,
                    SenderCurrency = withdrawalResult.Item!.Currency,
                    ReceiverCurrency = depositResult.Item!.Currency,
                    TransactionDate = DateTime.Now,
                    Description = new StringBuilder().Append(senderCardNumber).Append(" - ").Append(receiverCardNumber).Append(" (Card-To-Card) Transaction").ToString(),
                    SenderCardId = withdrawalResult.Item.CardId,
                    SenderBalanceId = withdrawalResult.Item.Id,
                    ReceiverCardId = depositResult.Item!.CardId,
                    ReceiverBalanceId = depositResult.Item!.Id,
                });

                await _bankingContext.SaveChangesAsync();
                return Ok();
            }, null, user);
        }
    }
}
