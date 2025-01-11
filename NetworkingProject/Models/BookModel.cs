using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetworkingProject.Models
{
    public class BookModel
    {
        [System.ComponentModel.DataAnnotations.Key]
        public string Title { get; set; }
        public string Author { get; set; }
        public string  Publisher { get; set; }
        public float Price { get; set; }
        public float? DiscountPrice { get; set; }
        public float BorrowPrice { get; set; }
        public int PublishingYear { get; set; }
        public string Genre { get; set; }
        public int AgeLim { get; set; }
        public string SelectedFormat { get; set; }
        public string SelectedAction { get; set; }
        public string Picture { get; set; }
        public int Rating { get; set; }
        public string Review { get; set; }
    }
}