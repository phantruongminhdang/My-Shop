using Application.ViewModels.CategoryViewModels;
using FluentValidation;

namespace Application.Validations.Category
{
    public class CategoryModelValidator : AbstractValidator<CategoryModel>
    {
        public CategoryModelValidator()
        {
            RuleFor(tag => tag.Name)
           .NotEmpty().WithMessage("Tên danh mục không được để trống.").MaximumLength(100).WithMessage("Tên danh mục không được quá 100 ký tự.");
        }
    }
}
