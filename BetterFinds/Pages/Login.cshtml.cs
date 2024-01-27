using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace BetterFinds.Pages
{
    /// <summary>
    /// Model for the **Login** page.
    /// </summary>
    public class LoginModel : PageModel
    {
        /// <summary>
        /// The IConfiguration instance.
        /// </summary>
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginModel"/> class.
        /// </summary>
        /// <param name="configuration">The IConfiguration instance.</param>
        public LoginModel(IConfiguration configuration) =>
            this.configuration = configuration;

        /// <summary>
        /// The username entered by the user.
        /// </summary>
        [BindProperty]
        public string Username { get; set; } = "";

        /// <summary>
        /// The password entered by the user.
        /// </summary>
        [BindProperty]
        public string Password { get; set; } = "";

        /// <summary>
        /// Whether the user wants to stay logged in.
        /// </summary>
        [BindProperty]
        public bool RememberMe { get; set; } = false;

        /// <summary>
        /// The action that occurs when the user visits the login page.
        /// </summary>
        /// <remarks>
        /// Checks if the user is already logged in. If so, redirects to the index page.
        /// If the user got redirected from logout, displays a success logout message.
        /// If the user got redirected from register, enters the username they just created and
        /// displays a success register message.
        /// </remarks>
        /// <returns>A task that represents the action of loading the login page.<returns>
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

        /// <summary>
        /// The action that occurs when the user submits the login form.
        /// </summary>
        /// <remarks>
        /// Checks if the username and password are valid. If so, logs the user in and redirects
        /// to the index page. If not, displays an error message.
        /// Manually creates the cookie to store the ClientId in the session.
        /// </remarks>
        /// <returns>A task that represents the asynchronous operation of logging in.</returns>
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

                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return Page();
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
}