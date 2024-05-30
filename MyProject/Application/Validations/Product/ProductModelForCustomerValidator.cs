using Application.ViewModels.ProductViewModel;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validations.Product
{
    public class BonsaiModelForCustomerValidator : AbstractValidator<ProductModel>
    {
        public BonsaiModelForCustomerValidator()
        {
            RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Danh mục không được để trống.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên không được để trống.")
                .MaximumLength(50).WithMessage("Tên không quá 50 ký tự.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Mô tả không được để trống.");
        }
    }
}
