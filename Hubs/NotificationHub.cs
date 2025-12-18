using Microsoft.AspNet.SignalR;
using System;

public class NotificationHub : Hub
{
    public static void SendNotification(string message, int? eventId = null, string eventName = null, int? requestId = null, string requestName = null)
    {
        try
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();

            // Send notification to all users (default)
            context.Clients.All.receiveNotification(message, eventId, eventName, requestId, requestName);

            // OPTIONAL: Send notification to a specific group (e.g., event attendees)
            if (eventId.HasValue)
            {
                context.Clients.Group(eventId.Value.ToString()).receiveNotification(message, eventId, eventName, requestId, requestName);
            }

            // OPTIONAL: Send notification to a specific request (e.g., if it's a borrowed item request)
            if (requestId.HasValue)
            {
                context.Clients.Group(requestId.Value.ToString()).receiveNotification(message, eventId, eventName, requestId, requestName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending notification: " + ex.Message);
        }
    }
}
