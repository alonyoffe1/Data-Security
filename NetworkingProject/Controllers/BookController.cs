using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NetworkingProject.Models;
using System.Configuration;

namespace NetworkingProject.Controllers
{
    public class BookController : Controller
    {

        private readonly BookRepository _bookRepository;

        public ActionResult Index()
        {

            return RedirectToAction("Catalog");
        }

        public BookController()
        {
            string connectionString = "Server=LAPTOP-492M1B9J;Database=NetProj_Web_db;Trusted_Connection=True;";
            _bookRepository = new BookRepository(connectionString);
        }

        public ActionResult Catalog(string sortOrder, string author, string genre, string method, decimal? minPrice, decimal? maxPrice, bool? onSale)
        {
            try
            {
                List<BookModel> books = _bookRepository.GetAllBooks();

                // Filter by author
                if (!string.IsNullOrEmpty(author))
                {
                    books = books.Where(b => b.Author.IndexOf(author, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                }

                // Filter by genre
                if (!string.IsNullOrEmpty(genre))
                {
                    books = books.Where(b => b.Genre.IndexOf(genre, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }

                // Filter by price range
                if (minPrice.HasValue)
                {
                    books = books.Where(b => Convert.ToInt32(b.Price) >= minPrice.Value).ToList();
                }

                if (maxPrice.HasValue)
                {
                    books = books.Where(b => Convert.ToInt32(b.Price) <= maxPrice.Value).ToList();
                }

                // Filter by sale (on sale books)
                if (onSale.HasValue && onSale.Value)
                {
                    books = books.Where(b => b.DiscountPrice != null).ToList(); // Assuming `IsOnSale` is a boolean indicating if the book is discounted
                }

                // Sorting logic
                switch (sortOrder)
                {
                    case "price_asc":
                        books = books.OrderBy(b => b.Price).ToList();
                        break;
                    case "price_desc":
                        books = books.OrderByDescending(b => b.Price).ToList();
                        break;
                    case "popularity":
                        //books = books.OrderByDescending(b => b.Popularity).ToList();
                        break;
                    case "genre":
                        books = books.OrderBy(b => b.Genre).ToList();
                        break;
                    case "year":
                        books = books.OrderByDescending(b => b.PublishingYear).ToList();
                        break;
                    default:
                        books = books.OrderBy(b => b.Title).ToList();
                        break;
                }

                string userRole = Session["UserRole"] as string;
                ViewBag.IsAdmin = userRole == "Admin";
                ViewBag.IsUser = userRole == "User" || ViewBag.IsAdmin;

                return View(books);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Catalog: {ex.Message}");
                return View(new List<BookModel>());
            }
        }

        // Action for the search results page
        [HttpGet]
        public ActionResult Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("Index");
            }

            // Convert query to lowercase for case-insensitive search
            query = query.ToLower();

            // Get books from database where title contains the search query
            List<BookModel> books = _bookRepository.GetAllBooks()
                .Where(b => b.Title.ToLower().Contains(query) ||
                            b.Author.ToLower().Contains(query) ||
                            b.Publisher.ToLower().Contains(query))
                .ToList();

            // Pass the books to the view
            return View(books);
        }

    }

}
