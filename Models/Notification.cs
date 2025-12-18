using System;

namespace NSH.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int? UserId { get; set; } // NULL means notification is for all users
        public int? EventId { get; set; }
        public string EventName { get; set; } // Optional
        public int? RequestId { get; set; }
        public string RequestName { get; set; } // Optional
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }

        // ✅ Constructor to handle default values
        public Notification()
        {
            CreatedAt = DateTime.Now;
            IsRead = false;
        }
    }
}
