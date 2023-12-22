using Microsoft.Data.SqlClient;
using System;
using System.Security.Claims;

namespace BetterFinds.Utils
{
    public class Client
    {
        private readonly IConfiguration _configuration;
        public Client(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public int GetClientId(HttpContext httpContext, ClaimsPrincipal user)
        {
            // Get ClientId from session cookie
            int ClientId = 0;
            try
            {
                ClientId = int.Parse(httpContext.Session.GetString("ClientId") ?? "");
            }
            catch (FormatException)
            {
                string? connectionString = _configuration.GetConnectionString("DefaultConnection");

                // Get ClientId from database with User.Identity.Name
                if (user.Identity?.Name != null)
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        string query = "SELECT ClientId FROM Client WHERE Username = @Username";
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@Username", user.Identity.Name);
                            ClientId = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        con.Close();
                    }
                }
            }
            return ClientId;
        }
    }
}