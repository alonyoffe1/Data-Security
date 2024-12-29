using NetworkingProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NetworkingProject.Controllers
{
    public class ShoppingCartController : Controller
    {

        private readonly BookRepository _bookRepository;

        public ShoppingCartController() //initialies book controller for methods to use
        {
            string connectionString = "Server=LAPTOP-492M1B9J;Database=NetProj_Web_db;Trusted_Connection=True;";
            _bookRepository = new BookRepository(connectionString);
        }

        // GET: ShoppingCart
        public ActionResult CartPage()
        {
            return View();
        }

        public ActionResult SelectFormat(string title, string author)
        {
            ViewBag.Title = title;
            ViewBag.Author = author;
            return View();
        }
        public ActionResult AddToCart(string title, string author)
        {
            // Query the database to ensure the book exists and has consistent data
            BookModel book = _bookRepository.GetBookByDetails(title, author);
            

            if (book != null)
            {
                // Add the book to the cart
                if (Session["Cart"] == null)
                    Session["Cart"] = new List<BookModel>();

                var cart = (List<BookModel>)Session["Cart"];
                cart.Add(book);
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
            return View(Session["Cart"]); // Pass the cart items to the view
        }
    }
}