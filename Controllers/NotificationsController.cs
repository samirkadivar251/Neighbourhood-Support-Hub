using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using NSH.Models;

namespace NSH.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly string connectionString = "Data Source=(localdb)\\ProjectModels;Initial Catalog=EventManagementDB;Integrated Security=True;";

        // ✅ Fetch Notifications
        public ActionResult Noti()
        {
            List<Notification> notifications = new List<Notification>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT Id, UserId, Message, CreatedAt, IsRead, EventId, RequestId
                    FROM Notifications
                    ORDER BY CreatedAt DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int? eventId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5);
                            int? requestId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6);

                            notifications.Add(new Notification
                            {
                                Id = reader.GetInt32(0),
                                UserId = reader.GetInt32(1),
                                Message = reader.GetString(2),
                                CreatedAt = reader.GetDateTime(3),
                                IsRead = reader.GetBoolean(4),
                                EventId = eventId,
                                RequestId = requestId
                            });
                        }
                    }
                }
            }

            return View(notifications);
        }

        // ✅ Mark a Notification as Read
        [HttpPost]
        public ActionResult MarkAsRead(int id)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "UPDATE Notifications SET IsRead = 1 WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return Json(new { success = true });
        }

        // ✅ Get Unread Notification Count
        [HttpGet]
        public JsonResult GetUnreadNotificationCount()
        {
            int unreadCount = 0;
            int currentUserId = Convert.ToInt32(Session["UserID"]);

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
            SELECT COUNT(*) FROM Notifications 
            WHERE IsRead = 0 AND (UserId = @UserId OR UserId IS NULL)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UserId", currentUserId);
                    con.Open();
                    unreadCount = (int)cmd.ExecuteScalar();
                }
            }

            return Json(new { count = unreadCount }, JsonRequestBehavior.AllowGet);
        }

    }
}
