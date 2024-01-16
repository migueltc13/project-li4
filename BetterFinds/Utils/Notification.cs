﻿using Microsoft.Data.SqlClient;

namespace BetterFinds.Utils
{
    public class Notification
    {
        private readonly IConfiguration _configuration;
        public Notification(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void CreateNotification(int clientId, int auctionId, string message)
        {
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Get NotificationId
            int notificationId = 1; // Default value
            using (SqlConnection con = new(connectionString))
            {
                con.Open();
                string query = "SELECT MAX(NotificationId) FROM Notification";
                using (SqlCommand cmd = new(query, con))
                {
                    // Check if there are any notifications
                    if (cmd.ExecuteScalar() != DBNull.Value)
                        notificationId = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
                    else
                        notificationId = 1;
                }
                con.Close();
            }

            using (SqlConnection con = new(connectionString))
            {
                con.Open();
                string query = "INSERT INTO Notification (NotificationId, Message, Timestamp, ClientId, AuctionId) VALUES (@NotificationId, @Message, @Timestamp, @ClientId, @AuctionId)";
                using (SqlCommand cmd = new(query, con))
                {
                    cmd.Parameters.AddWithValue("@NotificationId", notificationId);
                    cmd.Parameters.AddWithValue("@Message", message);
                    cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now);
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
        }

        public int GetNUnreadMessages(int clientId)
        {
            int nUnreadMessages = 0;
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new(connectionString))
            {
                con.Open();
                string query = "SELECT COUNT(*) FROM Notification WHERE ClientId = @ClientId AND IsRead = 0";
                using (SqlCommand cmd = new(query, con))
                {
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    nUnreadMessages = Convert.ToInt32(cmd.ExecuteScalar());
                }
                con.Close();
            }
            return nUnreadMessages;
        }

        public List<Dictionary<string, object>> GetNotifications(int clientId)
        {
            List<Dictionary<string, object>> notifications = new List<Dictionary<string, object>>();
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new(connectionString))
            {
                con.Open();
                string query = @"
                    SELECT N.NotificationId, N.Message, N.Timestamp, N.IsRead, N.AuctionId, P.Name, P.Price
                    FROM Notification N
                    INNER JOIN Product P ON N.AuctionId = P.AuctionId
                    WHERE N.ClientId = @ClientId
                    ORDER BY N.Timestamp DESC";
                using (SqlCommand cmd = new(query, con))
                {
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Dictionary<string, object> notification = new Dictionary<string, object>
                            {
                                { "NotificationId", reader.GetInt32(reader.GetOrdinal("NotificationId")) },
                                { "Message", reader.GetString(reader.GetOrdinal("Message")) },
                                { "Timestamp", reader.GetDateTime(reader.GetOrdinal("Timestamp")) },
                                { "IsRead", reader.GetBoolean(reader.GetOrdinal("IsRead")) },
                                { "AuctionId", reader.GetInt32(reader.GetOrdinal("AuctionId")) },
                                { "ProductName", reader.GetString(reader.GetOrdinal("Name")) },
                                { "ProductPrice", reader.GetInt32(reader.GetOrdinal("Price")) }
                            };
                            notifications.Add(notification);
                        }
                    }
                }
                con.Close();
            }
            return notifications;
        }

        // Get notifications count for a specific client
        public int GetNotificationsCount(int clientId)
        {
            int notificationsCount = 0;
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new(connectionString))
            {
                con.Open();
                string query = "SELECT COUNT(*) FROM Notification WHERE ClientId = @ClientId AND IsRead = 0";
                using (SqlCommand cmd = new(query, con))
                {
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    notificationsCount = Convert.ToInt32(cmd.ExecuteScalar());
                }
                con.Close();
            }
            return notificationsCount;
        }

        public void MarkAsRead(int clientId)
        {
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new(connectionString))
            {
                con.Open();
                string query = "UPDATE Notification SET IsRead = 1 WHERE ClientId = @ClientId";
                using (SqlCommand cmd = new(query, con))
                {
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
        }
    }
}
