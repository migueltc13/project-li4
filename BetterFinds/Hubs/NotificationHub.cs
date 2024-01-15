using Microsoft.AspNetCore.SignalR;

public class NotificationHub : Hub
{
    /*
    public string GetConnectionId()
    {
    return Context.ConnectionId;
    }

    // Notification to all clients
    public async Task SendNotificationToAll(string message)
    {
    await Clients.All.SendAsync("ReceiveNotification", message);
    }

    // Notification to a specific client
    public async Task SendNotificationToClient(string connectionId, string message)
    {
    await Clients.Client(connectionId).SendAsync("ReceiveNotification", message);
    }

    // Add a client to a group
    public async Task JoinAuctionGroup(string connectionId, string auctionId)
    {
    await Groups.AddToGroupAsync(connectionId, auctionId);
    }

    // Remove a client from a group
    public async Task LeaveAuctionGroup(string connectionId, string auctionId)
    {
    await Groups.RemoveFromGroupAsync(connectionId, auctionId);
    }

    // Notification to a specific group
    public async Task SendNotificationToGroup(string connectionId, string auctionId, string message)
    {
    await Clients.Group(auctionId).SendAsync("ReceiveNotification", message);
    }
    */

    /*
    public async Task UpdateNotificationCount(HttpContext httpContext)
    {
        // Get ClientId
        var clientUtils = new Utils.Client(_configuration);
        int clientId = clientUtils.GetClientId(httpContext, User);

        var notificationUtils = new Utils.Notification(_configuration);
        int newCount = notificationUtils.GetNUnreadMessages(clientId);
        // Broadcast the new notification count to all connected clients
        await Clients.All.SendAsync("ReceiveNotificationCount", newCount);
    }
    */
}