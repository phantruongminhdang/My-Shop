using Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class DeliveryImage : BaseEntity
    {
        [ForeignKey("Order")]
        public Guid OrderId { get; set; }
        public string Image { get; set; }
        public virtual Order Order { get; set; }
    }
}
