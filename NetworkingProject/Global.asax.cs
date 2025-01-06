using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Configuration;
namespace NetworkingProject
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            CheckDueDatesAndSendNotifications();
        }

        public void CheckDueDatesAndSendNotifications()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["NetProj_Web_db"].ToString();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Query to get users with due dates within 5 days
                    string query = @"
                    SELECT UserEmail, BookTitle, DueDate
                    FROM BorrowedBooks
                    WHERE DueDate BETWEEN GETDATE() AND DATEADD(DAY, 5, GETDATE())";

                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        string userEmail = reader["UserEmail"].ToString();
                        string bookTitle = reader["BookTitle"].ToString();
                        DateTime returnDate = Convert.ToDateTime(reader["DueDate"]);
                        // Calculate how many days are left for the due date
                        int daysLeft = (returnDate - DateTime.Now).Days;

                        // Log how many days are left
                        System.Diagnostics.Debug.WriteLine($"User {userEmail} has {daysLeft} days left to return the book '{bookTitle}'.");

                        // Send email notification (example)
                        SendNotification(userEmail, bookTitle, returnDate);
                    }

                    reader.Close();
                }
                catch (Exception ex)
                {
                    // Handle error (optional logging)
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }

        // Method to send the email notifications
        public void SendNotification(string userEmail, string bookTitle, DateTime returnDate)
        {
            string subject = "Reminder: Your Borrowed Book is Due Soon!";
            string body = $"Dear user,\n\n" +
                          $"This is a reminder that the book '{bookTitle}' you borrowed is due for return on {returnDate.ToString("MMMM dd, yyyy")}, " +
                          $"which is in 5 days. Please make sure to return it on time.\n\n" +
                          $"Best regards,\nYour Library Team";

            try
            {
                SmtpClient smtpClient = new SmtpClient("smtp.example.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential("your-email@example.com", "your-email-password"),
                    EnableSsl = true,
                };
                smtpClient.Send("your-email@example.com", userEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email sending failed: " + ex.Message);
            }
        }
    }
}

