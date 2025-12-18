/*using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace YourNamespace.Controllers
{
    public class AdminAuthController : Controller
    {
        private readonly string connectionString = "Server=(localdb)\\ProjectModels;Database=EventManagementDB;Integrated Security=True;";

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string Email, string Password)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "SELECT AdminID, Name, PasswordHash FROM Admins WHERE Email = @Email";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", Email);
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int adminId = Convert.ToInt32(reader["AdminID"]);
                                string dbPasswordHash = reader["PasswordHash"].ToString();
                                string name = reader["Name"].ToString();

                                System.Diagnostics.Debug.WriteLine("Admin ID: " + adminId);
                                System.Diagnostics.Debug.WriteLine("Stored Password Hash: " + dbPasswordHash);

                                if (VerifyPassword(Password, dbPasswordHash))
                                {
                                    Session["AdminID"] = adminId;
                                    Session["AdminName"] = name;
                                    TempData["SuccessMessage"] = "Welcome, " + name + "!";

                                    return RedirectToAction("Dashboard", "Admin");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("Password verification failed.");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("Admin not found.");
                            }
                        }
                    }
                }
                ViewBag.ErrorMessage = "Invalid email or password.";
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error: " + ex.Message;
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
            }
            return View();
        }

        public ActionResult Logout()
        {
            Session.Clear();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(enteredPassword));
                string enteredHash = BitConverter.ToString(bytes).Replace("-", "").ToLower();

                // Debugging Output
                System.Diagnostics.Debug.WriteLine("Entered Hash: " + enteredHash);
                System.Diagnostics.Debug.WriteLine("Stored Hash: " + storedHash);

                return StringComparer.OrdinalIgnoreCase.Compare(enteredHash, storedHash) == 0;
            }
        }
    }
}
*/