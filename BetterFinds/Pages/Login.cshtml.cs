using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace BetterFinds.Pages;

public class LoginModel(IConfiguration configuration) : PageModel
{
    [BindProperty]
    public string Username { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    [BindProperty]
    public bool RememberMe { get; set; } = false;

    public IActionResult OnGet()
    {
        // If user is already logged in, redirect to index
        if (User.Identity != null && User.Identity.IsAuthenticated)
            return RedirectToPage("index");

        // pass message to page via action parameter: logout=1
        if (Request.Query.ContainsKey("logout"))
        {
            ViewData["Message"] = "You successfully logged out.";
        }

        // pass message to page via action parameter: success=1
        if (Request.Query.ContainsKey("success"))
        {
            ViewData["Message"] = "Account created successfully.";
        }

        // enter username in the page via action parameter: username=example
        if (Request.Query.TryGetValue("username", out var username))
        {
            Username = (string?)username ?? "";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Console.WriteLine($"Username: {Username}");
        Console.WriteLine($"Password: {Password}");
        Console.WriteLine($"RememberMe: {RememberMe}");

        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        SqlConnection con = new(connectionString);

        try
        {
            con.Open();

            string query = "SELECT ClientId FROM Client WHERE Username = @Username AND Password = @Password";
            SqlCommand cmd = new(query, con);

            // Use parameters to avoid SQL injection
            cmd.Parameters.AddWithValue("@Username", Username);
            cmd.Parameters.AddWithValue("@Password", Password);

            using SqlDataReader reader = cmd.ExecuteReader();

            if (reader.HasRows && reader.Read())
            {
                // Retrieve ClientId from database to store in session
                string clientId = reader["ClientId"].ToString() ?? "";

                // Login successful. Manually create the cookie
                List<Claim> claims =
                [
                    new Claim(ClaimTypes.Name, Username),
                    new Claim(ClaimTypes.NameIdentifier, Username),
                    new Claim("ClientId", clientId), // save ClientId in session
                    // new Claim("OtherProperties", "Example Role")
                ];

                ClaimsIdentity claimsIdentity = new(claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                AuthenticationProperties authProperties = new()
                {
                    AllowRefresh = true,
                    IsPersistent = RememberMe,
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                HttpContext.Session.SetString("ClientId", clientId);

                return RedirectToPage("index");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return Page();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return RedirectToPage("login");
        }
        finally
        {
            con.Close();
        }
    }
}