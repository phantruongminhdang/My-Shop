using Application.Commons;
using Application.ViewModels.CategoryViewModels;
using Application.ViewModels.OrderViewModels;
using Application.ViewModels.ProductViewModel;
using Application.ViewModels.UserViewModels;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Base;

namespace Infrastructures.Mappers
{
    public class MapperConfigurationsProfile : Profile
    {
        public MapperConfigurationsProfile()
        {
            CreateMap(typeof(Pagination<>), typeof(Pagination<>));
            CreateMap<ProductModel, Product>();
            CreateMap<CategoryModel, Category>();
            CreateMap<OrderModel, Order>().ReverseMap();
            CreateMap<OrderViewModel, Order>();
            CreateMap<Order, OrderViewModel>().ForMember(dest => dest.OrderDetails, opt => opt.MapFrom(src => src.OrderDetails));
            CreateMap<UserViewModel, ApplicationUser>().ReverseMap();
        }
    }
}
