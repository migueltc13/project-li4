using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace BetterFinds.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        [BindProperty]
        public bool RememberMe { get; set; } = false;

        private readonly IConfiguration _configuration;
        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult OnGet()
        {
            // If user is already logged in, redirect to index
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToPage("/Index");

            return Page();
        }

        // [HttpPost]
        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine($"Username: {Username}");     // TODO: Remove this
            Console.WriteLine($"Password: {Password}");     // TODO: Remove this
            Console.WriteLine($"RememberMe: {RememberMe}"); // TODO: Remove this

            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            SqlConnection con = new SqlConnection(connectionString);

            try
            {
                con.Open();

                string query = "SELECT ClientId FROM Client WHERE Username = @Username AND Password = @Password";
                SqlCommand cmd = new SqlCommand(query, con);

                // Use parameters to avoid SQL injection
                cmd.Parameters.AddWithValue("@Username", Username);
                cmd.Parameters.AddWithValue("@Password", Password);

                using SqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows && reader.Read())
                {
                    // Retrieve ClientId from database to store in session
                    string clientId = reader["ClientId"].ToString() ?? "";

                    // Login successful. Manually create the cookie
                    List <Claim> claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, Username),
                        new Claim(ClaimTypes.NameIdentifier, Username),
                        new Claim("ClientId", clientId), // save ClientId in session
                        // new Claim("OtherProperties", "Example Role")
                    };

                    ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims,
                        CookieAuthenticationDefaults.AuthenticationScheme);

                    AuthenticationProperties authProperties = new AuthenticationProperties()
                    {
                        AllowRefresh = true,
                        IsPersistent = RememberMe,
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    HttpContext.Session.SetString("ClientId", clientId);

                    return RedirectToPage("/Index");
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
                return RedirectToPage("/Login");
            }
            finally
            {
                con.Close();
            }
        }
    }
}