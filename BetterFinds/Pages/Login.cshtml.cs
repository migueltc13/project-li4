using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace BetterFinds.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        private readonly IConfiguration _configuration;
        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void OnGet()
        {
            // Executed when the page is requested using an HTTP GET request
        }
        public Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine($"Username: {Username}"); // TODO: Remove this
            Console.WriteLine($"Password: {Password}"); // TODO: Remove this

            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            SqlConnection con = new SqlConnection(connectionString);

            try
            {
                con.Open();

                string query = "SELECT * FROM Users WHERE Username = @Username AND Password = @Password";
                SqlCommand cmd = new SqlCommand(query, con);

                // Use parameters to avoid SQL injection
                cmd.Parameters.AddWithValue("@Username", Username);
                cmd.Parameters.AddWithValue("@Password", Password);

                using SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    // Login successful. TODO: Store user ID in session cookie
                    return Task.FromResult<IActionResult>(RedirectToPage("/Index"));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return Task.FromResult<IActionResult>(Page());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Task.FromResult<IActionResult>(RedirectToPage("/Login"));
            }
            finally
            {
                con.Close();
            }
        }
    }
}