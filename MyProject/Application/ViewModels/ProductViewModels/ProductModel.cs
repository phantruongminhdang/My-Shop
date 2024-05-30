using Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations.Schema;

namespace Application.ViewModels.ProductViewModel
{
    public class ProductModel
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double? Price { get; set; }
        [NotMapped]
        public List<String>? OldImage { get; set; } = default!;
        [NotMapped]
        public List<IFormFile>? Image { get; set; } = default!;
    }
}
