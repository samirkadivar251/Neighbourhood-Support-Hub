using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace NSH.Models
{
    public class RequestModel
    {

        public int RequestID { get; set; }
        public int UserID { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Contact { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public string PostedBy { get; set; } // User's Name
        public bool IsCompleted { get; set; }


    }
}