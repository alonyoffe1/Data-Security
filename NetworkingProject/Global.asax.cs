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

                        var emailService = new EmailService();
                        string subject = "Borrow time is about to run out";
                        string body = $@"
                                <html>
                                <body>
                                        <h2>Borrow time is about to run out!</h2>
                                        <p>Dear Customer,</p>
                                        <p>We would like to notify you that your borrow period for {bookTitle} will be over in 5 days</p>
                                        <p>Make sure to finish the book beforehand!</p>
                                        <p>Otherwise you can always Buy the book or borrow it again!</p>

                                        <p>Best regards,</p>
                                        <p>Your Bookstore Team</p>
                                </body>
                                </html>";

                        // Send the email
                        emailService.SendEmail(userEmail, subject, body);
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
    }
}

