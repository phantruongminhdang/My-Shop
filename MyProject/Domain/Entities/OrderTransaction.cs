
using Domain.Entities.Base;
using Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities
{
    public class OrderTransaction : BaseEntity
    {
        [ForeignKey("Order")]
        public Guid OrderId { get; set; }
        public double Amount { get; set; }
        public string IpnURL { get; set; }
        public string Information { get; set; }
        public string PartnerCode { get; set; }
        public string RedirectUrl { get; set; }
        public string RequestId { get; set; }
        public string RequestType { get; set; }
        public TransactionStatus TransactionStatus { get; set; }
        public string PaymentMethod { get; set; }

        public string OrderIdFormMomo { get; set; }
        public string OrderType { get; set; }
        public long TransId { get; set; }
        public int ResultCode { get; set; }
        public string Message { get; set; }
        public string? PayType { get; set; }
        public long ResponseTime { get; set; }
        public string ExtraData { get; set; }
        public string Signature { get; set; }

        [JsonIgnore]
        public virtual Order Order { get; set; }
    }
}
