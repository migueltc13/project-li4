using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace BetterFinds.Utils
{
    /// <summary>
    /// Provides utility functions for handling **client-related** operations.
    /// </summary>
    public class Client
    {
        /// <summary>
        /// The IConfiguration instance.
        /// </summary>
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class.
        /// </summary>
        /// <param name="configuration">The IConfiguration instance.</param>
        public Client(IConfiguration configuration) =>
            this.configuration = configuration;

        /// <summary>
        /// Returns the ClientId of the currently logged in client.
        /// </summary>
        /// <param name="httpContext">The HttpContext instance.</param>
        /// <param name="user">The ClaimsPrincipal instance.</param>
        /// <returns>The ClientId of the currently logged in client.</returns>
        /// <example>
        /// Usage example:
        /// <code lang="csharp">
        /// var clientUtils = new ClientUtils(configuration);
        /// int clientId = clientUtils.GetClientId(HttpContext, User);
        /// </code>
        /// </example>
        /// <remarks>
        /// This method checks if the user is authenticated.
        /// <para/>
        /// If the user is authenticated, it attempts to obtain the ClientId from the session cookie.
        /// <para/>
        /// If that fails, it retrieves the ClientId from the database using the provided HttpContext
        /// and ClaimsPrincipal instances, serving as a fallback.
        /// <para/>
        /// This ensures that the ClientId is still obtained even if the session cookie is unavailable.
        /// </remarks>
        public int GetClientId(HttpContext httpContext, ClaimsPrincipal user)
        {
            // Check if user is authenticated
            if (user.Identity?.IsAuthenticated != true)
            {
                Console.WriteLine("User is not authenticated.");
                return 0;
            }

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

        /// <summary>
        /// Returns a list of all clients in the database.
        /// </summary>
        /// <remarks>
        /// This method is used to populate the list of clients in the Auction page. Avoiding the
        /// multiple database calls that would be required to obtain the clients information.
        /// The dictionary returned by this method contains the following keys:
        /// <code lang="json">
        /// {
        ///     "ClientId": int,
        ///     "FullName": string,
        ///     "Username": string
        /// }
        /// </code>
        /// </remarks>
        /// <returns>A list of all clients in the database.</returns>
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
}
