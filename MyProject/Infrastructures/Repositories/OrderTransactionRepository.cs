using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Infrastructures.Repositories.Base;

namespace Infrastructures.Repositories
{
    public class OrderTransactionRepository : GenericRepository<OrderTransaction>, IOrderTransactionRepository
    {
        public OrderTransactionRepository(AppDbContext context, ICurrentTime timeService, IClaimsService claimsService) : base(context, timeService, claimsService)
        {
        }
    }
}
