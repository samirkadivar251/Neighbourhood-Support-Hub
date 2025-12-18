using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using NSH.Models;
using PagedList;
using System.Linq;

namespace NSH.Controllers
{
    public class AdminController : Controller

    {
        private string connectionString = "Data Source=(localdb)\\ProjectModels;Initial Catalog=EventManagementDB;Integrated Security=True;";

        // GET: Admin Dashboard
        public ActionResult Dashboard()
        {
            if (Session["isAdmin"] == null)
            {
                TempData["ErrorMessage"] = "Please log in first.";
                return RedirectToAction("Login", "Home");
            }

            ViewBag.AdminName = Session["AdminName"] ?? "Admin";
            return View();
        }

        // Block a user
        public ActionResult BlockUser(int id)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "UPDATE Users SET IsBlocked = 1 WHERE UserID = @UserID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UserID", id);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Users");
        }

        // Unblock a user
        public ActionResult UnblockUser(int id)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "UPDATE Users SET IsBlocked = 0 WHERE UserID = @UserID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UserID", id);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Users");
        }

        // Delete a user
        public ActionResult DeleteUser(int id)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "DELETE FROM Users WHERE UserID = @UserID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UserID", id);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Users");
        }

        // List users with pagination and search
        public ActionResult Users(string search, int? page)
        {
            List<UserModel> users = new List<UserModel>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT UserID, Name, Email, IsBlocked FROM Users WHERE Name LIKE @Search OR Email LIKE @Search ORDER BY UserID DESC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + (search ?? "") + "%");
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        users.Add(new UserModel
                        {
                            UserID = Convert.ToInt32(reader["UserID"]),
                            Name = reader["Name"].ToString(),
                            Email = reader["Email"].ToString(),
                            IsBlocked = reader["IsBlocked"] != DBNull.Value && Convert.ToBoolean(reader["IsBlocked"])
                        });
                    }
                }
            }

            int pageSize = 10;  // Users per page
            int pageNumber = page ?? 1;
            return View(users.ToPagedList(pageNumber, pageSize));
        }
        public ActionResult Requests(int? page, string search)
        {
            List<RequestModel> requests = new List<RequestModel>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = @"
        SELECT r.RequestID, r.Title, r.Category, r.Status, r.IsCompleted, u.Name AS PostedBy
        FROM Requests r
        LEFT JOIN Users u ON r.UserID = u.UserID
        WHERE r.Title LIKE @Search OR r.Category LIKE @Search
        ORDER BY r.RequestID DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + (search ?? "") + "%");
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        requests.Add(new RequestModel
                        {
                            RequestID = Convert.ToInt32(reader["RequestID"]),
                            Title = reader["Title"].ToString(),
                            Category = reader["Category"].ToString(),
                            PostedBy = reader["PostedBy"] != DBNull.Value ? reader["PostedBy"].ToString() : "Unknown",
                            Status = reader["Status"] != DBNull.Value ? reader["Status"].ToString() : "Pending", // Default if NULL
                            IsCompleted = reader["IsCompleted"] != DBNull.Value ? Convert.ToBoolean(reader["IsCompleted"]) : false
                        });
                    }
                }
            }

            int pageSize = 10;
            int pageNumber = page ?? 1;
            return View(requests.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult ManageEvents()
        {
            if (Session["isAdmin"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            List<EventModel> events = new List<EventModel>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT Id, EventTitle, EventDate, EventTime, EventDescription, OrganizerInfo, Location, ImagePath, IsApproved FROM EventModels";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    events.Add(new EventModel
                    {
                        Id = reader.GetInt32(0),
                        EventTitle = reader.GetString(1),
                        EventDate = reader.GetDateTime(2),
                        EventTime = reader.GetString(3),
                        EventDescription = reader.GetString(4),
                        OrganizerInfo = reader.GetString(5),
                        Location = reader.GetString(6),
                        ImagePath = reader.IsDBNull(7) ? null : reader.GetString(7),
                        IsApproved = reader.GetBoolean(8)
                    });
                }
            }

            return View(events); // ✅ Ensure this is passing a List<EventModel> to the view.
        }


        // Approve Event
        [HttpPost]
        public JsonResult ApproveEvent(int id)
        {
            if (Session["isAdmin"] == null)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "UPDATE EventModels SET IsApproved = 1 WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return Json(new { success = true });
        }


        // Reject Event (Now it only marks as rejected instead of deleting)
        [HttpPost]
        public JsonResult RejectEvent(int id)
        {
            if (Session["isAdmin"] == null)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "UPDATE EventModels SET IsApproved = 0 WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return Json(new { success = true });
        }


        // Delete Event
        [HttpPost]
        public JsonResult DeleteEvent(int id)
        {
            if (Session["isAdmin"] == null)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "DELETE FROM EventModels WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return Json(new { success = true });
        }
        public ActionResult EditRequest(int id)
        {
            //Add isAdmin session validation
            if (Session["isAdmin"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            RequestModel request = null;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT RequestID, Title, Category, Description, Contact FROM Requests WHERE RequestID = @RequestID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@RequestID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    request = new RequestModel
                    {
                        RequestID = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Category = reader.GetString(2),
                        Description = reader.GetString(3),
                        Contact = reader.GetString(4)
                    };
                }
            }
            if (request == null)
            {
                return HttpNotFound();
            }
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRequest(RequestModel request)
        {
            //Add isAdmin session validation
            if (Session["isAdmin"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "UPDATE Requests SET Title=@Title, Category=@Category, Description=@Description, Contact=@Contact WHERE RequestID=@RequestID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Title", request.Title);
                cmd.Parameters.AddWithValue("@Category", request.Category);
                cmd.Parameters.AddWithValue("@Description", request.Description);
                cmd.Parameters.AddWithValue("@Contact", request.Contact);
                cmd.Parameters.AddWithValue("@RequestID", request.RequestID);
                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    return HttpNotFound();
                }
            }
            return RedirectToAction("Requests");
        }

        public ActionResult DeleteRequest(int id)
        {
            //Add isAdmin session validation
            if (Session["isAdmin"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "DELETE FROM Requests WHERE RequestID = @RequestID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@RequestID", id);
                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    return HttpNotFound();
                }
            }
            return RedirectToAction("Requests");
        }

    }

}

