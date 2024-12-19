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

        public BookController()
        {
            string connectionString = "Server=localhost;Database=ebooks;Trusted_Connection=True;";
            _bookRepository = new BookRepository(connectionString);
        }

        public ActionResult Catalog()
        {
            List<BookModel> books = _bookRepository.GetAllBooks();
            return View(books);
        }
    }
}