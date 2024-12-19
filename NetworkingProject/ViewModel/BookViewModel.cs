using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NetworkingProject.Models;

namespace NetworkingProject.ViewModel
{
    public class BookViewModel
    {
        public BookModel book { get; set; }
        public List<BookModel> bookList { get; set; }
    }
}