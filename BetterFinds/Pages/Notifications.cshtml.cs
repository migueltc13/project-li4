using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BetterFinds.Pages
{
    /// <summary>
    /// Model for the **Notifications** page.
    /// This class is decorated with the Authorize attribute.
    /// </summary>
    [Authorize]
    public class NotificationsModel : PageModel
    {
        /// <summary>
        /// The IConfiguration instance.
        /// </summary>
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationsModel"/> class.
        /// </summary>
        /// <param name="configuration">The IConfiguration instance.</param>
        public NotificationsModel(IConfiguration configuration) =>
            this.configuration = configuration;

        /// <summary>
        /// The list of notifications to display.
        /// </summary>
        public List<Dictionary<string, object>> Notifications = [];

        /// <summary>
        /// Whether to show all notifications or only unread ones.
        /// </summary>
        public bool ShowAll = false;

        /// <summary>
        /// Shows the client's notifications.
        /// </summary>
        /// <remarks>
        /// This page gets the client's notifications from the database and displays them.
        /// <para/>
        /// The notifications can be filtered to show only unread notifications or all notifications, 
        /// and can be marked as read individually or all at once.
        /// The notifications can be sorted by a specified sort order.
        /// To see available filter and sort options, see <see cref="Utils.Notification.GetNotifications"/>.
        /// <para/>
        /// This page is automatically refreshed whena new notification is received through **SignalR**.
        /// </remarks>
        /// <returns>A task that represents the action of loading the Notifications page.</returns>
        public IActionResult OnGet()
        {
            // Get ClientId
            var clientUtils = new Utils.Client(configuration);
            int clientId = clientUtils.GetClientId(HttpContext, User);

            // Notification utils
            var notificationUtils = new Utils.Notification(configuration);

            // Option to mark a notification as read: ?MarkAsRead=NofiticationId
            if (Request.Query.TryGetValue("MarkAsRead", out var markAsReadValue) && !string.IsNullOrEmpty(markAsReadValue))
            {
                if (int.TryParse(markAsReadValue, out int notificationId))
                {
                    notificationUtils.MarkAsRead(clientId, notificationId);
                }
                else
                {
                    return NotFound();
                }
            }

            // Option to mark all notifications as read: ?MarkAllAsRead=1
            if (Request.Query.ContainsKey("MarkAllAsRead"))
            {
                notificationUtils.MarkAllAsRead(clientId);
            }

            // Toggle between unread and all notifications: ?ShowAll=1 or nothing
            if (Request.Query.ContainsKey("ShowAll"))
            {
                ShowAll = true;
            }

            // Get notifications
            Notifications = notificationUtils.GetNotifications(clientId);

            // Get number of unread notifications
            ViewData["NUnreadNotifications"] = notificationUtils.GetUnreadNotificationsCount(clientId);

            return Page();
        }
    }
}
