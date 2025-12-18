using NSH.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using System.Configuration;
using System.Net.Mail;
using System.Net;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNet.SignalR;

namespace NSH.Controllers
{
    public class RequestsController : Controller
    {
        private readonly string connectionString = "Data Source=(localdb)\\ProjectModels;Initial Catalog=EventManagementDB;Integrated Security=True;";

        // ✅ GET ALL REQUESTS (With Search & Filter)
        public ActionResult Index(string searchQuery, string statusFilter)
        {
            List<RequestModel> requests = new List<RequestModel>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT RequestID, UserID, Title, Category, Description, Contact, CreatedAt, Status FROM Requests WHERE 1=1";

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query += " AND (Title LIKE @search OR Category LIKE @search OR Description LIKE @search)";
                }

                if (!string.IsNullOrEmpty(statusFilter))
                {
                    query += " AND Status = @status";
                }

                SqlCommand cmd = new SqlCommand(query, conn);

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    cmd.Parameters.AddWithValue("@search", "%" + searchQuery + "%");
                }

                if (!string.IsNullOrEmpty(statusFilter))
                {
                    cmd.Parameters.AddWithValue("@status", statusFilter);
                }

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    requests.Add(new RequestModel
                    {
                        RequestID = reader.GetInt32(0),
                        UserID = reader.GetInt32(1),
                        Title = reader.GetString(2),
                        Category = reader.GetString(3),
                        Description = reader.GetString(4),
                        Contact = reader.GetString(5),
                        CreatedAt = reader.GetDateTime(6),
                        Status = reader.GetString(7)
                    });
                }
            }
            return View(requests);
        }

        // ✅ ADD NEW REQUEST
        [HttpGet]
        public ActionResult Create()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RequestModel request)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Users");
            }

            List<string> validCategories = new List<string> { "Volunteer", "Borrow", "Emergency" };
            if (!validCategories.Contains(request.Category))
            {
                ModelState.AddModelError("Category", "Invalid category selected.");
                return View(request);
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Please check all fields.");
                return View(request);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = "INSERT INTO Requests (UserID, Title, Category, Description, Contact, CreatedAt, Status) VALUES (@UserID, @Title, @Category, @Description, @Contact, GETDATE(), 'Pending')";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        int loggedInUserID = Convert.ToInt32(Session["UserID"]);
                        cmd.Parameters.AddWithValue("@UserID", loggedInUserID);
                        cmd.Parameters.AddWithValue("@Title", request.Title);
                        cmd.Parameters.AddWithValue("@Category", request.Category);
                        cmd.Parameters.AddWithValue("@Description", request.Description);
                        cmd.Parameters.AddWithValue("@Contact", request.Contact);
                        con.Open();
                        cmd.ExecuteNonQuery();

                        // Store notification
                        StoreNotification(loggedInUserID, request.Title);

                        // Send real-time notification
                        NotificationHub.SendNotification("A new request has been posted: " + request.Title);
                    }
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
                return View(request);
            }
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




        public ActionResult Delete(int id)
        {
            bool isUserLoggedIn = Session["UserID"] != null;
            bool isAdminLoggedIn = Session["isAdmin"] != null && Convert.ToBoolean(Session["isAdmin"]);

            if (!isUserLoggedIn && !isAdminLoggedIn)
            {
                return RedirectToAction("Login", "Users");
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "DELETE FROM Requests WHERE RequestID = @RequestID";

                if (!isAdminLoggedIn)
                {
                    query += " AND UserID = @UserID";
                }

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@RequestID", id);

                if (!isAdminLoggedIn)
                {
                    cmd.Parameters.AddWithValue("@UserID", Convert.ToInt32(Session["UserID"]));
                }

                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return HttpNotFound();
                }
            }

            return RedirectToAction("Index");
        }
        // ✅ EDIT REQUEST (GET) - ONLY OWNER CAN EDIT
        [HttpGet]
        public ActionResult Edit(int id)
        {
            bool isUserLoggedIn = Session["UserID"] != null;
            bool isAdminLoggedIn = Session["isAdmin"] != null && Convert.ToBoolean(Session["isAdmin"]); // Corrected admin check

            if (!isUserLoggedIn && !isAdminLoggedIn)
            {
                return RedirectToAction("Login", "Users");
            }

            RequestModel request = null;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = isAdminLoggedIn
                    ? "SELECT RequestID, Title, Category, Description, Contact FROM Requests WHERE RequestID = @RequestID"
                    : "SELECT RequestID, Title, Category, Description, Contact FROM Requests WHERE RequestID = @RequestID AND UserID = @UserID";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@RequestID", id);

                if (!isAdminLoggedIn)
                {
                    cmd.Parameters.AddWithValue("@UserID", Convert.ToInt32(Session["UserID"]));
                }

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
        public ActionResult Edit(RequestModel request)
        {
            bool isUserLoggedIn = Session["UserID"] != null;
            bool isAdminLoggedIn = Session["isAdmin"] != null && Convert.ToBoolean(Session["isAdmin"]); // Corrected admin check

            if (!isUserLoggedIn && !isAdminLoggedIn)
            {
                return RedirectToAction("Login", "Users");
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = isAdminLoggedIn
                    ? "UPDATE Requests SET Title=@Title, Category=@Category, Description=@Description, Contact=@Contact WHERE RequestID=@RequestID"
                    : "UPDATE Requests SET Title=@Title, Category=@Category, Description=@Description, Contact=@Contact WHERE RequestID=@RequestID AND UserID=@UserID";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Title", request.Title);
                cmd.Parameters.AddWithValue("@Category", request.Category);
                cmd.Parameters.AddWithValue("@Description", request.Description);
                cmd.Parameters.AddWithValue("@Contact", request.Contact);
                cmd.Parameters.AddWithValue("@RequestID", request.RequestID);

                if (!isAdminLoggedIn)
                {
                    cmd.Parameters.AddWithValue("@UserID", Convert.ToInt32(Session["UserID"]));
                }

                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return HttpNotFound();
                }
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        public ActionResult AcceptRequest(int requestId)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login");
            }

            int acceptedByUserID = Convert.ToInt32(Session["UserID"]);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Get request details
                string getRequestQuery = "SELECT UserID, Title FROM Requests WHERE RequestID = @RequestID";
                int requestOwnerID = 0;
                string requestTitle = "";

                using (SqlCommand cmd = new SqlCommand(getRequestQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@RequestID", requestId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            requestOwnerID = Convert.ToInt32(reader["UserID"]);
                            requestTitle = reader["Title"].ToString();
                        }
                    }
                }

                if (requestOwnerID == 0)
                {
                    TempData["ErrorMessage"] = "Request not found!";
                    return RedirectToAction("Requests", "");
                }

                // Update request status
                string updateQuery = "UPDATE Requests SET Status = 'Accepted', AcceptedByUserID = @AcceptedBy WHERE RequestID = @RequestID";
                using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@AcceptedBy", acceptedByUserID);
                    cmd.Parameters.AddWithValue("@RequestID", requestId);
                    cmd.ExecuteNonQuery();
                }

                // Send real-time notification using SignalR
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
                hubContext.Clients.User(requestOwnerID.ToString()).receiveNotification($"Your request '{requestTitle}' has been accepted!");

                // Get request owner's email
                string ownerEmail = "";
                string getEmailQuery = "SELECT Email FROM Users WHERE UserID = @UserID";
                using (SqlCommand cmd = new SqlCommand(getEmailQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", requestOwnerID);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ownerEmail = reader["Email"].ToString();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(ownerEmail))
                {
                    string subject = "Your request has been accepted!";
                    string requesterEmail = "";

                    // Get the email of the user who accepted the request
                    string getRequesterEmailQuery = "SELECT Email FROM Users WHERE UserID = @UserID";
                    using (SqlCommand cmd = new SqlCommand(getRequesterEmailQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", acceptedByUserID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                requesterEmail = reader["Email"].ToString();
                            }
                        }
                    }

                    string body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; }}
                            .container {{ max-width: 600px; background: #fff; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ccc; }}
                            h2 {{ color: #2c3e50; }}
                            p {{ font-size: 16px; color: #555; }}
                            .footer {{ text-align: center; margin-top: 20px; font-size: 14px; color: #777; }}
                            .btn {{ display: inline-block; padding: 10px 20px; background-color: #3498db; color: #fff; text-decoration: none; border-radius: 5px; }}
                            .btn:hover {{ background-color: #2980b9; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <h2>🎉 Great News! Your Request Has Been Accepted</h2>
                            <p>Dear User,</p>
                            <p>Your request for assistance (Request ID: <b>{requestId}</b>) has been accepted by <b>{requesterEmail}</b>.</p>
                            <p>Feel free to connect and coordinate further.</p>
                            <p>
                                <a href='https://nsh.com/requests/{requestId}' class='btn'>View Request</a>
                            </p>
                            <p class='footer'>Thank you for using <b>Neighborhood Support Hub</b>. We are here to help! 😊</p>
                        </div>
                    </body>
                    </html>";

                    SendEmail(ownerEmail, subject, body);
                }

                TempData["SuccessMessage"] = "Request accepted, notification sent, and email delivered!";
            }

            return RedirectToAction("Dashboard", "Users");
        }

        private void SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("samirkadivar616@gmail.com");
                mail.To.Add(toEmail);
                mail.Subject = subject;
                mail.IsBodyHtml = true;
                mail.Body = body;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential("samirkadivar616@gmail.com", "cenk cxyp erfi zzwg");
                smtp.EnableSsl = true;
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email failed: " + ex.Message);
            }

        }

        }
}
