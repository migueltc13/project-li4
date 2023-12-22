using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

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
            // Get ClientId
            var clientUtils = new Utils.Client(_configuration);
            int ClientId = clientUtils.GetClientId(HttpContext, User);

            // Get user info from database
            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            SqlConnection con = new SqlConnection(connectionString);
            string query = "SELECT * FROM Client WHERE ClientId = @ClientId";
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@ClientId", ClientId);

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
