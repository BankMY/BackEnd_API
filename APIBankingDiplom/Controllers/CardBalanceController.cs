using APIBankingDiplom.DBClasses.DBContext;
using APIBankingDiplom.DBClasses.DBModels;
using APIBankingDiplom.DBClasses.Enums;
using APIBankingDiplom.GeneralUtilities.Factories;
using APIBankingDiplom.GeneralUtilities.Models;
using APIBankingDiplom.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIBankingDiplom.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class CardBalanceController : Controller
    {
        private readonly BankingContext _bankingContext;
        public CardBalanceController(BankingContext context)
        {
            _bankingContext = context;
        }
        #region Basic Functions
        [HttpPost("AddCardBalance")]
        public async Task<IActionResult> AddCardBalanceAsync(string cardNumber, BankingEnums.Currency currency)
        {
            UserModel? user = HttpContext.Items["ValidatedUser"] as UserModel;

            return await SecurityMeasures.TryExecutingAsync(async () =>
            {
                CardModel card = await _bankingContext.GetCardAsync(cardNumber, user!.Id);
                if (card is null)
                    return BadRequest(SecurityMeasures.CardNotOwnedOrNull);

                if (await _bankingContext.GetCardBalanceAsync(card.Id, currency) is not null)
                    return ResponseFactory.Create(StatusCodes.Status403Forbidden, SecurityMeasures.GenerateErrorObject("Unable to create 2 balances with same currency."));

                await _bankingContext.CardBalances.AddAsync(new CardBalanceModel()
                {
                    Currency = BankingEnums.GetCurrencyCode(currency),
                    Balance = decimal.Zero,
                    CardId = card.Id,
                    Card = card
                });

                await _bankingContext.SaveChangesAsync();
                return Ok();
            }, null, user);
        }
        [HttpGet("GetCardBalances")]
        public async Task<IActionResult> GetCardBalancesAsync(string cardNumber)
        {
            UserModel? user = HttpContext.Items["ValidatedUser"] as UserModel;

            return await SecurityMeasures.TryExecutingAsync(async () =>
            {
                CardModel card = await _bankingContext.GetCardAsync(cardNumber, user!.Id);
                if (card is null)
                    return BadRequest(SecurityMeasures.CardNotOwnedOrNull);

                List<CardBalanceModel> balances = await _bankingContext.CardBalances.Where(b => b.CardId == card.Id).ToListAsync();
                return Ok(new { balancesList = balances });
            }, null, user);
        }
        [HttpDelete("RemoveCardBalance")]
        public async Task<IActionResult> RemoveCardBalanceAsync(string cardNumber, BankingEnums.Currency currency)
        {
            UserModel? user = HttpContext.Items["ValidatedUser"] as UserModel;

            return await SecurityMeasures.TryExecutingAsync(async () =>
            {
                AvailabilityResult<CardModel> result = await _bankingContext.CheckCardAvailability(cardNumber, user!.Id);
                if (!result.IsSuccess)
                    return result.Response;

                CardModel card = result.Item!;

                CardBalanceModel balance = await _bankingContext.GetCardBalanceAsync(card.Id, currency);
                if (balance is null)
                    return BadRequest(SecurityMeasures.CardBalanceDoesNotExist);

                if (balance.Balance != decimal.Zero)
                    return ResponseFactory.Create(StatusCodes.Status403Forbidden, SecurityMeasures.GenerateErrorObject("Cannot remove non-empty balance."));

                _bankingContext.CardBalances.Remove(balance);

                await _bankingContext.SaveChangesAsync();
                return Ok();
            }, null, user);
        }
        #endregion
        #region Money operations
        // Those are placeholders - I'll replace those with actual life-like operations with ATMs
        [HttpPatch("Deposit")]
        public async Task<IActionResult> DepositAsync(string cardNumber, BankingEnums.Currency currency, decimal money)
        {
            ObjectResult result = _bankingContext.IsMoneyAmountValid(money);
            if (result.StatusCode is not StatusCodes.Status200OK)
                return result;

            UserModel? user = HttpContext.Items["ValidatedUser"] as UserModel;
            return await SecurityMeasures.TryExecutingAsync(async () =>
            {
                AvailabilityResult<CardBalanceModel> balanceResult = await _bankingContext.CheckCardBalanceAvailability(cardNumber, user!.Id, currency);
                if (!balanceResult.IsSuccess)
                    return balanceResult.Response;

                CardBalanceModel balance = balanceResult.Item!;
                balance.Balance = balance.Balance + money;

                await _bankingContext.SaveChangesAsync();
                return Ok();
            }, null, user);
        }
        [HttpPatch("Withdraw")]
        public async Task<IActionResult> WithdrawAsync(string cardNumber, BankingEnums.Currency currency, decimal money)
        {
            ObjectResult result = _bankingContext.IsMoneyAmountValid(money);
            if (result.StatusCode is not StatusCodes.Status200OK)
                return result;

            UserModel? user = HttpContext.Items["ValidatedUser"] as UserModel;
            return await SecurityMeasures.TryExecutingAsync(async () =>
            {
                AvailabilityResult<CardBalanceModel> balanceResult = await _bankingContext.CheckCardBalanceAvailability(cardNumber, user!.Id, currency, money);
                if (!balanceResult.IsSuccess)
                    return balanceResult.Response;

                CardBalanceModel balance = balanceResult.Item!;
                balance.Balance = balance.Balance - money;

                await _bankingContext.SaveChangesAsync();
                return Ok();
            }, null, user);
        }
        #endregion
    }
}
