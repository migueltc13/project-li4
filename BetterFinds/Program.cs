using Microsoft.AspNetCore.Authentication.Cookies;
using BetterFinds.Services;
using BetterFinds.Utils;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();

        builder.Services.AddAuthentication(
            CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => {
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/AccessDenied"; // TODO: Adjust the access denied path
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

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
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

        Console.WriteLine("Application started.");

        await app.RunAsync();
    }
}