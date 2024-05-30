using Application.ViewModels.OrderViewModels;
using FluentValidation;

namespace Application.Validations.Order
{
    public class OrderModelValidator : AbstractValidator<OrderModel>
    {
        public OrderModelValidator()
        {
            RuleFor(x => x.Address).NotEmpty().WithMessage("Địa chỉ chi tiết không được để trống!").MaximumLength(100)
                .WithMessage("Địa chỉ chi tiết không quá 100 ký tự.");
            RuleFor(x => x.Note).MaximumLength(1000)
                .WithMessage("Ghi chú không quá 1000 ký tự.");
        }
    }
}
