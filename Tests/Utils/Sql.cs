using Microsoft.Data.SqlClient;

namespace Tests.Utils
{
    public static class Sql
    {
        public static string GetConnection() =>
            "Server=betterfinds.pt;Database=master;User Id=sa;Password=LI4passwd2024;TrustServerCertificate=True;Encrypt=True;";

        public static void ExecuteQuery(string query)
        {
            using SqlConnection con = new(GetConnection());
            try
            {
                con.Open();
                using SqlCommand command = new(query, con);
                command.ExecuteNonQuery();
                con.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static int GetLastAuctionId()
        {
            int result = 0;
            using SqlConnection con = new(GetConnection());
            try
            {
                con.Open();
                using SqlCommand command = new("SELECT MAX(AuctionId) FROM Auction", con);
                using SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    result = reader.GetInt32(0);
                    reader.Close();
                    con.Close();
                    return result;
                }
                con.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return result;
        }

        public static int GetClientIdByUsername(string username)
        {
            int result = 0;
            using SqlConnection con = new(GetConnection());
            try
            {
                con.Open();
                using SqlCommand command = new($"SELECT ClientId FROM Client WHERE Username = '{username}'", con);
                using SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    result = reader.GetInt32(0);
                    reader.Close();
                    con.Close();
                    return result;
                }
                con.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return result;
        }
    }
}
