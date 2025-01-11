using Antlr.Runtime.Misc;
using Microsoft.SqlServer.Server;
using NetworkingProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static System.Collections.Specialized.BitVector32;
using System.Net;
using System.Net.Mail;
using System.Security.Policy;
using System.Web.Helpers;

namespace NetworkingProject.Controllers
{
    public class ShoppingCartController : Controller
    {

        private readonly BookRepository _bookRepository;

        public ShoppingCartController() //initializes book controller for methods to use
        {
            string connectionString = "Server=localhost;Database=NetProj_Web_db;Trusted_Connection=True;";
            _bookRepository = new BookRepository(connectionString);
        }

        public ActionResult AddToCart(string title, string author, string format, string typeofaction)
        {
            try
            {
                // Query the database to ensure the book exists and has consistent data
                BookModel book = _bookRepository.GetBookByDetails(title, author);

                if (book != null)
                {
                    // Add the book to the cart
                    if (Session["Cart"] == null)
                        Session["Cart"] = new List<BookModel>();

                    var cart = (List<BookModel>)Session["Cart"];

                    book.SelectedFormat = format;
                    book.SelectedAction = typeofaction;
                    cart.Add(book);

                    // Shows book information
                    Session["Cart"] = cart;

                    // If the action is "Borrow", store it in the borrowed books list
                    if (typeofaction == "Borrow")
                    {
                        if (Session["BorrowedBooks"] == null)
                            Session["BorrowedBooks"] = new List<BookModel>();

                        var borrowedBooks = (List<BookModel>)Session["BorrowedBooks"];
                        borrowedBooks.Add(book);
                        Session["BorrowedBooks"] = borrowedBooks;
                    }
                }
                else
                {
                    // Handle the case where the book is not found
                    return RedirectToAction("Error");
                }

                return RedirectToAction("CartPageWithCart");
            }
            catch (Exception ex)
            {
                // Log or handle the error
                Console.WriteLine($"Error in AddToCart: {ex.Message}");
                return RedirectToAction("Error");
            }
        }

        public ActionResult RemoveFromCartSession(string title)
        {
            try
            {
                // Retrieve the cart session
                var cart = Session["Cart"] as List<BookModel>;

                if (cart != null)
                {
                    // Find and remove the book with the given title
                    var bookToRemove = cart.FirstOrDefault(b => b.Title == title);
                    if (bookToRemove != null)
                    {
                        cart.Remove(bookToRemove);
                        Session["Cart"] = cart; // Update the session
                    }
                }

                // Redirect back to the shopping cart page
                return RedirectToAction("CartPageWithCart");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveFromCartSession: {ex.Message}");
                return RedirectToAction("Error");
            }
        }

