using NetworkingProject.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NetworkingProject.Controllers
{
    public class AccountsController : Controller
    {
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
                string connectionString = ConfigurationManager.ConnectionStrings["NetProj_Web_db"].ToString();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        connection.Open();

                        // Query to check if the email and password match
                        string query = "SELECT Role FROM Users WHERE Email = @Email AND Password = @Password";
                        using (SqlCommand cmd = new SqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@Email", model.Email);
                            cmd.Parameters.AddWithValue("@Password", model.Password);

                            string role = (string)cmd.ExecuteScalar(); //retrieve the role as a string from the db
                            Session["UserRole"] = role;
                            if (role == "Admin")
                            {
                                return RedirectToAction("Index", "HomePage");
                            }
                            else if (role == "User")
                            {
                                return RedirectToAction("Index", "HomePage");
                            }
                            else
                            {
                                ModelState.AddModelError("", "Invalid role.");
                            }
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        // Log error (you can use a logger instead of displaying the exception)
                        ModelState.AddModelError("", "An error occurred while processing your request: " + ex.Message);
                    }
                }
            }

            // If validation fails or login is unsuccessful, return the same view with errors
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
                            cmd.Parameters.AddWithValue("@Password", model.Password); 
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
    }
}