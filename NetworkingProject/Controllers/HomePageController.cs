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
            string connectionString = "Server=LAPTOP-492M1B9J;Database=NetProj_Web_db;Trusted_Connection=True;";
            _bookRepository = new BookRepository(connectionString);
        }

        // Action for the main page
        public ActionResult Index()
        {
            // Redirect to the Catalog action in the Books controller
            return RedirectToAction("Index", "Book");
        }


    }
}