        // Action to display the cart with the list of books
        public ActionResult CartPageWithCart()
        {
            try
            {
                return View(Session["Cart"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CartPageWithCart: {ex.Message}");
                return RedirectToAction("Error");
            }
        }

        [HttpGet]
        public JsonResult CheckBorrowAvailability(string title, string author)
        {
            try
            {
                bool isAvailable = _bookRepository.CheckIfBorrowableCopiesAvailable(title, author);
                return Json(new { isAvailable }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log or handle the error as needed
                Console.WriteLine($"Error in CheckBorrowAvailability: {ex.Message}");
                return Json(new { isAvailable = false }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult Checkout()
        {
            try
            {
                // Initialize borrowedBooks and cart sessions
                var borrowedBooks = (List<BookModel>)Session["BorrowedBooks"] ?? new List<BookModel>();
                var cart = (List<BookModel>)Session["Cart"] ?? new List<BookModel>();

                var unavailableBooks = new List<BookModel>();
                float totalPrice = 0;

                // Step 1: Check availability for borrowed books
                foreach (var book in borrowedBooks.ToList()) // Using ToList() to avoid modification during iteration
                {
                    // Perform a server-side check for availability
                    bool isAvailable = _bookRepository.CheckIfBorrowableCopiesAvailable(book.Title, book.Author);

                    if (!isAvailable)
                    {
                        unavailableBooks.Add(book);
                        borrowedBooks.Remove(book);
                    }
                    else
                    {
                        totalPrice += book.BorrowPrice; // Add rental fee for borrowed books
                    }
                }

                // Step 2: Calculate the price of books in Cart with "Buy" action
                foreach (var book in cart)
                {
                    if (book.SelectedAction == "Buy")
                    {
                        totalPrice += book.Price; // Add price for books being bought from the Cart
                    }
                }

                // Step 3: Update the session with the remaining borrowedBooks and cart (after removing unavailable books)
                Session["BorrowedBooks"] = borrowedBooks;

                // Step 4: Return the JSON response with total price and unavailable books
                return Json(new
                {
                    success = true,
                    totalPrice = totalPrice,
                    unavailableBooks = unavailableBooks.Select(b => new { title = b.Title, author = b.Author }).ToList()
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Checkout: {ex.Message}");
                return Json(new { success = false, message = "Error during checkout" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [RequireHttps]
        public ActionResult ProcessPayment(string ccNumber, string expiryDate, string cvc)
        {
            try
            {
                string userEmail = (string)Session["UserEmail"];
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Json(new { success = false, message = "User not logged in" });
                }

                // Validate cart exists
                var cart = Session["Cart"] as List<BookModel>;
                var borrowedBooks = Session["BorrowedBooks"] as List<BookModel>;

                if ((cart == null || !cart.Any()) && (borrowedBooks == null || !borrowedBooks.Any()))
                {
                    return Json(new { success = false, message = "Cart is empty" });
                }

                // Process payment
                if (!SimulatePaymentProcessing(ccNumber, expiryDate, cvc))
                {
                    return Json(new { success = false, message = "Payment failed. Please check your card details." });
                }

                // Add books to library
                var libraryController = new LibraryController();
                var result = libraryController.AddToLibrary(userEmail, cart, borrowedBooks);

                // Check the result
                if (result is JsonResult jsonResult && jsonResult.Data != null)
                {
                    dynamic resultData = jsonResult.Data;
                    if (resultData.success == true)
                    {
                        // Process borrowed books if any
                        if (borrowedBooks != null && borrowedBooks.Any())
                        {
                            var waitingListController = new WaitingListController();
                            foreach (var book in borrowedBooks)
                            {
                                waitingListController.AddToBorrowedBooks(userEmail, book.Title);
                            }
                        }

                        // Send confirmation email
                        try
                        {
                            SendConfirmationEmail(userEmail, cart, borrowedBooks);
                        }
                        catch (Exception emailEx)
                        {
                            Console.WriteLine($"Email error: {emailEx.Message}");
                        }

                        // Clear cart sessions
                        Session["Cart"] = null;
                        Session["BorrowedBooks"] = null;

                        return Json(new
                        {
                            success = true,
                            message = "Payment successful and books added to your library"
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Failed to add books to library: {resultData.message}");
                    }
                }

                return Json(new
                {
                    success = false,
                    message = "Failed to add books to library. Please try again."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessPayment: {ex.Message}");
                return Json(new { success = false, message = "An error occurred during processing" });
            }
        }

        private void SendConfirmationEmail(string userEmail, List<BookModel> cart, List<BookModel> borrowedBooks)
        {
            var emailService = new EmailService();
            var subject = "Your Book Purchase Confirmation";

            var bodyBuilder = new System.Text.StringBuilder();
            bodyBuilder.AppendLine("<h2>Thank you for your purchase!</h2>");
            bodyBuilder.AppendLine("<h3>Your books:</h3><ul>");

            if (cart != null)
            {
                foreach (var book in cart)
                {
                    bodyBuilder.AppendLine($"<li>{book.Title} by {book.Author} ({book.SelectedFormat}) - {book.SelectedAction}</li>");
                }
            }

            if (borrowedBooks != null)
            {
                foreach (var book in borrowedBooks)
                {
                    bodyBuilder.AppendLine($"<li>{book.Title} by {book.Author} ({book.SelectedFormat}) - Borrowed</li>");
                }
            }

            bodyBuilder.AppendLine("</ul>");
            bodyBuilder.AppendLine("<p>You can access your books in your library now.</p>");

            emailService.SendEmail(userEmail, subject, bodyBuilder.ToString());
        }


        // Simulated payment processing logic
        private bool SimulatePaymentProcessing(string ccNumber, string expiryDate, string cvc)
        {
            try
            {
                // Check if the credit card number is exactly 16 digits
                if (ccNumber.Length != 16 || !ccNumber.All(char.IsDigit))
                {
                    return false;
                }

                // Check if the expiry date is in the format MM/YY
                if (expiryDate.Length != 5 || expiryDate[2] != '/')
                {
                    return false;
                }

                string[] dateParts = expiryDate.Split('/');
                if (!int.TryParse(dateParts[0], out int month) || !int.TryParse(dateParts[1], out int year) || month < 1 || month > 12)
                {
                    return false;
                }

                // Check if the CVC is exactly 3 digits
                if (cvc.Length != 3 || !cvc.All(char.IsDigit))
                {
                    return false;
                }

                // If all checks pass, simulate a successful payment
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SimulatePaymentProcessing: {ex.Message}");
                return false;
            }
        }

       
    }
}
