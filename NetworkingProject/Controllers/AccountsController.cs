using NetworkingProject.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace NetworkingProject.Controllers
{
    public class AccountsController : Controller
    {
        public static string ComputeSHA256Hash(string text)
        {
            // Create a SHA256 hash object
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Convert the input string to a byte array and compute the hash
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(text));

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2")); // Convert to hexadecimal string
                }
                return builder.ToString();
            }
        }

        //sign in segment
        public ActionResult SignIn()
        {
            return View();
        }

        // POST: Account/SignIn
        [HttpPost]
        public ActionResult SignIn(SignInModel model)
        {
            if (ModelState.IsValid)
            {
                string connectionString = "Server=localhost;Database=NetProj_Web_db;Trusted_Connection=True;";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        connection.Open();

                        // Query to check if the email and password match
                        string query = "SELECT Role FROM NetProj_Web_db.dbo.Users WHERE Email = @Email AND Password = @Password";
                        //string query = $"SELECT Role FROM NetProj_Web_db.dbo.Users WHERE Email = '{model.Email}' AND Password = '{model.Password}'";
                        using (SqlCommand cmd = new SqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@Email", model.Email);
                            cmd.Parameters.AddWithValue("@Password", model.Password);

                            object result = cmd.ExecuteScalar();

                            if (result == null)
                            {
                                // No user found with matching email and password
                                TempData["AlertMessage"] = "Invalid email or password.";
                                TempData["AlertType"] = "danger";
                                return View(model);
                            }

                            string role = result.ToString();
                            Session["UserEmail"] = model.Email;
                            Session["UserRole"] = role;

                            switch (role)
                            {
                                case "Admin":
                                case "User":
                                    return RedirectToAction("Index", "HomePage");
                                case "Suspended":
                                    TempData["AlertMessage"] = "Your account has been suspended. Please contact support for assistance.";
                                    TempData["AlertType"] = "warning";
                                    break;
                                default:
                                    TempData["AlertMessage"] = "Invalid role assigned to account.";
                                    TempData["AlertType"] = "danger";
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error and show user-friendly message
                        Debug.WriteLine($"Sign-in error: {ex.Message}");
                        TempData["AlertMessage"] = "An error occurred while signing in. Please try again.";
                        TempData["AlertType"] = "danger";
                    }
                }
            }
            else
            {
                // Model validation failed
                TempData["AlertMessage"] = "Please enter valid email and password.";
                TempData["AlertType"] = "danger";
            }

            // If we get here, something went wrong
            return View(model);
        }

        //Reset Password segment
        public ActionResult ResetPassword()
        {
            return View(new ResetPasswordModel());
        }

        // POST: Account/ResetPassword
        [HttpPost]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                // First check if passwords match
                if (model.Password != model.ConfirmPassword)
                {
                    TempData["AlertMessage"] = "Passwords do not match!";
                    TempData["AlertType"] = "danger";
                    return View(model);
                }

                string connectionString = "Server=localhost;Database=NetProj_Web_db;Trusted_Connection=True;";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        connection.Open();
                        // First check if the email exists
                        string checkQuery = "SELECT COUNT(*) FROM NetProj_Web_db.dbo.Users WHERE Email = @Email";
                        using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@Email", model.Email);
                            int userCount = (int)checkCmd.ExecuteScalar();

                            if (userCount == 0)
                            {
                                // No user found with this email
                                TempData["AlertMessage"] = "No account found with this email address.";
                                TempData["AlertType"] = "danger";
                                return View(model);
                            }
                        }

                        // Now update the password for the user
                        model.Password = ComputeSHA256Hash(model.Password);
                        string updateQuery = "UPDATE NetProj_Web_db.dbo.Users SET Password = @Password WHERE Email = @Email";
                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, connection))
                        {
                            updateCmd.Parameters.AddWithValue("@Email", model.Email);
                            updateCmd.Parameters.AddWithValue("@Password", model.Password);

                            int rowsAffected = updateCmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                // Password updated successfully
                                TempData["AlertMessage"] = "Your password has been reset successfully!";
                                TempData["AlertType"] = "success";
                                return RedirectToAction("SignIn", "Accounts");
                            }
                            else
                            {
                                // Something went wrong with the update
                                TempData["AlertMessage"] = "Failed to update password. Please try again.";
                                TempData["AlertType"] = "danger";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error and show user-friendly message
                        Debug.WriteLine($"Reset Password error: {ex.Message}");
                        TempData["AlertMessage"] = "An error occurred while resetting password. Please try again.";
                        TempData["AlertType"] = "danger";
                    }
                }
            }
            else
            {
                // Model validation failed
                TempData["AlertMessage"] = "Please enter valid email and password.";
                TempData["AlertType"] = "danger";
            }
            // If we get here, something went wrong
            return View(model);
        }

        public ActionResult SignOut()
        {
            Session["UserRole"] = null; // Clears the role key stored in the session by the sign in method
            return RedirectToAction("Index", "HomePage");
        }


        //registration segment
        public ActionResult Register()
        {
            return View();
        }


        [HttpPost]
        public ActionResult Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                string connectionString = ConfigurationManager.ConnectionStrings["NetProj_Web_db"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        connection.Open();


                        string query = "INSERT INTO Users (Email, Password, FirstName, LastName, PhoneNumber, DateOfBirth, Role) " +
                                       "VALUES (@Email, @Password, @FirstName, @LastName, @PhoneNumber, @DateOfBirth, @Role)";

                        using (SqlCommand cmd = new SqlCommand(query, connection))
                        {
                            // Add parameters to prevent SQL injection
                            cmd.Parameters.AddWithValue("@Email", model.Email);
                            cmd.Parameters.AddWithValue("@Password", ComputeSHA256Hash(model.Password)); 
                            cmd.Parameters.AddWithValue("@FirstName", model.FirstName);
                            cmd.Parameters.AddWithValue("@LastName", model.LastName);
                            cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
                            cmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth);
                            cmd.Parameters.AddWithValue("@Role", "User"); // Default role as "User"

                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions (e.g., log the error)
                        ModelState.AddModelError("", "Error occurred while registering the user: " + ex.Message);
                        return View(model);
                    }
                }
                return RedirectToAction("Index", "HomePage");
            }

            // If validation fails, return the same view with error messages
            return View(model);
        }


        [HttpGet]
        public JsonResult CheckEmailExists(string email)
        {
            // Check if email exists in the database
            bool emailExists = false;
            string connectionString = ConfigurationManager.ConnectionStrings["NetProj_Web_db"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    connection.Open();
                    int count = (int)cmd.ExecuteScalar();
                    emailExists = count > 0; // If count > 0, email exists
                }
            }

            // Return the result as JSON
            return Json(new { exists = emailExists }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetUserDetails(string email)
        {
            // Initialize an object to hold user details
            var userDetails = new
            {
                firstName = "",
                lastName = "",
                phoneNumber = "",
                dob = "",
                role = ""
            };

            // Database connection string
            string connectionString = ConfigurationManager.ConnectionStrings["NetProj_Web_db"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Query to fetch user details by email
                    string query = @"
                SELECT FirstName, LastName, PhoneNumber, DateOfBirth, Role
                FROM Users
                WHERE Email = @Email";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        // Add email parameter to prevent SQL injection
                        cmd.Parameters.AddWithValue("@Email", email);

                        // Open connection
                        connection.Open();

                        // Execute query and read data
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Map database fields to the userDetails object
                                userDetails = new
                                {
                                    firstName = reader["FirstName"]?.ToString() ?? "",
                                    lastName = reader["LastName"]?.ToString() ?? "",
                                    phoneNumber = reader["PhoneNumber"]?.ToString() ?? "",
                                    dob = reader["DateOfBirth"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["DateOfBirth"]).ToString("yyyy-MM-dd")
                                    : "", // Format as "yyyy-MM-dd"
                                    role = reader["Role"]?.ToString() ?? ""
                                };
                            }
                        }
                    }
                }

                // Return user details as JSON
                return Json(userDetails, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to use a logging framework)
                Console.WriteLine("Error fetching user details: " + ex.Message);

                // Return an error message (optional: include a custom error field)
                return Json(new { error = "An error occurred while fetching user details." }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public JsonResult UpdateUserDetails(
                                                string email,   
                                                string firstName,
                                                string lastName,
                                                string phoneNumber,
                                                DateTime dateOfBirth,
                                                string role)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["NetProj_Web_db"].ConnectionString;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"
                UPDATE Users
                SET 
                    FirstName = @FirstName,
                    LastName = @LastName,
                    PhoneNumber = @PhoneNumber,
                    DateOfBirth = @DateOfBirth,
                    Role = @Role
                WHERE Email = @Email";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@FirstName", firstName);
                        cmd.Parameters.AddWithValue("@LastName", lastName);
                        cmd.Parameters.AddWithValue("@PhoneNumber", phoneNumber);
                        cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                        cmd.Parameters.AddWithValue("@Role", role);

                        connection.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Json(new { success = true });
                        }
                        else
                        {
                            return Json(new { success = false, message = "No rows were updated. Please check the email address." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine("Error updating user details: " + ex.Message);
                return Json(new { success = false, message = "An error occurred while updating user details." }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public JsonResult ToggleUserSuspension(string email)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["NetProj_Web_db"].ConnectionString;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Query to check if the user exists
                    var checkUserQuery = "SELECT Role FROM Users WHERE Email = @Email";
                    using (var checkCommand = new SqlCommand(checkUserQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Email", email);

                        var role = checkCommand.ExecuteScalar() as string;

                        if (role == null)
                        {
                            // If no user is found, return a failure response
                            return Json(new { success = false, message = "User not found." });
                        }

                        // Determine the new role
                        var newRole = role == "Suspended" ? "User" : "Suspended";

                        // Update the user's role
                        var updateRoleQuery = "UPDATE Users SET Role = @NewRole WHERE Email = @Email";
                        using (var updateCommand = new SqlCommand(updateRoleQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@NewRole", newRole);
                            updateCommand.Parameters.AddWithValue("@Email", email);
                            updateCommand.ExecuteNonQuery();
                        }

                        return Json(new { success = true, message = $"User role updated to {newRole}." });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response
                Console.WriteLine($"Error in ToggleUserSuspension: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while processing the request." });
            }
        }


    }
}
