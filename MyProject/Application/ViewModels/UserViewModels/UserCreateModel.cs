using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.ViewModels.UserViewModels
{
    public class UserCreateModel
    {
        public string Email { get; set; }
        public string Fullname { get; set; }
        [NotMapped]
        public IFormFile? Avatar { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
    }
}
