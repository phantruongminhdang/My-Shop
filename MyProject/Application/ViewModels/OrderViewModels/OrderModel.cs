
namespace Application.ViewModels.OrderViewModels
{
    public class OrderModel
    {
        public OrderInfoModel? OrderInfo { get; set; }
        public string Address { get; set; }
        public string? Note { get; set; }
        public List<Guid> ListProduct { get; set; }

    }
}
