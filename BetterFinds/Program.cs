using BetterFinds.Hubs;
using BetterFinds.Services;
using BetterFinds.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BetterFinds
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            builder.Services.AddAuthentication(
                CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/login";
                });

            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = false;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromMinutes(30);
            });

            // Register the Auctions service
            builder.Services.AddSingleton<Auctions>();

            // Register the AuctionBackgroundService as a hosted service
            builder.Services.AddHostedService<AuctionBackgroundService>();

            // Register the Bidders groups service
            builder.Services.AddSingleton<Bids>();

            // Register SignalR
            builder.Services.AddSignalR();

            var app = builder.Build();

            app.UseStatusCodePages();
            app.UseExceptionHandler("/error");
            // app.UseStatusCodePagesWithRedirects("/errors/{0}");
            app.UseStatusCodePagesWithReExecute("/errors/{0}");

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                // Use HTTPS
                app.UseHttpsRedirection();

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            // app.UseCookiePolicy();

            app.UseAuthorization();

            app.UseSession();

            app.MapRazorPages();

            // Initialize Auctions
            var auctions = app.Services.GetRequiredService<Auctions>();
            auctions.CreateAuctionsToCheck();

            // Initialize Bidder Groups
            var bids = app.Services.GetRequiredService<Bids>();
            await bids.CreateBidderGroup();

            // Map SignalR hub
            app.MapHub<NotificationHub>("/notificationHub");

            Console.WriteLine("Starting application...");

            await app.RunAsync();
        }
    }
}