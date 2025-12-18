using NSH.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;

namespace NSH.Controllers
{
    public class HomeController : Controller
    {
        private readonly string connectionString = "Data Source=(localdb)\\ProjectModels;Initial Catalog=EventManagementDB;Integrated Security=True;";

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                ViewBag.UserName = Session["UserName"];
            }
            return View();
        }

        public ActionResult Event()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login", "Users");
        }

        [HttpGet]
        public ActionResult CreateEvent()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateEvent(EventModel eventModel, HttpPostedFileBase image)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Users");
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Validation failed. Please check all fields.");
                return View(eventModel);
            }

            try
            {
                string fileName = null;
                if (image != null && image.ContentLength > 0)
                {
                    fileName = Path.GetFileName(image.FileName);
                    string folderPath = Server.MapPath("~/Content/Images/");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                    string fullPath = Path.Combine(folderPath, fileName);
                    image.SaveAs(fullPath);
                }

                int userId = Convert.ToInt32(Session["UserID"]);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = @"INSERT INTO EventModels 
                            (EventTitle, EventDate, EventTime, EventDescription, OrganizerInfo, Location, ImagePath, IsApproved, UserID) 
                            VALUES 
                            (@EventTitle, @EventDate, @EventTime, @EventDescription, @OrganizerInfo, @Location, @ImagePath, 0, @UserID)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@EventTitle", eventModel.EventTitle);
                        cmd.Parameters.AddWithValue("@EventDate", eventModel.EventDate);
                        cmd.Parameters.AddWithValue("@EventTime", eventModel.EventTime);
                        cmd.Parameters.AddWithValue("@EventDescription", eventModel.EventDescription);
                        cmd.Parameters.AddWithValue("@OrganizerInfo", eventModel.OrganizerInfo);
                        cmd.Parameters.AddWithValue("@Location", eventModel.Location);
                        cmd.Parameters.AddWithValue("@ImagePath", fileName != null ? "/Content/Images/" + fileName : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserID", userId);

                        con.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            StoreNotification(userId, eventModel.EventTitle);
                            NotificationHub.SendNotification($"A new event has been created: {eventModel.EventTitle}");

                            TempData["SuccessMessage"] = "Your event has been submitted. It will be published after admin approval.";
                            return RedirectToAction("ViewEvents");
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Event creation failed. Please try again.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }
            return View(eventModel);
        }

        private void StoreNotification(int userId, string message, int? eventId = null, string eventName = null, int? requestId = null, string requestName = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = "INSERT INTO Notifications (UserId, EventId, EventName, RequestId, RequestName, Message, CreatedAt, IsRead) " +
                                   "VALUES (@UserId, @EventId, @EventName, @RequestId, @RequestName, @Message, @CreatedAt, 0)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@EventId", eventId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EventName", eventName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RequestId", requestId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RequestName", requestName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Message", message);
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while storing notification: " + ex.Message);
            }
        }

        public ActionResult EventDetails(int id)
        {
            EventModel eventDetails = null;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT Id, EventTitle, EventDate, EventTime, EventDescription, OrganizerInfo, Location, ImagePath FROM EventModels WHERE Id = @Id";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            eventDetails = new EventModel
                            {
                                Id = reader.GetInt32(0),
                                EventTitle = reader.GetString(1),
                                EventDate = reader.GetDateTime(2),
                                EventTime = reader.GetString(3),
                                EventDescription = reader.GetString(4),
                                OrganizerInfo = reader.GetString(5),
                                Location = reader.GetString(6),
                                ImagePath = reader.IsDBNull(7) ? null : reader.GetString(7)
                            };
                        }
                    }
                }
            }

            if (eventDetails == null)
            {
                return HttpNotFound();
            }

            // ✅ Check if the current logged-in user has already attended this event
            bool hasAttended = false;
            if (Session["UserId"] != null)
            {
                int userId = Convert.ToInt32(Session["UserId"]);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string checkQuery = "SELECT COUNT(*) FROM EventAttendance WHERE EventId = @EventId AND UserId = @UserId";
                    using (SqlCommand cmd = new SqlCommand(checkQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@EventId", id);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        con.Open();

                        int count = (int)cmd.ExecuteScalar();
                        hasAttended = count > 0;
                    }
                }
            }

            ViewBag.HasAttended = hasAttended;

            // 🧍 Fetch attendee names
            List<string> attendees = new List<string>();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string attendeeQuery = @"
            SELECT u.Name 
            FROM EventAttendance ea
            JOIN Users u ON ea.UserId = u.UserID
            WHERE ea.EventId = @EventId";

                using (SqlCommand cmd = new SqlCommand(attendeeQuery, con))
                {
                    cmd.Parameters.AddWithValue("@EventId", id);
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            attendees.Add(reader.GetString(0));
                        }
                    }
                }
            }

            ViewBag.Attendees = attendees;

            return View(eventDetails);
        }



        [HttpGet]
        public ActionResult ViewEvents()
        {
            List<EventModel> events = new List<EventModel>();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT Id, EventTitle, EventDate, EventTime, EventDescription, OrganizerInfo, Location, ImagePath FROM EventModels WHERE IsApproved = 1";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
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
                                ImagePath = reader.IsDBNull(7) ? null : reader.GetString(7)
                            });
                        }
                    }
                }
            }
            return View(events);
        }

        [HttpPost]
        public ActionResult AttendEvent(int eventId)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Users");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // 🔍 Check if the user has already attended this event
                    string checkQuery = "SELECT COUNT(*) FROM EventAttendance WHERE UserId = @UserId AND EventId = @EventId";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@UserId", userId);
                        checkCmd.Parameters.AddWithValue("@EventId", eventId);

                        int exists = (int)checkCmd.ExecuteScalar();

                        if (exists > 0)
                        {
                            TempData["ErrorMessage"] = "You have already marked attendance for this event.";
                            return RedirectToAction("EventDetails", new { id = eventId });
                        }
                    }

                    // ✅ Insert attendance record
                    string insertQuery = "INSERT INTO EventAttendance (UserId, EventId, AttendedDate) VALUES (@UserId, @EventId, GETDATE())";
                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, con))
                    {
                        insertCmd.Parameters.AddWithValue("@UserId", userId);
                        insertCmd.Parameters.AddWithValue("@EventId", eventId);
                        insertCmd.ExecuteNonQuery();
                    }

                    TempData["SuccessMessage"] = "You have successfully attended the event!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while marking your attendance. Please try again.";
                // Optionally log the error: ex.Message
            }

            return RedirectToAction("EventDetails", new { id = eventId });
        }


        public ActionResult AttendEvent()
        {
            List<EventAttendanceModel> attendanceList = new List<EventAttendanceModel>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
            SELECT e.EventTitle, u.Name AS AttendeeName, ea.AttendedDate
            FROM EventAttendance ea
            JOIN Users u ON ea.UserId = u.UserID
            JOIN EventModels e ON ea.EventId = e.Id
            WHERE e.EventDate >= GETDATE()"; // Only show future events

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            attendanceList.Add(new EventAttendanceModel
                            {
                                Event = new EventModel
                                {
                                    EventTitle = reader["EventTitle"].ToString()
                                },
                                User = new UserModel
                                {
                                    Name = reader["AttendeeName"].ToString()
                                },
                                AttendedDate = Convert.ToDateTime(reader["AttendedDate"])
                            });
                        }
                    }
                }
            }

            return View(attendanceList);
        }


    }
}
