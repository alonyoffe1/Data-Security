using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NetworkingProject.Models;

namespace AudioBookStore.Controllers
{
    public class HomePageController : Controller
    {
        private readonly BookRepository _bookRepository;

        public HomePageController()
        {
            string connectionString = "Server=localhost;Database=NetProj_Web_db;Trusted_Connection=True;";
            _bookRepository = new BookRepository(connectionString);
        }

        // Action for the main page
        public ActionResult Index()
        {
            // Redirect to the Catalog action in the Books controller
            return RedirectToAction("Index", "Book");
        }

        public ActionResult RedirectToBooks()
        {
            return RedirectToAction("Index", "Catalog");  // Redirects to the Books controller Index action
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
