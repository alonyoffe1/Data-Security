using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetworkingProject.Models
{
    public class LibraryModel
    {
        [Key]
        [Required]
        [MaxLength(255)]
        public string Title { get; set; }

        [Required]
        [MaxLength(255)]
        public string Author { get; set; }

        [Required]
        [MaxLength(255)]
        public string Publisher { get; set; }

        [Required]
        [MaxLength(100)]
        public string Genre { get; set; }

        [Required]
        [Range(0, 120, ErrorMessage = "Age limit must be between 0 and 120.")]
        public int AgeLim { get; set; }

        [Required]
        public bool Borrowed { get; set; }
    }
}
