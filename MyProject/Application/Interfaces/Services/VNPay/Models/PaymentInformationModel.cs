namespace Application.Interfaces.Services.VNPay.Models
{
    public class PaymentInformationModel
    {
        public Guid Id { get; set; }
        public string OrderType { get; set; }
        public double Amount { get; set; }
        public string OrderDescription { get; set; }
        public string? Name { get; set; }
    }
}
