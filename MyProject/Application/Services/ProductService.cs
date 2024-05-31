using Application.Commons;
using Application.Interfaces.Services;
using Application.Utils;
using Application.Validations.Product;
using Application.ViewModels.ProductViewModel;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.Export.ToCollection;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly FirebaseService _fireBaseService;
        private readonly IMapper _mapper;
        private readonly IdUtil _idUtil;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper, FirebaseService fireBaseService, IdUtil idUtil)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _fireBaseService = fireBaseService;
            _idUtil = idUtil;
        }

        public async Task<Pagination<Product>> GetPagination(int pageIndex, int pageSize, bool isAdmin = false)
        {
            Pagination<Product> Products;
            List<Expression<Func<Product, object>>> includes = new List<Expression<Func<Product, object>>>{
                                 x => x.ProductImages.Where(y => !y.IsDeleted),
                                 x => x.Category,
                                    };
            if (isAdmin)
            {
                Products = await _unitOfWork.ProductRepository
                    .GetAsync(pageIndex: pageIndex, pageSize: pageSize, expression: x => !x.IsDeleted && !x.Code.Contains("KHACHHANG"), includes: includes);
            }
            else
            {
                Products = await _unitOfWork.ProductRepository
                    .GetAsync(pageIndex: pageIndex, pageSize: pageSize, expression: x => !x.IsDeleted && !x.Code.Contains("KHACHHANG") && !x.isDisable, includes: includes);
            }
            return Products;
        }
        public async Task<Pagination<Product>> GetAll(bool isAdmin = false)
        {
            Pagination<Product> products;
            List<Expression<Func<Product, object>>> includes = new List<Expression<Func<Product, object>>>{
                                 x => x.ProductImages.Where(y => !y.IsDeleted),
                                 x => x.Category,
                                    };
            if (isAdmin)
            {
                products = await _unitOfWork.ProductRepository.GetAsync(isTakeAll: true, expression: x => !x.IsDeleted && !x.Code.Contains("KHACHHANG"),
                isDisableTracking: true, includes: includes);
            }
            else
            {
                products = await _unitOfWork.ProductRepository.GetAsync(isTakeAll: true, expression: x => !x.IsDeleted && !x.Code.Contains("KHACHHANG") && !x.isDisable,
                isDisableTracking: true, includes: includes);
            }

            return products;
        }
        public async Task<Pagination<Product>?> GetByFilter(int pageIndex, int pageSize, FilterProductModel filterProductModel, bool isAdmin = false)
        {
            if (filterProductModel.Keyword != null && filterProductModel.Keyword.Length > 50)
            {
                throw new Exception("Từ khóa phải dưới 50 kí tự");
            }
            var filter = new List<Expression<Func<Product, bool>>>();
            filter.Add(x => !x.IsDeleted && !x.Code.Contains("KHACHHANG"));
            if (!isAdmin)
                filter.Add(x => !x.isDisable);

            if (filterProductModel.Keyword != null)
            {
                string keywordLower = filterProductModel.Keyword.ToLower();
                filter.Add(x => x.Name.ToLower().Contains(keywordLower) || x.NameUnsign.ToLower().Contains(keywordLower));
            }
            try
            {
                if (filterProductModel.CategoryId != null && filterProductModel.CategoryId != "")
                {
                    filter.Add(x => x.CategoryId == Guid.Parse(filterProductModel.CategoryId));
                }
            }
            catch (Exception)
            {
                throw new Exception("Xảy ra lỗi trong quá trình nhập bộ lọc!");
            }

            if (filterProductModel.MinPrice != null)
            {
                filter.Add(x => x.Price >= filterProductModel.MinPrice);
            }
            if (filterProductModel.MaxPrice != null)
            {
                filter.Add(x => x.Price <= filterProductModel.MaxPrice);
            }
            var finalFilter = filter.Aggregate((current, next) => current.AndAlso(next));
            List<Expression<Func<Product, object>>> includes = new List<Expression<Func<Product, object>>>{
                                 x => x.ProductImages.Where(y => !y.IsDeleted),
                                 x => x.Category,
                                    };
            var products = await _unitOfWork.ProductRepository.GetAsync(pageIndex: pageIndex, pageSize: pageSize, expression: finalFilter,
                isDisableTracking: true, includes: includes);
            return products;
        }

        public async Task<Product?> GetById(Guid id, bool isAdmin = false)
        {
            Pagination<Product> products;
            List<Expression<Func<Product, object>>> includes = new List<Expression<Func<Product, object>>>{
                                 x => x.ProductImages.Where(y => !y.IsDeleted),
                                 x => x.Category,
                                    };
            if (isAdmin)
            {
                products = await _unitOfWork.ProductRepository.GetAsync(isTakeAll: true, expression: x => !x.IsDeleted && x.Id == id && !x.Code.Contains("KHACHHANG"),
                isDisableTracking: true, includes: includes);
            }
            else
            {
                products = await _unitOfWork.ProductRepository.GetAsync(isTakeAll: true, expression: x => !x.IsDeleted && !x.isDisable && x.Id == id && !x.Code.Contains("KHACHHANG"),
                isDisableTracking: true, includes: includes);
            }
            return products.Items[0];
        }

        public async Task AddAsync(ProductModel productModel)
        {

            if (productModel == null)
                throw new ArgumentNullException(nameof(ProductModel), "Vui lòng điền đầy đủ thông tin!");

            var validationRules = new ProductModelValidator();
            var resultProductInfo = await validationRules.ValidateAsync(productModel);
            if (!resultProductInfo.IsValid)
            {
                var errors = resultProductInfo.Errors.Select(x => x.ErrorMessage);
                string errorMessage = string.Join(Environment.NewLine, errors);
                throw new Exception(errorMessage);
            }
            if (productModel.Price == null)
            {
                throw new Exception("Giá không được để trống.");
            }
            if (productModel.Image == null || productModel.Image.Count == 0)
                throw new Exception("Vui lòng thêm hình ảnh");
            var category = await _unitOfWork.CategoryRepository.GetByIdAsync(productModel.CategoryId);
            if (category == null)
                throw new Exception("Không tìm thấy danh mục!");
            var product = _mapper.Map<Product>(productModel);
            product.isDisable = false;
            product.Code = await generateCode(productModel.CategoryId);
            try
            {
                _unitOfWork.BeginTransaction();
                await _unitOfWork.ProductRepository.AddAsync(product);
                if (productModel.Image != null)
                {
                    foreach (var singleImage in productModel.Image.Select((image, index) => (image, index)))
                    {
                        string newImageName = product.Id + "_i" + singleImage.index;
                        string folderName = $"Product/{product.Id}/Image";
                        string imageExtension = Path.GetExtension(singleImage.image.FileName);
                        //Kiểm tra xem có phải là file ảnh không.
                        string[] validImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                        const long maxFileSize = 20 * 1024 * 1024;
                        if (Array.IndexOf(validImageExtensions, imageExtension.ToLower()) == -1 || singleImage.image.Length > maxFileSize)
                        {
                            throw new Exception("Có chứa file không phải ảnh hoặc quá dung lượng tối đa(>20MB)!");
                        }
                        var url = await _fireBaseService.UploadFileToFirebaseStorage(singleImage.image, newImageName, folderName);
                        if (url == null)
                            throw new Exception("Lỗi khi đăng ảnh lên Firebase!");

                        ProductImage productImage = new ProductImage()
                        {
                            ProductId = product.Id,
                            ImageUrl = url
                        };

                        await _unitOfWork.ProductImageRepository.AddAsync(productImage);
                    }
                }
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                _unitOfWork.RollbackTransaction();
                throw;
            }
        }
        public async Task Update(Guid id, ProductModel productModel)
        {
            var result = await _unitOfWork.ProductRepository.GetByIdAsync(id);
            if (result == null)
                throw new Exception("Không tìm thấy Product!");
            if (productModel == null)
                throw new ArgumentNullException(nameof(productModel), "Vui lòng điền đầy đủ thông tin!");
            var validationRules = new ProductModelValidator();
            var resultProductInfo = await validationRules.ValidateAsync(productModel);
            if (!resultProductInfo.IsValid)
            {
                var errors = resultProductInfo.Errors.Select(x => x.ErrorMessage);
                string errorMessage = string.Join(Environment.NewLine, errors);
                throw new Exception(errorMessage);
            }
            if ((productModel.Image == null || productModel.Image.Count == 0) && (productModel.OldImage == null || productModel.OldImage.Count == 0))
                throw new Exception("Vui lòng thêm hình ảnh");
            var product = _mapper.Map<Product>(productModel);
            product.Id = id;

            product.Code = result.Code;
            product.isDisable = false;
            try
            {
                _unitOfWork.BeginTransaction();
                _unitOfWork.ProductRepository.Update(product);
                if (productModel.Image != null)
                {
                    var images = await _unitOfWork.ProductImageRepository.GetAsync(isTakeAll: true, expression: x => x.ProductId == id && !x.IsDeleted, isDisableTracking: true);
                    if (productModel.OldImage != null)
                    {
                        foreach (ProductImage image in images.Items.ToList())
                        {
                            if (productModel.OldImage.Contains(image.ImageUrl))
                            {
                                //Bỏ những cái có trong danh sách cũ truyền về -> không xóa
                                images.Items.Remove(image);
                            }
                        }

                    }
                    _unitOfWork.ProductImageRepository.SoftRemoveRange(images.Items);
                    foreach (var singleImage in productModel.Image.Select((image, index) => (image, index)))
                    {
                        string newImageName = product.Id + "_i" + singleImage.index;
                        string folderName = $"Product/{product.Id}/Image";
                        string imageExtension = Path.GetExtension(singleImage.image.FileName);
                        string[] validImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                        const long maxFileSize = 20 * 1024 * 1024;
                        if (Array.IndexOf(validImageExtensions, imageExtension.ToLower()) == -1 || singleImage.image.Length > maxFileSize)
                        {
                            throw new Exception("Có chứa file không phải ảnh hoặc quá dung lượng tối đa(>20MB)!");
                        }
                        var url = await _fireBaseService.UploadFileToFirebaseStorage(singleImage.image, newImageName, folderName);
                        if (url == null)
                            throw new Exception("Lỗi khi đăng ảnh lên firebase!");

                        ProductImage ProductImage = new ProductImage()
                        {
                            ProductId = product.Id,
                            ImageUrl = url
                        };

                        await _unitOfWork.ProductImageRepository.AddAsync(ProductImage);
                    }
                }
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task Delete(Guid id)
        {
            var result = await _unitOfWork.ProductRepository.GetByIdAsync(id);
            if (result == null)
                throw new Exception("Không tìm thấy!");
            var orderDetails = await _unitOfWork.OrderDetailRepository.GetAsync(pageIndex: 0, pageSize: 1, expression: x => x.ProductId == id && !x.IsDeleted);
            if (orderDetails.TotalItemsCount > 0)
            {
                throw new Exception("Tồn tại đơn hàng thuộc về cây này, không thể xóa!");
            }
            try
            {
                _unitOfWork.ProductRepository.SoftRemove(result);
                await _unitOfWork.SaveChangeAsync();
            }
            catch (Exception)
            {
                throw new Exception("Đã xảy ra lỗi trong quá trình xóa Product. Vui lòng thử lại!");
            }
        }
        public async Task<Pagination<Product>> GetBoughtProduct(Guid id)
        {
            var customer = await _idUtil.GetCustomerAsync(id);
            var orderDetails = await _unitOfWork.OrderDetailRepository.GetAsync(isTakeAll: true, expression: x => x.Order.CustomerId == customer.Id && x.Order.OrderStatus == Domain.Enums.OrderStatus.Delivered);
            if (orderDetails.Items.Count == 0)
            {
                throw new Exception("Bạn chưa có đơn hàng hoàn thành nào!");
            }
            List<Guid> orderDetailsId = new List<Guid>();
            foreach (OrderDetail orderDetail in orderDetails.Items)
            {
                orderDetailsId.Add(orderDetail.Id);
            }
            List<Expression<Func<Product, object>>> includes = new List<Expression<Func<Product, object>>>{
                                 x => x.ProductImages.Where(y => !y.IsDeleted)
                                    };
            var products = await _unitOfWork.ProductRepository.GetAsync(isTakeAll: true, expression: x => x.OrderDetails.Any(y => orderDetailsId.Contains(y.Id)) && !x.Code.Contains("KHACHHANG"), includes: includes);
            return products;
        }
        private async Task<string> generateCode(Guid categoryId)
        {
            List<Expression<Func<Product, object>>> includes = new List<Expression<Func<Product, object>>>{
                                 x => x.Category,
                                    };
            var lastCodeProduct = await _unitOfWork.ProductRepository.GetAsync(pageIndex: 0, pageSize: 1, expression: x => x.CategoryId == categoryId && !x.Code.Contains("KHACHHANG"), orderBy: query => query.OrderByDescending(x => x.Code), includes: includes);
            if (lastCodeProduct.Items.Count > 0)
            {
                var lastCodeNumericPart = Regex.Match(lastCodeProduct.Items[0].Code, @"\d+").Value;
                if (lastCodeNumericPart == "")
                {
                    var category = await _unitOfWork.CategoryRepository.GetByIdAsync(categoryId);
                    return $"{StringUtils.RemoveDiacritics(category.Name)}00001";
                }
                var newCodeNumericPart = (int.Parse(lastCodeNumericPart) + 1).ToString().PadLeft(lastCodeNumericPart.Length, '0');
                return $"{StringUtils.RemoveDiacritics(lastCodeProduct.Items[0].Category.Name)}{newCodeNumericPart}";
            }
            else
            {
                var category = await _unitOfWork.CategoryRepository.GetByIdAsync(categoryId);
                return $"{StringUtils.RemoveDiacritics(category.Name)}00001";
            }
        }
        public async Task<Pagination<Product>> GetByCategory(int pageIndex, int pageSize, Guid categoryId)
        {
            List<Expression<Func<Product, object>>> includes = new List<Expression<Func<Product, object>>>{
                                 x => x.ProductImages.Where(y => !y.IsDeleted),
                                 x => x.Category,
                                    };
            var products = await _unitOfWork.ProductRepository.GetAsync(pageIndex: pageIndex, pageSize: pageSize, expression: x => !x.IsDeleted && !x.isDisable && x.CategoryId == categoryId && !x.Code.Contains("KHACHHANG"),
                isDisableTracking: true, includes: includes);
            return products;
        }
        public async Task DisableProduct(Guid id)
        {
            var product = await _unitOfWork.ProductRepository.GetAsync(isTakeAll: true, expression: x => !x.IsDeleted && x.Id == id && !x.Code.Contains("KHACHHANG"),
                isDisableTracking: true);
            if (!product.Items[0].isDisable)
                product.Items[0].isDisable = true;
            else
                product.Items[0].isDisable = false;
            _unitOfWork.ProductRepository.Update(product.Items[0]);
            await _unitOfWork.SaveChangeAsync();
        }
        public async Task<List<Product>> getCurrentCart(List<Guid> ProductId)
        {
            List<Product> products = new List<Product>();
            foreach (Guid id in ProductId)
            {
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
                if (product == null)
                {
                    throw new Exception("Không tìm thấy Product " + id.ToString());
                }
                products.Add(product);
            }
            if (ProductId.Count != products.Count)
            {
                throw new Exception("Số lượng cây trong cart khác với số lượng cây");
            }
            return products;
        }
    }
}
