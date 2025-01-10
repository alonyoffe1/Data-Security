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

            string connectionString = ConfigurationManager.ConnectionStrings["NetProj_Web_db"].ToString();
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

            if (string.IsNullOrEmpty(userEmail) || (cart == null && borrowedBooks == null))
            {
                return Json(new { success = false, message = "No data to process or user not logged in." }, JsonRequestBehavior.AllowGet);
            }

            string connectionString = ConfigurationManager.ConnectionStrings["NetProj_Web_db"].ToString();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Insert only "Buy" books from Cart into Library
                    if (cart != null)
                    {
                        var booksToBuy = cart.Where(book => book.SelectedAction == "Buy").ToList();

                        foreach (var book in booksToBuy)
                        {
                            string insertQuery = @"
                        INSERT INTO Library (UserEmail, BookTitle, TypeOfPurchase, Format)
                        SELECT @UserEmail, @BookTitle, 'Buy', @Format
                        WHERE NOT EXISTS (
                            SELECT 1 FROM Library WHERE UserEmail = @UserEmail AND BookTitle = @BookTitle
                        )";

                            using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                            {
                                cmd.Parameters.AddWithValue("@UserEmail", userEmail);
                                cmd.Parameters.AddWithValue("@BookTitle", book.Title);
                                cmd.Parameters.AddWithValue("@Format", book.SelectedFormat ?? "Unknown"); // e.g., PDF, ePub
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // Insert borrowed books from BorrowedCart into Library
                    if (borrowedBooks != null)
                    {
                        foreach (var book in borrowedBooks)
                        {
                            string insertQuery = @"
                        INSERT INTO Library (UserEmail, BookTitle, TypeOfPurchase, Format)
                        SELECT @UserEmail, @BookTitle, 'Borrow', @Format
                        WHERE NOT EXISTS (
                            SELECT 1 FROM Library WHERE UserEmail = @UserEmail AND BookTitle = @BookTitle
                        )";

                            using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                            {
                                cmd.Parameters.AddWithValue("@UserEmail", userEmail);
                                cmd.Parameters.AddWithValue("@BookTitle", book.Title);
                                cmd.Parameters.AddWithValue("@Format", book.SelectedFormat ?? "Unknown");
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    return Json(new { success = true, message = "Books successfully added to library." }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    // Log error
                    return Json(new { success = false, message = "An error occurred: " + ex.Message }, JsonRequestBehavior.AllowGet);
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



    }

}
