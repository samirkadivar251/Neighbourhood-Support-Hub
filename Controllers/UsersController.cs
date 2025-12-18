using NSH.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;


namespace NSH.Controllers
{
    public class UsersController : Controller
    {
        private readonly string connectionString = "Data Source=(localdb)\\ProjectModels;Initial Catalog=EventManagementDB;Integrated Security=True;";

        // GET: Register
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        public ActionResult AdminLogin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(User user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // 📞 Check if Phone already exists
                    string phoneCheckQuery = "SELECT COUNT(*) FROM Users WHERE Phone = @Phone";
                    using (SqlCommand checkPhoneCmd = new SqlCommand(phoneCheckQuery, conn))
                    {
                        checkPhoneCmd.Parameters.AddWithValue("@Phone", user.Phone);
                        int phoneExists = (int)checkPhoneCmd.ExecuteScalar();

                        if (phoneExists > 0)
                        {
                            ViewBag.Message = "Phone number is already registered.";
                            return View(user);
                        }
                    }

                    // 📧 Optional: Check if Email already exists
                    string emailCheckQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                    using (SqlCommand checkEmailCmd = new SqlCommand(emailCheckQuery, conn))
                    {
                        checkEmailCmd.Parameters.AddWithValue("@Email", user.Email);
                        int emailExists = (int)checkEmailCmd.ExecuteScalar();

                        if (emailExists > 0)
                        {
                            ViewBag.Message = "Email is already registered.";
                            return View(user);
                        }
                    }

                    // 🔐 Hash Password
                    string hashedPassword = HashPassword(user.Password);

                    // ✅ Insert new user
                    string query = "INSERT INTO Users (Name, Email, Phone, PasswordHash, CreatedAt) VALUES (@Name, @Email, @Phone, @PasswordHash, GETDATE())";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", user.Name);
                        cmd.Parameters.AddWithValue("@Email", user.Email);
                        cmd.Parameters.AddWithValue("@Phone", user.Phone ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            // 🔓 Auto-login after registration
                            Session["UserID"] = user.Email;
                            Session["UserName"] = user.Name;
                            TempData["SuccessMessage"] = "Registration successful! Welcome, " + user.Name + "!";
                            return RedirectToAction("Dashboard");
                        }
                        else
                        {
                            ViewBag.Message = "Registration failed. Please try again.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error: " + ex.Message;
            }

            return View(user);
        }


        // GET: Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string Email, string Password)
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ViewBag.Message = "Please enter both email and password.";
                return View();
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "SELECT UserID, Name, PasswordHash, IsAdmin, IsBlocked FROM Users WHERE Email = @Email";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", Email);
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = Convert.ToInt32(reader["UserID"]);
                                string dbPasswordHash = reader["PasswordHash"].ToString();
                                string name = reader["Name"].ToString();
                                bool isAdmin = Convert.ToBoolean(reader["IsAdmin"]);
                                bool isBlocked = Convert.ToBoolean(reader["IsBlocked"]); // Get IsBlocked status

                                if (isBlocked)
                                {
                                    ViewBag.Message = "Your account has been blocked by the admin.";
                                    return View(); // Prevent login if user is blocked
                                }

                                if (VerifyPassword(Password, dbPasswordHash))
                                {
                                    Session["UserID"] = userId;
                                    Session["UserName"] = name;
                                    Session["IsAdmin"] = isAdmin; // Store IsAdmin flag in session

                                    TempData["SuccessMessage"] = "Welcome back, " + name + "!";

                                    // Redirect based on user role
                                    if (isAdmin)
                                    {
                                        return RedirectToAction("Dashboard", "Admin");
                                    }
                                    else
                                    {
                                        return RedirectToAction("Dashboard");
                                    }
                                }
                            }
                        }
                    }
                }
                ViewBag.Message = "Invalid email or password.";
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error: " + ex.Message;
            }

            return View();
        }



        // GET: Dashboard (Protected Page)
        public ActionResult Dashboard()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login");
            }

            int loggedInUserID;
            if (!int.TryParse(Session["UserID"].ToString(), out loggedInUserID))
            {
                Session.Clear();  // Clear invalid session
                return RedirectToAction("Login");
            }

            List<RequestModel> createdRequests = new List<RequestModel>();
            List<RequestModel> acceptedRequests = new List<RequestModel>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // ✅ Fetch requests where the logged-in user is the creator
                string createdRequestsQuery = "SELECT * FROM Requests WHERE UserID = @UserID";
                using (SqlCommand cmd = new SqlCommand(createdRequestsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", loggedInUserID);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            createdRequests.Add(new RequestModel
                            {
                                RequestID = Convert.ToInt32(reader["RequestID"]),
                                Title = reader["Title"].ToString(),
                                Description = reader["Description"].ToString(),
                                Status = reader["Status"].ToString()
                            });
                        }
                    }
                }

                // ✅ Fetch requests accepted by the logged-in user
                string acceptedRequestsQuery = "SELECT * FROM Requests WHERE AcceptedByUserID = @UserID";
                using (SqlCommand cmd = new SqlCommand(acceptedRequestsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", loggedInUserID);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            acceptedRequests.Add(new RequestModel
                            {
                                RequestID = Convert.ToInt32(reader["RequestID"]),
                                Title = reader["Title"].ToString(),
                                Description = reader["Description"].ToString(),
                                Status = reader["Status"].ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.CreatedRequests = createdRequests;
            ViewBag.AcceptedRequests = acceptedRequests;
            ViewBag.UserName = Session["UserName"];

            return View();
        }




        // Logout
        public ActionResult Logout()
        {
            Session.Clear();
            TempData["SuccessMessage"] = "You have been logged out.";
            return RedirectToAction("Login");
        }

        // Password Hashing Function
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        // Password Verification Function
        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            string enteredHash = HashPassword(enteredPassword);
            return StringComparer.OrdinalIgnoreCase.Compare(enteredHash, storedHash) == 0;
        }
        [HttpPost]
        public ActionResult MarkAsCompleted(int requestId)
        {
            Console.WriteLine("Received requestId: " + requestId); // Debugging
            if (requestId <= 0)
            {
                TempData["ErrorMessage"] = "Invalid request!";
                return RedirectToAction("Dashboard");
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE Requests SET Status = 'Completed', CompletedAt = GETDATE() WHERE RequestID = @RequestID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@RequestID", requestId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            TempData["SuccessMessage"] = "Request marked as completed!";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "No rows updated. Check if RequestID exists.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Dashboard");
        }
     




    }
}
