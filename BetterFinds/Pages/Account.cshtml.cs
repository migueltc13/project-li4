using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;

namespace BetterFinds.Pages
{
    [Authorize]
    public class AccountModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public AccountModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            // Get username from User.Identity.Name
            string username = User.Identity?.Name ?? "";

            // Get user info from database
            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            SqlConnection con = new SqlConnection(connectionString);
            string query = "SELECT * FROM Client WHERE Username = @Username";
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Username", username);

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            reader.Read();
            ViewData["Username"] = reader.GetString(reader.GetOrdinal("Username"));
            ViewData["FullName"] = reader.GetString(reader.GetOrdinal("FullName"));
            ViewData["Email"] = reader.GetString(reader.GetOrdinal("Email"));
            ViewData["OptNewsletter"] = reader.GetBoolean(reader.GetOrdinal("OptNewsletter")) ? "Yes" : "No";

            con.Close();
            // TODO (optional) option to change account options
        }
    }
}
