using System;
using System.ComponentModel.DataAnnotations;

namespace NSH.Models
{
    public class EventModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Event title is required")]
        public string EventTitle { get; set; }  // Changed from EventTitle → Title (Matches SQL column)

        [Required(ErrorMessage = "Event date is required")]
        public DateTime EventDate { get; set; }  // Changed from EventDate → Date

        [Required(ErrorMessage = "Event time is required")]
        public string EventTime { get; set; }  // Changed from EventTime → Time

        [Required(ErrorMessage = "Event description is required")]
        [StringLength(500, ErrorMessage = "Description can't be longer than 500 characters")]
        public string EventDescription { get; set; }  // Changed from EventDescription → Description

        [Required(ErrorMessage = "Organizer info is required")]
        public string OrganizerInfo { get; set; }  // Changed from OrganizerInfo → Organizer

        public string Location { get; set; }
        public string ImagePath { get; set; }
        public bool IsApproved { get; set; } // Admin Approval Status (true = approved, false = pending)

        // (Optional) Additional Fields
        public DateTime CreatedAt { get; set; } // Timestamp when the event was created
        public int CreatedByUserId { get; set; }
        public int UserID { get; set; }

        // Navigation Property (Optional)
        public User User { get; set; }
    }
}
