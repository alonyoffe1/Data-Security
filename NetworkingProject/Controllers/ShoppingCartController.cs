using Antlr.Runtime.Misc;
using Microsoft.SqlServer.Server;
using NetworkingProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static System.Collections.Specialized.BitVector32;

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
        [RequireHttps] // Ensures this method is only accessible over HTTPS
        public ActionResult ProcessPayment(string ccNumber, string expiryDate, string cvc)
        {
            try
            {
                // Simulate borrowed books availability check
                var borrowedBooks = (List<BookModel>)Session["BorrowedBooks"] ?? new List<BookModel>();
                var unavailableBooks = new List<BookModel>();

                foreach (var book in borrowedBooks.ToList()) // Using ToList() to avoid modification during iteration
                {
                    // Perform a server-side check for availability
                    bool isAvailable = _bookRepository.CheckIfBorrowableCopiesAvailable(book.Title, book.Author);

                    if (!isAvailable)
                    {
                        unavailableBooks.Add(book);
                        borrowedBooks.Remove(book); // Remove unavailable book from the session
                    }
                }

                // Update the session with remaining borrowed books
                Session["BorrowedBooks"] = borrowedBooks;

                // If any unavailable books were found, return an error
                if (unavailableBooks.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Some borrowed books are no longer available.",
                        unavailableBooks = unavailableBooks.Select(b => new { b.Title, b.Author }).ToList()
                    });
                }

                // Validate the input
                if (string.IsNullOrWhiteSpace(ccNumber) || string.IsNullOrWhiteSpace(expiryDate) || string.IsNullOrWhiteSpace(cvc))
                {
                    return Json(new { success = false, message = "Invalid card details" }, JsonRequestBehavior.AllowGet);
                }

                // Simulate payment processing
                bool isPaymentSuccessful = SimulatePaymentProcessing(ccNumber, expiryDate, cvc);
                if (isPaymentSuccessful)
                {
                    return Json(new { success = true, message = "Payment processed successfully" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "Payment failed. Please try again." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessPayment: {ex.Message}");
                return Json(new { success = false, message = "Error during payment processing" }, JsonRequestBehavior.AllowGet);
            }
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
