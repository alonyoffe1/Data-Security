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
                // Replace this with real authentication logic
                if (IsValidUser(model))
                {
                    // Redirect to a logged-in page, such as "Index" of "Dashboard"
                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    // Add an error message for invalid credentials
                    ModelState.AddModelError("", "Invalid email or password.");
                }
            }

            // If validation fails, return the view with the current model
            return View(model);
        }


        private bool IsValidUser(SignInModel model)
        {
            // Log or debug to check values
            Debug.WriteLine($"Email: {model.Email}, Password: {model.Password}");
            return true;
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
                string connectionString = ConfigurationManager.ConnectionStrings["MyDbConnection"].ToString();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        connection.Open();

                        string query = "INSERT INTO Users (Email, Password, FirstName, LastName, PhoneNumber, DateOfBirth) " +
                                       "VALUES (@Email, @Password, @FirstName, @LastName, @PhoneNumber, @DateOfBirth)";

                        using (SqlCommand cmd = new SqlCommand(query, connection))
                        {
                            // Add parameters to prevent SQL injection
                            cmd.Parameters.AddWithValue("@Email", model.Email);
                            cmd.Parameters.AddWithValue("@Password", model.Password); 
                            cmd.Parameters.AddWithValue("@FirstName", model.FirstName);
                            cmd.Parameters.AddWithValue("@LastName", model.LastName);
                            cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
                            cmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth);

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