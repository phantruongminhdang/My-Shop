using Application.ViewModels.UserViewModels;
using FluentValidation;

namespace Application.Validations.User
{
    public class UserModelValidator : AbstractValidator<UserRequestModel>
    {
        public UserModelValidator()
        {
            RuleFor(x => x.Username).NotEmpty().WithMessage("Tên đăng nhập không được để trống.")
                .MaximumLength(50)
                .WithMessage("Tên đăng nhập không quá 50 ký tự.").MustAsync(IsLetterOrDigitOnly);
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Số điện thoại không được để trống.")
               .MaximumLength(10)
                .WithMessage("Số điện thoại phải có 10 ký tự.").MinimumLength(10)
                .WithMessage("Số điện thoại  phải có 10 ký tự.").MustAsync(IsPhoneNumberValid).WithMessage("Số điện thoại chỉ được chứa các chữ số.")
                .MustAsync(IsPhoneNumberStartWith).WithMessage("Số điện thoại chỉ được bắt đầu bằng các đầu số 03, 05, 07, 08, 09.");
            RuleFor(x => x.Fullname).NotEmpty().WithMessage("Họ tên không được để trống.")
                .MaximumLength(50)
                .WithMessage("Họ tên ngắn không quá 50 ký tự.");
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
        public async Task<bool> ContainsDigit(string input, CancellationToken cancellationToken)
        {
            return input.Any(char.IsDigit);
        }

        public async Task<bool> ContainsLowercase(string input, CancellationToken cancellationToken)
        {
            return input.Any(char.IsLower);
        }
        public async Task<bool> ContainsUppercase(string input, CancellationToken cancellationToken)
        {
            return input.Any(char.IsUpper);
        }
        public async Task<bool> ContainsSpecialCharacter(string input, CancellationToken cancellationToken)
        {
            return input.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
        }
        public async Task<bool> IsLetterOrDigitOnly(string input, CancellationToken cancellationToken)
        {
            return input.All(char.IsLetterOrDigit);
        }
    }
}
