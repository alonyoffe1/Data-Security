using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NetworkingProject.Models;
using System.Configuration;
using System.Data.SqlClient;

namespace NetworkingProject.Controllers
{
    public class LibraryController : Controller
    {

        public ActionResult Library()
        {
            string userEmail = Session["UserEmail"] as string;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account"); // Redirect to login if the user is not logged in
            }

            string connectionString = "Server=localhost;Database=NetProj_Web_db;Trusted_Connection=True;";
            List<BookModel> libraryBooks = new List<BookModel>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT BookTitle, TypeOfPurchase, Format FROM Library WHERE UserEmail = @UserEmail";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserEmail", userEmail);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                libraryBooks.Add(new BookModel
                                {
                                    Title = reader["BookTitle"].ToString(),
                                    SelectedAction = reader["TypeOfPurchase"].ToString(),
                                    SelectedFormat = reader["Format"].ToString()
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception (e.g., log the error)
                    ViewBag.ErrorMessage = "An error occurred while retrieving your library: " + ex.Message;
                }
            }

            return View(libraryBooks);
        }

        public ActionResult AddToLibrary(string userEmail, List<BookModel> cart, List<BookModel> borrowedBooks)
        {
            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, message = "User email is required" }, JsonRequestBehavior.AllowGet);
            }

            string connectionString = ConfigurationManager.ConnectionStrings["NetProj_Web_db"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            int insertedBooks = 0;

                            // Process bought books
                            if (cart != null && cart.Any())
                            {
                                foreach (var book in cart)
                                {
                                    string insertQuery = @"
                                IF NOT EXISTS (
                                    SELECT 1 FROM Library 
                                    WHERE UserEmail = @UserEmail 
                                    AND BookTitle = @BookTitle
                                )
                                BEGIN
                                    INSERT INTO Library (
                                        UserEmail, BookTitle, TypeOfPurchase, 
                                        Format, DateAdded
                                    )
                                    VALUES (
                                        @UserEmail, @BookTitle, @TypeOfPurchase, 
                                        @Format, GETDATE()
                                    )
                                END";

                                    using (SqlCommand cmd = new SqlCommand(insertQuery, connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@UserEmail", userEmail);
                                        cmd.Parameters.AddWithValue("@BookTitle", book.Title);
                                        cmd.Parameters.AddWithValue("@TypeOfPurchase", book.SelectedAction);
                                        cmd.Parameters.AddWithValue("@Format", book.SelectedFormat ?? "Digital");
                                        insertedBooks += cmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            // Process borrowed books
                            if (borrowedBooks != null && borrowedBooks.Any())
                            {
                                foreach (var book in borrowedBooks)
                                {
                                    string insertQuery = @"
                                IF NOT EXISTS (
                                    SELECT 1 FROM Library 
                                    WHERE UserEmail = @UserEmail 
                                    AND BookTitle = @BookTitle
                                )
                                BEGIN
                                    INSERT INTO Library (
                                        UserEmail, BookTitle, TypeOfPurchase, 
                                        Format, DateAdded
                                    )
                                    VALUES (
                                        @UserEmail, @BookTitle, 'Borrow', 
                                        @Format, GETDATE()
                                    )
                                END";

                                    using (SqlCommand cmd = new SqlCommand(insertQuery, connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@UserEmail", userEmail);
                                        cmd.Parameters.AddWithValue("@BookTitle", book.Title);
                                        cmd.Parameters.AddWithValue("@Format", book.SelectedFormat ?? "Digital");
                                        insertedBooks += cmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            transaction.Commit();
                            Console.WriteLine($"Successfully added {insertedBooks} books to library for user {userEmail}");
                            return Json(new { success = true, message = "Books successfully added to library" }, JsonRequestBehavior.AllowGet);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error adding books to library: {ex.Message}");
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database error: {ex.Message}");
                    return Json(new { success = false, message = $"Failed to add books to library: {ex.Message}" }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        public int? GetDaysUntilReturn(string userEmail, string bookTitle)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["NetProj_Web_db"].ToString();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = @"
                SELECT DATEDIFF(DAY, GETDATE(), DueDate) AS DaysRemaining
                FROM BorrowedBooks
                WHERE UserEmail = @UserEmail AND BookTitle = @BookTitle";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UserEmail", userEmail);
                    command.Parameters.AddWithValue("@BookTitle", bookTitle);

                    object result = command.ExecuteScalar();

                    if (result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                }
                catch (Exception ex)
                {
                    // Handle error (e.g., logging)
                    Console.WriteLine("Error: " + ex.Message);
                }
            }

            return null; // Return null if no due date is found or an error occurs
        }

        public ActionResult DownloadBook(string bookTitle, string format)
        {
            // Simulated file content based on book format
            string fileContent = $"This is the content of {bookTitle} in {format} format.";
            string fileName = $"{bookTitle}.{format.ToLower()}";

            // Convert content to byte array
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(fileContent);

            // Return file for download
            return File(fileBytes, "application/octet-stream", fileName);
        }

        public ActionResult RemoveFromLibrary(string bookTitle)
        {
            string userEmail = (string)Session["UserEmail"];
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Library"); // Redirect to the library view
            }

            string connectionString = ConfigurationManager.ConnectionStrings["NetProj_Web_db"].ToString();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string deleteQuery = @"
                DELETE FROM Library
                WHERE UserEmail = @UserEmail AND BookTitle = @BookTitle AND TypeOfPurchase = 'Buy'";

                    using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserEmail", userEmail);
                        command.Parameters.AddWithValue("@BookTitle", bookTitle);
                        command.ExecuteNonQuery();
                    }

                    return RedirectToAction("Library"); // Redirect back to the Library view after deletion
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                    return RedirectToAction("Library");
                }
            }
        }
        [HttpPost]
        public ActionResult RateAndReview(string bookTitle, string userEmail, int rating, string review)
        {
            if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(bookTitle))
            {
                return Json(new { success = false, message = "Invalid parameters" });
            }

            string connectionString = "Server=localhost;Database=NetProj_Web_db;Trusted_Connection=True;";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Test the connection explicitly
                    connection.Open();

                    string updateQuery = @"
                UPDATE Books 
                SET Rating = @Rating, 
                    Review = @Review
                WHERE Title = @BookTitle";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@BookTitle", bookTitle);
                        cmd.Parameters.AddWithValue("@Rating", rating);
                        cmd.Parameters.AddWithValue("@Review", review ?? (object)DBNull.Value);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return Json(new { success = true, message = "Rating saved successfully" });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Book not found or no changes made" });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // Log the detailed error
                System.Diagnostics.Debug.WriteLine($"SQL Error: {ex.Message}, Number: {ex.Number}");
                return Json(new { success = false, message = $"Database error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                // Log the general error
                System.Diagnostics.Debug.WriteLine($"General Error: {ex.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }



    }

}
