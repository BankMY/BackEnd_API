using APIBankingDiplom.DBClasses.DBContext;
using APIBankingDiplom.DBClasses.DBModels;
using APIBankingDiplom.Security;

namespace APIBankingDiplom.BackgroundServices
{
    public class InvalidTokensCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public InvalidTokensCleanupService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CleanUpExpiredTokens();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Run every minute
            }
        }
        private async Task CleanUpExpiredTokens()
        {
            using (IServiceScope scope = _serviceScopeFactory.CreateScope())
            {
                DateTime now = DateTime.UtcNow;
                await SecurityMeasures.TryExecutingAsync(async () =>
                {
                    BankingContext bankingContext = scope.ServiceProvider.GetRequiredService<BankingContext>();
                    IQueryable<InvalidatedToken> expiredAccessTokens = bankingContext.InvalidatedTokens.Where(t => t.ExpireDate < now);
                    bankingContext.InvalidatedTokens.RemoveRange(expiredAccessTokens);

                    IQueryable<RefreshToken> expiredRefreshTokens = bankingContext.RefreshTokens.Where(t => t.ExpireDate < now);
                    bankingContext.RefreshTokens.RemoveRange(expiredRefreshTokens);

                    await bankingContext.SaveChangesAsync();
                    return true;
                });
            }
        }
    }
}
