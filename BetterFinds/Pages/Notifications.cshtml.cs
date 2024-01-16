using Microsoft.AspNetCore.Authorization;
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

        public void OnGet()
        {
            // Get ClientId
            var clientUtils = new Utils.Client(_configuration);
            int clientId = clientUtils.GetClientId(HttpContext, User);

            // Get notifications
            var notificationUtils = new Utils.Notification(_configuration);
            Notifications = notificationUtils.GetNotifications(clientId);

            // Mark notifications as read
            notificationUtils.MarkAsRead(clientId);
        }
    }
}
