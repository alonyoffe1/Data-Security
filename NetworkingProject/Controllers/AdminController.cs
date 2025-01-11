using NetworkingProject.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static NetworkingProject.Models.BookRepository;

namespace NetworkingProject.Controllers
{
    public class AdminController : Controller
    {
        private readonly BookRepository _bookRepository;

        public AdminController() //initialies book controller for methods to use
        {
            string connectionString = "Server=LAPTOP-492M1B9J;Database=NetProj_Web_db;Trusted_Connection=True;";
            _bookRepository = new BookRepository(connectionString);
        }

        public ActionResult AddBook(string title, string author, string publisher, decimal price, int year, string genre, int age, int rating, string review)
        {
            try
            {
                AddBookResult result = _bookRepository.AddNewBook(title, author, publisher, price, year, genre, age,rating,review);

                switch (result)
                {
                    case AddBookResult.Success:
                        TempData["SuccessMessage"] = "Book added successfully.";
                        break;
                    case AddBookResult.AlreadyExists:
                        TempData["ErrorMessage"] = "The book already exists in the database.";
                        break;
                    case AddBookResult.Failure:
                        TempData["ErrorMessage"] = "Failed to add the book. Please try again.";
                        break;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Catalog", "Book");
        }



        public ActionResult SetDiscount(string title, decimal discountPrice, string discountPeriod)
        {
            try
            {

                // Call the repository or database logic to set the discount for the book
                bool isDiscountSet = _bookRepository.SetDiscountByTitle(title, discountPrice, discountPeriod);

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            // Redirect back to the catalog page
            return RedirectToAction("Catalog", "Book");
        }


        // GET: Admin
        [HttpGet]
        public ActionResult DeleteBook(string id) // 'id' corresponds to the book title
        {
            try
            {
                // Call the repository or database logic to delete the book
                bool isDeleted = _bookRepository.DeleteBookByTitle(id);

                if (isDeleted)
                {
                    TempData["SuccessMessage"] = "Book deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete the book. Please try again.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            // Redirect back to the catalog page
            return RedirectToAction("Catalog", "Book");
        }

    }
}