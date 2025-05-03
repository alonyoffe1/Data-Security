﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel.DataAnnotations;

namespace NetworkingProject.Models
{
    public class SignInModel
    {
        [Required]

        public string Email { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }
        public String Role { get; set; }
    }
}