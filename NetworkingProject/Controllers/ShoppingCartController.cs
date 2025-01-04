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

        public ShoppingCartController() //initialies book controller for methods to use
        {
            string connectionString = "Server=localhost;Database=NetProj_Web_db;Trusted_Connection=True;";
            _bookRepository = new BookRepository(connectionString);
        }

        public ActionResult AddToCart(string title, string author, string format, string typeofaction)
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
                //shows book information
                Session["Cart"] = cart;

            }
            else
            {
                // Handle the case where the book is not found
                return RedirectToAction("Error");
            }

            return RedirectToAction("CartPageWithCart");
        }


        // Action to display the cart with the list of books
        public ActionResult CartPageWithCart()
        {
            return View(Session["Cart"]);
        }
    }
}