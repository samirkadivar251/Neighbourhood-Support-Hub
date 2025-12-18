using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using NSH.Models;

namespace NSH.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base("DefaultConnection") // Change if needed
        {
        }

        public DbSet<Notification> Notifications { get; set; }
         // ✅ Add this line
        public DbSet<EventModel> EventModels { get; set; } // Add other tables if needed
    }
}