using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NetworkingProject.Models;
using System.Configuration;

namespace NetworkingProject.Controllers
{
    public class LibraryController : Controller
    {

        private readonly LibraryRepository _libraryRepository;

        public ActionResult Index()
        {

            return RedirectToAction("Catalog");
        }

        public LibraryController()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["NetProj_Web_db"].ConnectionString;
            _libraryRepository = new LibraryRepository(connectionString);
        }

        public ActionResult Library()
        {
            List<LibraryModel> books = _libraryRepository.GetAllBooks();

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
