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

        public ActionResult Catalog()
        {
            List<BookModel> books = _bookRepository.GetAllBooks();

            string userRole = Session["UserRole"] as string;

            // You can now check the role and perform logic accordingly
            bool isAdmin = userRole == "Admin";
            bool isUser = userRole == "User" || isAdmin; // Admins are considered users

            // Pass role data to the view
            ViewBag.IsAdmin = isAdmin;
            ViewBag.IsUser = isUser;

            return View(books);
        }

    }

}
