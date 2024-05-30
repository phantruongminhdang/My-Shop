
using Application.ViewModels.OrderViewModels;
using Application.Commons;
using Domain.Entities;
using Domain.Enums;
using Application.Interfaces.Services.Momo;

namespace Application.Interfaces.Services
{
    public interface IOrderService
    {
        public Task<IList<string>> ValidateOrderModel(OrderModel model, string userId);
        public Task<string> CreateOrderAsync(OrderModel model, string userId);
        public Task HandleIpnAsync(MomoRedirect momo);
        public Task<string> PaymentAsync(Guid tempId);
        public Task<Pagination<OrderViewModel>> GetPaginationAsync(string userId, int pageIndex = 0, int pageSize = 10);
        public Task<Order> GetByIdAsync(string userId, Guid orderId);
        public Task UpdateOrderStatusAsync(Guid orderId, OrderStatus orderStatus);
        Task FinishDeliveryOrder(Guid orderId, FinishDeliveryOrderModel finishDeliveryOrderModel);
        Task ShipperAddition(Guid orderId, Guid gardenerId);
    }
}
