using NetworkingProject.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace NetworkingProject.Controllers
{
    public class WaitingListController : Controller
    {
        //The methods below are used to interact with the WaitingList Table
        [HttpPost]
        public JsonResult Add(string title)
        {
            string userEmail = Session["UserEmail"]?.ToString();

            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, message = "User is not signed in." });
            }

            // Retrieve the connection string from the configuration file (Web.config)
            string connectionString = ConfigurationManager.ConnectionStrings["NetProj_Web_db"].ToString();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    // Open the connection to the database
                    connection.Open();


                    // Define the SQL query to check the user's BorrowedBooks count
                    string checkBorrowedBooksQuery = @"
                        SELECT BorrowedBooks 
                        FROM Users 
                        WHERE Email = @Email";

                    SqlCommand checkCommand = new SqlCommand(checkBorrowedBooksQuery, connection);
                    checkCommand.Parameters.AddWithValue("@Email", userEmail);
                    // Check the number of books the user has already borrowed
                    int borrowedBooksCount = (int)checkCommand.ExecuteScalar();

                    if (borrowedBooksCount >= 3)//The maximum number of books a user is allowed to borrow is 3
                    {
                        // Return failure if the user has reached the borrowing limit
                        return Json(new { success = false, message = "You have reached the maximum borrowing limit of 3 books." });
                    }

                    // If the user is allowed, proceed with adding to the waiting list
                    string query = @"
                    INSERT INTO WaitingList (Title, Email, DateAdded, Notified)
                    VALUES (@Title, @Email, GETDATE(), 0)";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Title", title);
                    command.Parameters.AddWithValue("@Email", userEmail);

                    // Execute the query
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {

                        // Return a success response if the insertion is successful
                        return Json(new { success = true });
                    }
                    else
                    {
                        // Return failure if no rows were affected
                        return Json(new { success = false, message = "Failed to add to the waiting list." });
                    }
                }
                catch (Exception ex)
                {
                    // Log the error (optional) and return an error response
                    return Json(new { success = false, message = "An error occurred: " + ex.Message });
                }
            }
        }
        //The methods below are used to interact with the BorrowedBooks Table
        public ActionResult AddToBorrowedBooks(string userEmail, string bookTitle)
        {
            // Retrieve the borrow date and due date (this can be dynamic or fixed)
            DateTime borrowDate = DateTime.Now;
            DateTime dueDate = borrowDate.AddDays(30); // Assuming a 30-day borrowing period

            // Insert the user into BorrowedBooks table
            string connectionString = ConfigurationManager.ConnectionStrings["NetProj_Web_db"].ToString();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // 1. Insert the record into BorrowedBooks table
                    string query = "INSERT INTO BorrowedBooks (UserEmail, BookTitle, BorrowDate, DueDate) " +
                                   "VALUES (@UserEmail, @BookTitle, @BorrowDate, @DueDate)";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserEmail", userEmail);
                        cmd.Parameters.AddWithValue("@BookTitle", bookTitle);
                        cmd.Parameters.AddWithValue("@BorrowDate", borrowDate);
                        cmd.Parameters.AddWithValue("@DueDate", dueDate);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            return Json(new { success = false, message = "Failed to borrow the book." }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // 2. Increment the BorrowedBooks count for the user
                    string incrementBorrowedBooksQuery = @"
                        UPDATE Users
                        SET BorrowedBooks = BorrowedBooks + 1
                        WHERE Email = @Email";
                    using (SqlCommand incrementCommand = new SqlCommand(incrementBorrowedBooksQuery, connection))
                    {
                        incrementCommand.Parameters.AddWithValue("@Email", userEmail);
                        int rowsUpdated = incrementCommand.ExecuteNonQuery();
                        if (rowsUpdated == 0)
                        {
                            return Json(new { success = false, message = "Failed to update user's borrowed books count." }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // 3. Subtract 1 from BorrowCopies in the Books table for the specific book
                    string decrementBorrowCopiesQuery = @"
                        UPDATE Books
                        SET BorrowCopies = BorrowCopies - 1
                        WHERE Title = @BookTitle";
                    using (SqlCommand decrementCommand = new SqlCommand(decrementBorrowCopiesQuery, connection))
                    {
                        decrementCommand.Parameters.AddWithValue("@BookTitle", bookTitle);
                        int rowsUpdated = decrementCommand.ExecuteNonQuery();
                        if (rowsUpdated == 0)
                        {
                            return Json(new { success = false, message = "Failed to update book borrow count." }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // If everything was successful, return success
                    return Json(new { success = true, message = "Book successfully borrowed." }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    // In case of an error, return a failure message
                    return Json(new { success = false, message = "An error occurred: " + ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
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
                        SELECT UserEmail, BookTitle, ReturnDate
                        FROM BorrowedBooks
                        WHERE ReturnDate BETWEEN GETDATE() AND DATEADD(DAY, 5, GETDATE())";

                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        string userEmail = reader["UserEmail"].ToString();
                        string bookTitle = reader["BookTitle"].ToString();
                        DateTime returnDate = Convert.ToDateTime(reader["ReturnDate"]);

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
        public void SendNotification(string userEmail, string bookTitle, DateTime returnDate)
        {
            string subject = "Reminder: Your Borrowed Book is Due Soon!";
            string body = $"Dear user,\n\n" +
                          $"This is a reminder that the book '{bookTitle}' you borrowed is due for return on {returnDate.ToString("MMMM dd, yyyy")}, " +
                          $"which is in 5 days. Please make sure to return it on time.\n\n" +
                          $"Best regards,\nYour Library Team";

            // Example: Sending email
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
