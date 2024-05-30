using Domain.Entities.Base;
using System.Text.Json.Serialization;

namespace Domain.Entities
{
    public class Category : BaseEntity
    {
        public required string Name { get; set; }
        [JsonIgnore]
        public IList<Product>? Products { get; set; }
    }
}
