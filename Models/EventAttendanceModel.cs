using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NSH.Models
{
    public class EventAttendanceModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int EventId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime AttendedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual UserModel User { get; set; }

        [ForeignKey("EventId")]
        public virtual EventModel Event { get; set; }
    }
}
