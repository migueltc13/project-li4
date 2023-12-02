using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
// using Microsoft.EntityFrameworkCore;
// using System.Data;

namespace BetterFinds.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string? Username { get; set; }

        [BindProperty]
        public string? Password { get; set; }
        public void OnGet()
        {
            // Executed when the page is requested using an HTTP GET request
        }
        public Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine($"Username: {Username}");
            Console.WriteLine($"Password: {Password}");
            SqlConnection con = new SqlConnection("Server=localhost\\MSSQLSERVER01;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;");
            con.Open();
            string query = "SELECT * FROM Users WHERE Username = @Username AND Password = @Password";
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Username", Username);
            cmd.Parameters.AddWithValue("@Password", Password);

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                // Console.WriteLine("Checking credentials: " + reader.Read());
                if (reader.Read())
                {
                    Console.WriteLine("Correct credentials");
                    return Task.FromResult<IActionResult>(RedirectToPage("/Index"));
                }
                else
                {
                    Console.WriteLine("Incorrect credentials");
                    return Task.FromResult<IActionResult>(RedirectToPage("/Login"));
                }
            }
        }
    }
}