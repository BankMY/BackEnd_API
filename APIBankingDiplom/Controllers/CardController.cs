using APIBankingDiplom.DBClasses.DBContext;
using APIBankingDiplom.DBClasses.DBModels;
using APIBankingDiplom.DBClasses.Enums;
using APIBankingDiplom.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIBankingDiplom.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class CardController : Controller
    {
        private readonly BankingContext _bankingContext;
        public CardController(BankingContext context)
        {
            _bankingContext = context;
        }
        #region Issuing a Card
        private const byte maxCardsOnOneUser = 15;
        [HttpPost("IssueCard")]
        public async Task<IActionResult> AddCardToUser(BankingEnums.CardType cardType) 
        {
            UserModel? user = HttpContext.Items["ValidatedUser"] as UserModel;

            return await SecurityMeasures.TryExecutingAsync(async () =>
            {
                if (await _bankingContext.Cards.CountAsync(c => c.UserId == user!.Id && c.Status != BankingEnums.CardStatus.Expired) >= maxCardsOnOneUser)
                    return Conflict(SecurityMeasures.GenerateErrorObject("You have reached maximum card amount."));

                // Issue User new PIN every time a new card has been issued
                string PIN = _bankingContext.GeneratePersonalIdentificationNumber();
                user!.PIN = BCrypt.Net.BCrypt.EnhancedHashPassword(PIN);

                // Expire Date and Status are already predetermined in DBClasses/DBModels/CardModel.cs
                await _bankingContext.Cards.AddAsync(new CardModel()
                {
                    CardNumber = await _bankingContext.GenerateCardNumberAsync(user!.Id),
                    CVV = _bankingContext.GenerateCardVerificationValue(),
                    UserId = user.Id,
                    User = user,
                });
                await SecurityMeasures.SendEmailAsync(user.Email, "Your PIN (Personal Identification Number) has been updated!", "New PIN: " + PIN);

                await _bankingContext.SaveChangesAsync();
                return Ok();
            }, null, user);
        }
        #endregion
        #region Get User Cards
        [HttpGet("GetUserCards")]
        public async Task<IActionResult> GetUserCardsAsync()
        {
            UserModel? user = HttpContext.Items["ValidatedUser"] as UserModel;

            return await SecurityMeasures.TryExecutingAsync(async () =>
            {
                return Ok(new { cardsList = await _bankingContext.Cards.Where(c => c.UserId == user!.Id).ToListAsync() });
            }, null, user);
        }
        #endregion
        #region Set Card Status
        [HttpPatch("SetCardStatus")]
        public async Task<IActionResult> ChangeCardStatusAsync(string cardNumber, BankingEnums.CardStatus status)
        {
            if (status is BankingEnums.CardStatus.Expired)
                return BadRequest(SecurityMeasures.GenerateErrorObject("User cannot expire cards."));

            UserModel? user = HttpContext.Items["ValidatedUser"] as UserModel;

            return await SecurityMeasures.TryExecutingAsync(async () =>
            {
                CardModel card = await _bankingContext.GetCardAsync(cardNumber, user!.Id);
                if (card is null)
                    return BadRequest(SecurityMeasures.CardNotOwnedOrNull);

                card.Status = status;
                await _bankingContext.SaveChangesAsync();

                return Ok();
            }, null, user);
        }
        #endregion
    }
}
