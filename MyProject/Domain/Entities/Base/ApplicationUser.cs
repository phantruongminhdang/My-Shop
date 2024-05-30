using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace Domain.Entities.Base
{
    public class ApplicationUser : IdentityUser
    {
        public string? Fullname { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsRegister { get; set; } = false;


        [JsonIgnore]
        public virtual Customer? Customer { get; set; }
        public virtual Manager? Manager { get; set; }
    }
}
