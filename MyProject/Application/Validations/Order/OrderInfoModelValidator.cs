using Application.ViewModels.OrderViewModels;
using FluentValidation;

namespace Application.Validations.Order
{
    public class OrderInfoModelValidator : AbstractValidator<OrderInfoModel>
    {
        public OrderInfoModelValidator()
        {
            RuleFor(x => x.Fullname).NotEmpty().WithMessage("Họ tên không được để trống.")
                .MaximumLength(50)
                .WithMessage("Họ tên không quá 50 ký tự.");
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email không được để trống.")
                .MaximumLength(50)
                .WithMessage("Email không quá 50 ký tự.").EmailAddress().WithMessage("Email không đúng.");
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Số điện thoại không được để trống.")
              .MaximumLength(10)
               .WithMessage("Số điện thoại phải có 10 ký tự.").MinimumLength(10)
               .WithMessage("Số điện thoại  phải có 10 ký tự.").MustAsync(IsPhoneNumberValid).WithMessage("Số điện thoại chỉ được chứa các chữ số.")
               .MustAsync(IsPhoneNumberStartWith).WithMessage("Số điện thoại chỉ được bắt đầu bằng các đầu số 03, 05, 07, 08, 09.");

        }
        public async Task<bool> IsPhoneNumberStartWith(string phoneNumber, CancellationToken cancellationToken)
        {
            if (phoneNumber.StartsWith("08") || phoneNumber.StartsWith("09") || phoneNumber.StartsWith("03") || phoneNumber.StartsWith("07") || phoneNumber.StartsWith("05"))
            {
                return true;
            }
            else
            {
                return false;

            }
        }
        public async Task<bool> IsPhoneNumberValid(string phoneNumber, CancellationToken cancellationToken)
        {
            foreach (char c in phoneNumber)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
