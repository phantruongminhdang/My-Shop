using Application.Interfaces.Services;
using Application.ViewModels.DashboardViewModels;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class DashBoardService : IDashBoardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashBoardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<DashboardViewModel> GetDashboardAsync()
        {

            var newUser = await _unitOfWork.CustomerRepository.GetAsync(isTakeAll: true, expression: x => x.CreationDate >= DateTime.Now.AddDays(-30));
            var newOrder = await _unitOfWork.OrderRepository.GetAsync(isTakeAll: true, expression: x => x.CreationDate >= DateTime.Now.AddDays(-30) && x.OrderStatus >= Domain.Enums.OrderStatus.Paid);
            double totalOrderIncome = newOrder.Items.Sum(item => item.TotalPrice);
            DashboardViewModel dashboardViewModel = new DashboardViewModel()
            {
                NewUser = newUser.Items.Count,
                NewOrder = newOrder.Items.Count,
                TotalOrderIncome = totalOrderIncome,                
                OrderCircleGraphs = await GetOrderCircleGraph(totalOrderIncome),
            };
            return dashboardViewModel;
        }
        private async Task<List<OrderCircleGraph>> GetOrderCircleGraph(double totalOrderIncome)
        {
            if (totalOrderIncome == 0) return new List<OrderCircleGraph>();
            List<OrderCircleGraph> orderCircleGraphs = new List<OrderCircleGraph>();
            var categories = await _unitOfWork.CategoryRepository.GetAsync(isTakeAll: true, expression: x => !x.IsDeleted);
            foreach (Category category in categories.Items)
            {
                var orderDetail = await _unitOfWork.OrderDetailRepository.GetAllQueryable()
                    .AsNoTracking()
                    .Where(x => x.Product.CategoryId == category.Id && x.Order.OrderStatus >= Domain.Enums.OrderStatus.Paid && x.CreationDate >= DateTime.Now.AddDays(-30))
                    .ToListAsync();
                double totalCategoryOrderDetailPrice = orderDetail.Sum(x => x.Price);
                double percent = totalCategoryOrderDetailPrice / totalOrderIncome * 100;
                orderCircleGraphs.Add(new OrderCircleGraph()
                {
                    CategoryName = category.Name,
                    Percent = percent
                });
            }
            return orderCircleGraphs;
        }
    }
}
