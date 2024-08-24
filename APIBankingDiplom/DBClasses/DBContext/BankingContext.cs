using APIBankingDiplom.DBClasses.DBModels;
using APIBankingDiplom.DBClasses.Enums;
using APIBankingDiplom.GeneralUtilities.Factories;
using APIBankingDiplom.GeneralUtilities.Models;
using APIBankingDiplom.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace APIBankingDiplom.DBClasses.DBContext
{
    public class BankingContext : DbContext
    {
        #region DB Tables
        public DbSet<UserModel> Users { get; set; }
        public DbSet<TransactionModel> Transactions { get; set; }
        public DbSet<CardModel> Cards { get; set; }
        public DbSet<CardBalanceModel> CardBalances { get; set; }
        public DbSet<NotificationModel> Notifications { get; set; }
        public DbSet<SettingModel> Settings { get; set; }
        public DbSet<LogModel> Logs { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<InvalidatedToken> InvalidatedTokens { get; set; }
        #endregion
        #region Connect to database
        private static readonly string connectionString = Environment.GetEnvironmentVariable("API_DATABASE_CONNECTION_STRING") ?? throw new InvalidOperationException("API_DATABASE_CONNECTION_STRING is missing!");
        protected override async void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(connectionString);
        }
        #endregion
        #region Setting up Foreign Keys and Decimals
        // Will be done like this because shadow properties allow foreign keys to be NULLs, not likeable!
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TransactionModel>()
                .HasOne(t => t.SenderBalance)
                .WithMany()
                .HasForeignKey(t => t.SenderBalanceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransactionModel>()
                .HasOne(t => t.ReceiverBalance)
                .WithMany()
                .HasForeignKey(t => t.ReceiverBalanceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransactionModel>()
                .Property(t => t.Amount)
                .HasColumnType("decimal(15,2)");

            modelBuilder.Entity<CardModel>()
                .Property(t => t.WithdrawnToday)
                .HasColumnType("decimal(15,2)");

            modelBuilder.Entity<CardModel>()
                .Property(t => t.WithdrawalLimit)
                .HasColumnType("decimal(15,2)");

            modelBuilder.Entity<CardModel>()
                .Property(t => t.TransactedToday)
                .HasColumnType("decimal(15,2)");

            modelBuilder.Entity<CardModel>()
                .Property(t => t.TransactionLimit)
                .HasColumnType("decimal(15,2)");

            modelBuilder.Entity<CardModel>()
                .Property(t => t.ExpireDate)
                .HasColumnType("DATE");

            modelBuilder.Entity<CardModel>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CardBalanceModel>()
                .HasOne(t => t.Card)
                .WithMany()
                .HasForeignKey(t => t.CardId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CardBalanceModel>()
                .Property(t => t.Balance)
                .HasColumnType("decimal(15,2)");

            modelBuilder.Entity<NotificationModel>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SettingModel>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LogModel>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RefreshToken>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
        #endregion
        #region Extended DB functions
        // Most of these functions are used in only one controller and I could place them
        // Inside their related controllers to reduce memory consumption
        // But I dont want to hinder code readability
        public void ChangeUserAccessToken(UserModel user, JwtSecurityToken accessToken)
        {
            if (user is null || accessToken is null)
                return;

            user.AccessTokenHash = SecurityMeasures.HashString(new JwtSecurityTokenHandler().WriteToken(accessToken));
            user.AccessTokenExpire = accessToken.ValidTo;
        }
        public async Task<bool> DeleteRefreshToken(UserModel user, bool saveChanges = false)
        {
            if (user is null)
                return false;

            RefreshToken? token = await RefreshTokens.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (token is null) return false;

            RefreshTokens.Remove(token);

            if (saveChanges) await SaveChangesAsync();
            return true;
        }
        public async Task<CardModel> GetCardAsync(string cardNumber, int userId)
        {
            return (await Cards.FirstOrDefaultAsync(c => c.UserId == userId && c.CardNumber.Equals(cardNumber)))!;
        }
        public async Task<CardBalanceModel> GetCardBalanceAsync(int cardId, BankingEnums.Currency currency)
        {
            string currencyCode = BankingEnums.GetCurrencyCode(currency);
            return (await CardBalances.FirstOrDefaultAsync(b => b.CardId == cardId && b.Currency.Equals(currencyCode)))!;
        }
        public async Task<bool> CardExistsAsync(string cardNumber)
        {
            return await Cards.AnyAsync(c => c.CardNumber.Equals(cardNumber));
        }
        public async Task<AvailabilityResult<CardModel>> CheckCardAvailability(string cardNumber, int userId)
        {
            CardModel card = await GetCardAsync(cardNumber, userId);

            if (card is null)
                return AvailabilityFactory.Create(card, ResponseFactory.Create(StatusCodes.Status404NotFound, SecurityMeasures.GenerateErrorObject("Card has not been found!")));

            if (card.Status is BankingEnums.CardStatus.Blocked)
                return AvailabilityFactory.Create(card, ResponseFactory.Create(StatusCodes.Status403Forbidden, SecurityMeasures.CardIsBlocked));

            if (card.Status is BankingEnums.CardStatus.Expired)
                return AvailabilityFactory.Create(card, ResponseFactory.Create(StatusCodes.Status406NotAcceptable, SecurityMeasures.CardIsExpired));

            return AvailabilityFactory.Create(card, ResponseFactory.Create(StatusCodes.Status200OK));
        }
        public async Task<AvailabilityResult<CardBalanceModel>> CheckCardBalanceAvailability(string cardNumber, int userId, BankingEnums.Currency currency, decimal? withdrawMoney = null)
        {
            AvailabilityResult<CardModel> cardResult = await CheckCardAvailability(cardNumber, userId);
            if (!cardResult.IsSuccess)
                return AvailabilityFactory.Create<CardBalanceModel>(null, cardResult.Response);

            CardBalanceModel balance = await GetCardBalanceAsync(cardResult.Item!.Id, currency);
            if (balance is null)
                return AvailabilityFactory.Create<CardBalanceModel>(null, ResponseFactory.Create(StatusCodes.Status400BadRequest, SecurityMeasures.CardBalanceDoesNotExist));

            if (withdrawMoney.HasValue && balance.Balance < withdrawMoney.Value)
                return AvailabilityFactory.Create<CardBalanceModel>(null, ResponseFactory.Create(StatusCodes.Status406NotAcceptable, SecurityMeasures.CardBalanceInsufficientFunds));

            return AvailabilityFactory.Create(balance, ResponseFactory.Create(StatusCodes.Status200OK));
        }
        public ObjectResult IsMoneyAmountValid(decimal money)
        {
            if (money != Math.Round(money, 2))
                return ResponseFactory.Create(StatusCodes.Status400BadRequest, SecurityMeasures.CardBalanceMaxTwoDecimals);

            if (money <= 0)
                return ResponseFactory.Create(StatusCodes.Status400BadRequest, SecurityMeasures.CardBalanceMoneyMustBePositive);

            return ResponseFactory.Create(StatusCodes.Status200OK);
        }
        #endregion
        #region Card Numbers Generation
        private const string IIN_START = "25";
        private const int IIN_END_MIN = 3510;
        private const int IIN_END_MAX = 3941;
        private const byte CARDNUMBER_USERID_LENGTH = 8;
        private readonly RandomNumberGenerator RNG = RandomNumberGenerator.Create();
        private short GenerateRandomNumber(short minValue, short maxValue)
        {
            if (minValue >= maxValue)
                throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater than minValue.");

            byte[] randomNumberBytes = new byte[2];
            RNG.GetBytes(randomNumberBytes);

            short randomNumber = BitConverter.ToInt16(randomNumberBytes, 0);
            return (short)(Math.Abs(randomNumber % (maxValue - minValue)) + minValue);
            // This is only used for card number and CVV, so we dont need negative values here.
        }
        private int CalculateLuhnCheckDigit(string cardNumber)
        {
            int sum = 0;
            int digit;
            bool doubleDigit = true;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                digit = int.Parse(cardNumber[i].ToString());

                if (doubleDigit)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                doubleDigit = !doubleDigit;
            }

            return (10 - (sum % 10)) % 10;
        }
        private string GetUserIdFormat()
        {
            return "D" + CARDNUMBER_USERID_LENGTH;
        }
        public async Task<string> GenerateCardNumberAsync(int userId)
        {
            if (await Users.FirstOrDefaultAsync(user => user.Id == userId) is null)
                return string.Empty;

            StringBuilder stringBuilder = new StringBuilder();
            string cardNumber;
            do
            {
                stringBuilder.Clear();
                cardNumber = stringBuilder.Append(IIN_START) // BANK ID START
                                          .Append(GenerateRandomNumber(IIN_END_MIN, IIN_END_MAX)) // Randomization, BANK ID END
                                          .Append(userId.ToString(GetUserIdFormat())) // User ID in the card
                                          .ToString();

                cardNumber = cardNumber + CalculateLuhnCheckDigit(cardNumber);
            } while (await CardExistsAsync(cardNumber));

            return cardNumber;
        }
        public string GenerateCardVerificationValue()
        {
            return GenerateRandomNumber(0, 1000).ToString("D3");
        }
        // Let's also include Personal Identification Numbers!
        public string GeneratePersonalIdentificationNumber()
        {
            return GenerateRandomNumber(0, 10000).ToString("D4");
        }
        #endregion
    }
}