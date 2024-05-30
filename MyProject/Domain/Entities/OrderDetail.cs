using Domain.Entities.Base;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities
{
    public class OrderDetail : BaseEntity
    {
        [ForeignKey("Order")]
        public Guid OrderId { get; set; }
        [ForeignKey("Product")]
        public Guid ProductId { get; set; }
        public double Price { get; set; }
        [JsonIgnore]
        public virtual Order Order { get; set; }
        public virtual Product Product { get; set; }

    }
}
