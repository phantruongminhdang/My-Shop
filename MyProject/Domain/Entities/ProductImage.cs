using Domain.Entities.Base;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities
{
    public class ProductImage : BaseEntity
    {
        [ForeignKey("Product")]
        [JsonIgnore]
        public Guid ProductId { get; set; }
        public string? ImageUrl { get; set; }
        [JsonIgnore]
        public virtual Product? Product { get; set; }
    }
}
