using NSH.Data;
using NSH.Models;
using System.Collections.Generic;
using System.Linq;

namespace NSH.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Fetch all notifications from the database
        public List<Notification> GetNotifications()
        {
            return _context.Notifications
                           .OrderByDescending(n => n.CreatedAt)
                           .ToList();  // Fetch from DB
        }
    }
}
