using Microsoft.Data.SqlClient;

namespace BetterFinds.Utils
{
    /// <summary>
    /// Provides utility functions for handling **notifications-related** operations.
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// The IConfiguration instance.
        /// </summary>
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Notification"/> class.
        /// </summary>
        /// <param name="configuration">The IConfiguration instance.</param>
        public Notification(IConfiguration configuration) =>
            this.configuration = configuration;

        /// <summary>
        /// Creates a new notification for the specified client.
        /// </summary>
        /// <remarks>
        /// Creates and inserts a new notification into the database.
        /// <para/>
        /// A notification has the following properties:
        /// <list type="bullet">
        ///     <item><description>NotificationId: The unique identifier of the notification.</description></item>
        ///     <item><description>Message: The message content of the notification.</description></item>
        ///     <item><description>Timestamp: The timestamp of the notification.</description></item>
        ///     <item><description>ClientId: The ClientId of the client.</description></item>
        ///     <item><description>AuctionId: The AuctionId of the auction related to the notification.</description></item>
        ///     <item><description>IsRead: A boolean value indicating whether the notification has been read. Default value: false.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="clientId">ClientId of the client.</param>
        /// <param name="auctionId">AuctionId of the auction related to the notification.</param>
        /// <param name="message">The message content of the notification.</param>
        public void CreateNotification(int clientId, int auctionId, string message)
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");

            // Get NotificationId
            int notificationId = 1; // Default value
            using (SqlConnection con = new(connectionString))
            {
                con.Open();
                string query = "SELECT MAX(NotificationId) FROM Notification";
                using (SqlCommand cmd = new(query, con))
                {
                    // Check if there are any notifications
                    var result = cmd.ExecuteScalar();
                    notificationId = result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
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
                    cmd.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
        }

        /// <summary>
        /// Returns a dictionary list of notifications for a specific client.
        /// </summary>
        /// <param name="clientId">The ClientId of the client.</param>
        /// <remarks>
        /// This method is used to display the notifications in the <see cref="Pages.NotificationsModel"/> page.
        /// <para/>
        /// The dictionary returned by this method contains the following keys:
        /// <code lang="json">
        /// {
        ///     "NotificationId": int,
        ///     "Message": string,
        ///     "Timestamp": DateTime,
        ///     "IsRead": bool,
        ///     "AuctionId": int,
        ///     "ProductName": string,
        ///     "ProductPrice": decimal
        /// }
        /// </code>
        /// </remarks>
        /// <returns>A dictionary list of notifications for a specific client.</returns>
        public List<Dictionary<string, object>> GetNotifications(int clientId)
        {
            List<Dictionary<string, object>> notifications = [];
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
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
                    using SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Dictionary<string, object> notification = new()
                        {
                                { "NotificationId", reader.GetInt32(reader.GetOrdinal("NotificationId")) },
                                { "Message", reader.GetString(reader.GetOrdinal("Message")) },
                                { "Timestamp", reader.GetDateTime(reader.GetOrdinal("Timestamp")) },
                                { "IsRead", reader.GetBoolean(reader.GetOrdinal("IsRead")) },
                                { "AuctionId", reader.GetInt32(reader.GetOrdinal("AuctionId")) },
                                { "ProductName", reader.GetString(reader.GetOrdinal("Name")) },
                                { "ProductPrice", reader.GetDecimal(reader.GetOrdinal("Price")) }
                            };
                        notifications.Add(notification);
                    }
                }
                con.Close();
            }
            return notifications;
        }

        /// <summary>
        /// Returns the total number of notifications for a specific client.
        /// </summary>
        /// <remarks>
        /// Currently not being used.
        /// </remarks>
        /// <param name="clientId">The ClientId of the client.</param>
        /// <returns>The total number of notifications for a specific client.</returns>
        public int GetNotificationsCount(int clientId)
        {
            int notificationsCount = 0;
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new(connectionString))
            {
                con.Open();
                string query = "SELECT COUNT(*) FROM Notification WHERE ClientId = @ClientId";
                using (SqlCommand cmd = new(query, con))
                {
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    notificationsCount = Convert.ToInt32(cmd.ExecuteScalar());
                }
                con.Close();
            }
            return notificationsCount;
        }

        /// <summary>
        /// Returns the number of unread notifications for a specific client.
        /// </summary>
        /// <remarks>
        /// This method is used to display the number of unread notifications in the navbar.
        /// </remarks>
        /// <param name="clientId">The ClientId of the client.</param>
        /// <returns>The number of unread notifications for a specific client.</returns>
        public int GetUnreadNotificationsCount(int clientId)
        {
            int nUnreadMessages = 0;
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
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

        /// <summary>
        /// Marks a notification as read. Used in the <see cref="Pages.NotificationsModel"/> page.
        /// </summary>
        /// <param name="clientId">The ClientId of the client to mark the notification as read.</param>
        /// <param name="notificationId">The NotificationId of the notification to mark as read.</param>
        public void MarkAsRead(int clientId, int notificationId)
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            using SqlConnection con = new(connectionString);
            con.Open();
            string query = "UPDATE Notification SET IsRead = 1 WHERE NotificationId = @NotificationId AND ClientId = @ClientId";
            using (SqlCommand cmd = new(query, con))
            {
                cmd.Parameters.AddWithValue("@NotificationId", notificationId);
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                cmd.ExecuteNonQuery();
            }
            con.Close();
        }

        /// <summary>
        /// Marks all notifications as read. Used in the <see cref="Pages.NotificationsModel"/> page.
        /// </summary>
        /// <param name="clientId">The ClientId of the client to mark all notifications as read.</param>
        public void MarkAllAsRead(int clientId)
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            using SqlConnection con = new(connectionString);
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
