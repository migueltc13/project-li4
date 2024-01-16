using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BetterFinds.Pages
{
    [Authorize]
    public class NotificationsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public NotificationsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<Dictionary<string, object>> Notifications = new();

        public bool ShowAll = false;

        public IActionResult OnGet()
        {
            // Get ClientId
            var clientUtils = new Utils.Client(_configuration);
            int clientId = clientUtils.GetClientId(HttpContext, User);
            
            // Notification utils
            var notificationUtils = new Utils.Notification(_configuration);

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
                Notifications = notificationUtils.GetNotifications(clientId);
                ShowAll = Request.Query["ShowAll"] == "1";
            }

            // Get notifications
            Notifications = notificationUtils.GetNotifications(clientId);

            // Get number of unread messages
            ViewData["NUnreadMessages"] = notificationUtils.GetNUnreadMessages(clientId);

            return Page();
        }
    }
}
