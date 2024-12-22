using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AudioBookStore.Controllers
{
    public class HomePageController : Controller
    {
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
            ViewBag.Query = query ?? "No query provided"; // Show "No query provided" if the search bar is empty.
            ViewBag.Message = "Search functionality is under development.";
            return View(); // Looks for Views/HomePage/Search.cshtml
        }
    }
}
