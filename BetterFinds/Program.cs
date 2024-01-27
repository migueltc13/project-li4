using BetterFinds.Hubs;
using BetterFinds.Services;
using BetterFinds.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;

namespace BetterFinds
{
    /// <summary>
    /// The main entry point of the application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main entry point of the application.
        /// </summary>
        /// <remarks>
        /// It's responsible for configuring services used by the application and starting it.
        /// <para/>
        /// Configuration includes:
        /// <list type="bullet">
        ///     <item>Razor pages</item>
        ///     <item>Authentication</item>
        ///     <item>Session</item>
        ///     <item>Localization</item>
        ///     <item>Auctions background service</item>
        ///     <item>Bidders groups service</item>
        ///     <item>SignalR</item>
        ///     <item>Error handling and page redirection</item>
        /// </list>
        /// Additionally, it initializes the Auctions and Bidders groups services, and maps the SignalR hub.
        /// <para/>
        /// The Auction background service is started by the <see cref="Services.AuctionBackgroundService"/> class and
        /// will run asynchronously throughout the application's lifetime to monitor ending auctions.
        /// </remarks>
        /// <param name="args">The command line arguments.</param>
        /// <returns>A task that represents the asynchronous operation of running the application.</returns>
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            builder.Services.AddAuthentication(
                CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/login";
                });

            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = false;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromDays(7);
            });

            // Add localization and configure the supported cultures
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new System.Globalization.CultureInfo("pt-PT"),
                    new System.Globalization.CultureInfo("en-GB"),
                    new System.Globalization.CultureInfo("en-US"),
                    new System.Globalization.CultureInfo("es-ES"),
                    new System.Globalization.CultureInfo("fr-FR"),
                    // ...
                };

                options.DefaultRequestCulture = new RequestCulture("pt-PT");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            // Register the Auctions service
            builder.Services.AddSingleton<Auctions>();

            // Register the AuctionBackgroundService as a hosted service
            builder.Services.AddHostedService<AuctionBackgroundService>();

            // Register the Bidders groups service
            builder.Services.AddSingleton<Bids>();

            // Register SignalR
            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            }).AddHubOptions<NotificationHub>(options =>
            {
                options.EnableDetailedErrors = true;
            });

            var app = builder.Build();

            app.UseStatusCodePages();
            app.UseExceptionHandler("/error");
            // app.UseStatusCodePagesWithRedirects("/errors/{0}");
            app.UseStatusCodePagesWithReExecute("/errors/{0}");

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Use HTTPS - nginx will handle this
            // app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            // app.UseCookiePolicy();

            app.UseAuthorization();

            app.UseSession();

            app.MapRazorPages();

            app.UseRequestLocalization();

            // Initialize Auctions
            var auctions = app.Services.GetRequiredService<Auctions>();
            auctions.CreateAuctionsToCheck();

            // Initialize Bidder Groups
            var bids = app.Services.GetRequiredService<Bids>();
            await bids.CreateBidderGroupAsync();

            // Map SignalR hub
            app.MapHub<NotificationHub>("/notificationHub");

            Console.WriteLine("Starting application...");

            await app.RunAsync();
        }
    }
}
