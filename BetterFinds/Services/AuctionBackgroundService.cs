using BetterFinds.Utils;

namespace BetterFinds.Services
{
    /// <summary>
    /// Background service that checks for auctions that ended and notifies users
    /// </summary>
    public class AuctionBackgroundService : BackgroundService
    {
        /// <summary>
        /// The IServiceProvider instance.
        /// </summary>
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuctionBackgroundService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The IServiceProvider instance.</param>
        public AuctionBackgroundService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Executes the background service.
        /// </summary>
        /// <param name="stoppingToken">The stopping token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Create a new scope to retrieve scoped services
                using (var scope = serviceProvider.CreateScope())
                {
                    var auctions = scope.ServiceProvider.GetRequiredService<Auctions>();
                    // Check for auctions that ended notify users and update the database
                    await auctions.CheckAuctionsAsync();
                }

                // Calculate the time until the next minute after the CheckAuctionsAsync execution
                DateTime now = DateTime.UtcNow;
                DateTime nextMinute = now.AddMinutes(1).AddSeconds(-now.Second);

                // Calculate the delay until the next minute
                TimeSpan delay = nextMinute - now;

                // Add extra delay to make sure it runs exactly at the 0 second of each minute
                delay = delay.Add(TimeSpan.FromSeconds(0));

                // Use Task.Delay with the calculated delay
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}