using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace BetterFinds.Utils;

public class Client(IConfiguration configuration)
{
    public int GetClientId(HttpContext httpContext, ClaimsPrincipal user)
    {
        // Get ClientId from session cookie
        if (int.TryParse(httpContext.Session.GetString("ClientId"), out int clientId))
        {
            Console.WriteLine("Obtained ClientId from session cookie.");
            return clientId;
        }

        Console.WriteLine("Couldn't obtain ClientId from session cookie, getting it from database.");
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        // Get ClientId from database with User.Identity.Name
        if (user.Identity?.Name != null)
        {
            using SqlConnection con = new(connectionString);
            con.Open();
            string query = "SELECT ClientId FROM Client WHERE Username = @Username";
            using (SqlCommand cmd = new(query, con))
            {
                cmd.Parameters.AddWithValue("@Username", user.Identity.Name);
                clientId = Convert.ToInt32(cmd.ExecuteScalar());
            }
            con.Close();
        }

        return clientId;
    }

    public List<Dictionary<string, object>> GetClients()
    {
        List<Dictionary<string, object>> clients = [];
        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        using (SqlConnection con = new(connectionString))
        {
            con.Open();
            string query = "SELECT ClientId, FullName, Username FROM Client";
            using (SqlCommand cmd = new(query, con))
            {
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Dictionary<string, object> client = new()
                    {
                            { "ClientId", reader.GetInt32(0) },
                            { "FullName", reader.GetString(1) },
                            { "Username", reader.GetString(2) }
                        };
                    clients.Add(client);
                }
            }
            con.Close();
        }
        return clients;
    }
}