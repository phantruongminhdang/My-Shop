using Application.Commons;
using Application.Interfaces.Services;
using Application.Interfaces.Services.VNPay.Models;
using Application.Interfaces.Services.VNPay.Utils;
using Application.Utils;
using Application.Validations.Order;
using Application.ViewModels.OrderViewModels;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Base;
using Domain.Enums;
using Firebase.Auth;
using MailKit.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Diagnostics.Eventing.Reader;
using System.Linq.Expressions;

namespace Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unit;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IFirebaseService _fireBaseService;
        private readonly IdUtil _idUtil;
        private readonly IVnPayService _vnPayService;
        public OrderService(IConfiguration configuration, IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IMapper mapper, FirebaseService fireBaseService, IdUtil idUtil, VnPayService vnPayService)
        {
            _configuration = configuration;
            _unit = unitOfWork;
            _userManager = userManager;
            _mapper = mapper;
            _fireBaseService = fireBaseService;
            _idUtil = idUtil;
            _vnPayService = vnPayService;
        }
        public async Task<IList<string>> ValidateOrderModel(OrderModel model, string userId)
        {
            if (model == null)
            {
                throw new Exception("Vui lòng điền đầu đủ thông tin.");
            }
            else if (userId == null && model.OrderInfo == null)
            {
                throw new Exception("Vui lòng thêm các thông tin người mua hàng.");
            }
            else if (model.OrderInfo != null)
            {
                var orderInfoValidate = new OrderInfoModelValidator();
                var resultOrderInfo = await orderInfoValidate.ValidateAsync(model.OrderInfo);
                if (!resultOrderInfo.IsValid)
                {
                    var errors = new List<string>();
                    errors.AddRange(resultOrderInfo.Errors.Select(x => x.ErrorMessage));
                    return errors;
                }
            }
            var validator = new OrderModelValidator();
            var result = await validator.ValidateAsync(model);
            if (!result.IsValid)
            {
                var errors = new List<string>();
                errors.AddRange(result.Errors.Select(x => x.ErrorMessage));
                return errors;
            }

            if (model.ListProduct == null || model.ListProduct.Count == 0)
            {
                throw new Exception("Vui lòng chọn product bạn muốn mua.");
            }
            return null;
        }


        public async Task<string> CreateOrderAsync(OrderModel model, string userId, HttpContext httpContext)
        {
            var paymentInformation = await CreateOrderByTransaction(model, userId);
            var vnPayUrl = _vnPayService.CreatePaymentUrl(paymentInformation, httpContext);
            return vnPayUrl;
        }

        public async Task<ErrorViewModel> PaymentExecuteIpn(IQueryCollection collections)
        {
            Console.WriteLine("hello");
            try
            {
                var pay = new VnPayLibrary();
                foreach (var (key, value) in collections)
                {
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    {
                        pay.AddResponseData(key, value);
                    }
                }

                var orderId = Guid.Parse(pay.GetResponseData("vnp_TxnRef"));
                var vnPayTranId = Convert.ToInt64(pay.GetResponseData("vnp_TransactionNo"));
                var vnpResponseCode = pay.GetResponseData("vnp_ResponseCode");
                var vnpSecureHash =
                    collections.FirstOrDefault(k => k.Key == "vnp_SecureHash").Value; //hash của dữ liệu trả về
                var orderInfo = pay.GetResponseData("vnp_OrderInfo");
                long vnp_Amount = Convert.ToInt64(pay.GetResponseData("vnp_Amount")) / 100;
                string vnp_TransactionStatus = pay.GetResponseData("vnp_TransactionStatus");
                var checkSignature =
                    pay.ValidateSignature(vnpSecureHash, _configuration["Vnpay:HashSecret"]); //check Signature


                if (checkSignature)
                {
                    //Cap nhat ket qua GD
                    //Yeu cau: Truy van vao CSDL cua  Merchant => lay ra duoc OrderInfo
                    //Giả sử OrderInfo lấy ra được như giả lập bên dưới
                    TransactionStatus transactionStatus = TransactionStatus.Failed;
                    OrderStatus orderStatus = OrderStatus.Failed;

                    var order = await _unit.OrderRepository.GetAllQueryable().FirstOrDefaultAsync(x => x.Id == orderId);

                    PaymentResponseModel result = new PaymentResponseModel()
                    {
                        Success = true,
                        PaymentMethod = "VnPay",
                        OrderDescription = orderInfo,
                        OrderId = orderId.ToString(),
                        PaymentId = vnPayTranId.ToString(),
                        TransactionId = vnPayTranId.ToString(),
                        Token = vnpSecureHash,
                        VnPayResponseCode = vnpResponseCode,
                        Amount = vnp_Amount,
                    };
                    if (order != null)
                    {
                        if (result.Amount == vnp_Amount)
                        {
                            if (order.OrderStatus == OrderStatus.Waiting)
                            {
                                if (vnpResponseCode == "00" && vnp_TransactionStatus == "00")
                                {
                                    //Thanh toan thanh cong
                                    transactionStatus = TransactionStatus.Success;
                                    orderStatus = OrderStatus.Paid;
                                }
                                else
                                {
                                    //Thanh toan khong thanh cong. Ma loi: vnp_ResponseCode
                                    //  displayMsg.InnerText = "Có lỗi xảy ra trong quá trình xử lý. 
                                    transactionStatus = TransactionStatus.Failed;
                                    orderStatus = OrderStatus.Failed;
                                    await UpdateProductFromOrder(orderId);
                                }
                                //Thêm code Thực hiện cập nhật vào Database 
                                //Update Database
                                var orderTransaction = new OrderTransaction();
                                orderTransaction.OrderId = orderId;
                                orderTransaction.Amount = vnp_Amount;
                                orderTransaction.IpnURL = _configuration["PaymentCallBack:ReturnUrl"];
                                orderTransaction.Information = orderInfo;
                                orderTransaction.RedirectUrl = _configuration["PaymentCallBack:ReturnUrl"];
                                orderTransaction.TransactionStatus = transactionStatus;
                                orderTransaction.PaymentMethod = "VNPAY Payment";
                                orderTransaction.TransId = vnPayTranId;
                                orderTransaction.ResultCode = int.Parse(vnpResponseCode);
                                orderTransaction.Message = "Thanh cong";
                                // Tạo transaction
                                orderTransaction.Signature = vnpSecureHash;
                                await _unit.OrderTransactionRepository.AddAsync(orderTransaction);
                                order.OrderStatus = orderStatus;
                                _unit.OrderRepository.Update(order);
                                await _unit.SaveChangeAsync();
                                if (vnpResponseCode != "00" || vnp_TransactionStatus != "00")
                                {
                                    await UpdateProductFromOrder(orderId);
                                }
                                Console.WriteLine("Thanh cong");
                                return new ErrorViewModel()
                                {
                                    RspCode = "00",
                                    Message = "Confirm Success!"
                                };
                            }
                            else if (order.OrderStatus == OrderStatus.Paid)
                            {
                                Console.WriteLine(" Don hang da Da xac nhan!");
                                return new ErrorViewModel()
                                {
                                    RspCode = "02",
                                    Message = "Order already confirmed"
                                };
                            }
                        }
                        else
                        {
                            Console.WriteLine("Khong dung gia tien");
                            return new ErrorViewModel()
                            {
                                RspCode = "04",
                                Message = "invalid amount!"
                            };
                        }
                    }
                    else
                    {
                        return new ErrorViewModel()
                        {
                            RspCode = "01",
                            Message = "Order not found"
                        };
                    }

                }

                return new ErrorViewModel()
                {
                    RspCode = "99",
                    Message = "Input data required"
                };
            }
            catch
            {
                throw new Exception("Đã xảy ra lối trong qua trình thanh toán. Vui lòng thanh toán lại sau!");
            }
        }

        public async Task UpdateProductFromOrder(Guid orderId)
        {
            var order = await _unit.OrderRepository.GetAllQueryable()
                .Include(x => x.OrderDetails)
                .Where(x => !x.IsDeleted && x.Id == orderId && x.OrderStatus == OrderStatus.Failed)
                .FirstOrDefaultAsync();
            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng.");
            foreach (var item in order.OrderDetails)
            {
                var product = await _unit.ProductRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                    throw new Exception("Không tìm thấy product bạn muốn mua");
                product.isDisable = false;
                _unit.ProductRepository.Update(product);
                await _unit.SaveChangeAsync();
            }
        }

        public async Task<Pagination<OrderViewModel>> GetPaginationAsync(string userId, int pageIndex = 0, int pageSize = 10)
        {
            IList<Order> listOrder = new List<Order>();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("Không tìm thấy người dùng!");
            var isCustomer = await _userManager.IsInRoleAsync(user, "Customer");
            var isAdmin = await _userManager.IsInRoleAsync(user, "Manager");
            List<Expression<Func<Order, object>>> includes = new List<Expression<Func<Order, object>>>
            {
                x => x.Customer.ApplicationUser
            };
            if (isCustomer && !isAdmin)
                listOrder = await _unit.OrderRepository.GetAllQueryable()
                    .Include(x => x.Customer)
                    .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.Product.ProductImages)
                    .Where(x => x.Customer.UserId.ToLower() == userId).OrderByDescending(y => y.CreationDate).ToListAsync();
            else if (isAdmin)
                listOrder = await _unit.OrderRepository.GetAllQueryable()
                   .Include(x => x.Customer.ApplicationUser)
                   .Include(x => x.OrderDetails)
                   .ThenInclude(x => x.Product.ProductImages)
                   .OrderByDescending(y => y.CreationDate).ToListAsync();
            else return null;
            var itemCount = listOrder.Count();
            var items = listOrder.OrderByDescending(x => x.CreationDate)
                                    .Skip(pageIndex * pageSize)
                                    .Take(pageSize)
                                    .ToList();

            var res = new Pagination<Order>()
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalItemsCount = itemCount,
                Items = items,
            };

            var result = _mapper.Map<Pagination<OrderViewModel>>(res);
            return result;
        }
        public async Task<Order> GetByIdAsync(string userId, Guid orderId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("Không tìm thấy người dùng!");
            var isCustomer = await _userManager.IsInRoleAsync(user, "Customer");
            var isAdmin = await _userManager.IsInRoleAsync(user, "Manager");
            var order = await _unit.OrderRepository.GetAllQueryable()
                .Include(x => x.OrderTransaction).Include(x => x.Customer.ApplicationUser)
                .Include(x => x.OrderDetails.Where(i => !i.IsDeleted)).ThenInclude(x => x.Product.ProductImages)
                .FirstOrDefaultAsync(x => x.Id == orderId);
            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng bạn yêu cầu");

            if (isCustomer && !order.Customer.UserId.ToLower().Equals(userId.ToLower()))
                throw new Exception("Bạn không có quyền truy cập vào đơn hàng này!");
            return order;
        }
        public async Task<PaymentInformationModel> CreateOrderByTransaction(OrderModel model, string? userId)
        {
            var customer = await GetCustomerAsync(model, userId);
            _unit.BeginTransaction();
            try
            {
                Order order = await CreateOrder(model, customer.Id);
                foreach (var item in model.ListProduct)
                {
                    await CreateOrderDetail(item, order.Id);
                }
                order = await UpdateOrder(order.Id, model.ListProduct.Distinct().ToList());
                await _unit.CommitTransactionAsync();
                return new PaymentInformationModel()
                {
                    Id = order.Id,
                    Amount = order.TotalPrice,
                    Name = customer.ApplicationUser.Email,
                    OrderDescription = order.Note,
                    OrderType = "1"
                };
            }
            catch (Exception ex)
            {
                _unit.RollbackTransaction();
                throw new Exception(ex.Message);
            }
        }

        public async Task<IdentityResult> CreateUserAsync(OrderInfoModel model)
        {
            var user = new ApplicationUser
            {
                Email = model.Email,
                Fullname = model.Fullname,
                PhoneNumber = model.PhoneNumber,
                UserName = model.Email,
                IsRegister = false,
                TwoFactorEnabled = true
            };

            var result = await _userManager.CreateAsync(user);

            return result;
        }

        public async Task<Customer> GetCustomerAsync(OrderModel model, string? userId)
        {
            ApplicationUser? user = null;
            if (userId == null || userId.Equals("00000000-0000-0000-0000-000000000000"))
            {
                if (model.OrderInfo == null)
                    throw new Exception("Vui lòng thêm các thông tin người mua hàng.");
                user = await _userManager.FindByEmailAsync(model.OrderInfo.Email);
                if (user == null)
                {
                    var result = await CreateUserAsync(model.OrderInfo);
                    if (!result.Succeeded)
                    {
                        throw new Exception("Đã xảy ra lỗi trong quá trình đặt hàng!");
                    }
                    else
                    {
                        user = await _userManager.FindByEmailAsync(model.OrderInfo.Email);
                        //tạo account customer.
                        try
                        {
                            await _userManager.AddToRoleAsync(user, "Customer");
                            Customer cus = new Customer { UserId = user.Id };
                            await _unit.CustomerRepository.AddAsync(cus);
                            await _unit.SaveChangeAsync();
                        }
                        catch (Exception)
                        {
                            await _userManager.DeleteAsync(user);
                            throw new Exception("Đã xảy ra lỗi trong quá trình đăng ký. Vui lòng thử lại!");
                        }
                    }
                }
            }
            else
            {
                user = await _userManager.FindByIdAsync(userId);
            }
            if (user == null)
                throw new Exception("Đã xảy ra lỗi trong quá trình đặt hàng!");
            var isCustomer = await _userManager.IsInRoleAsync(user, "Customer");
            if (!isCustomer)
                throw new Exception("Bạn không có quyền để thực hiện hành động này!");
            var customer = await _unit.CustomerRepository.GetAllQueryable().Include(x => x.ApplicationUser).FirstOrDefaultAsync(x => x.UserId.ToLower().Equals(user.Id.ToLower()));
            if (customer == null)
                throw new Exception("Không tìm thấy thông tin người dùng");
            return customer;
        }

        public async Task<Order> CreateOrder(OrderModel model, Guid customerId)
        {
            try
            {
                var order = _mapper.Map<Order>(model);
                order.OrderDate = DateTime.Now;
                order.CustomerId = customerId;
                order.TotalPrice = 0;
                order.OrderStatus = OrderStatus.Waiting;
                await _unit.OrderRepository.AddAsync(order);
                await _unit.SaveChangeAsync();
                return order;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task CreateOrderDetail(Guid productId, Guid orderId)
        {
            try
            {
                var product = await _unit.ProductRepository.GetByIdAsync(productId);
                if (product == null)
                    throw new Exception("Không tìm thấy product bạn muốn mua");
                else if (product.isDisable == true)
                    throw new Exception($"{product.Name} không khả dụng");

                //tạo order đetail
                var orderDetail = new OrderDetail();
                orderDetail.ProductId = productId;
                orderDetail.OrderId = orderId;
                orderDetail.Price = product.Price;
                await _unit.OrderDetailRepository.AddAsync(orderDetail);

                //trừ quantity của product
                product.isDisable = true;
                _unit.ProductRepository.Update(product);
                await _unit.SaveChangeAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public async Task<Order> UpdateOrder(Guid orderId, List<Guid> productsId)
        {
            try
            {
                var order = await _unit.OrderRepository.GetAllQueryable().Where(x => x.Id == orderId && !x.IsDeleted).FirstOrDefaultAsync();
                if (order == null)
                    throw new Exception("Đã xảy ra lỗi trong quá trình đặt hàng!");

                var listOrderDetail = await _unit.OrderDetailRepository.GetAllQueryable().Where(x => x.OrderId == orderId && !x.IsDeleted).ToListAsync();

                if (listOrderDetail == null || listOrderDetail.Count == 0)
                    throw new Exception("Đã xảy ra lỗi trong quá trình đặt hàng!");
                double total = 0;
                foreach (var item in listOrderDetail)
                {
                    total += item.Price;
                }
                order.ExpectedDeliveryDate = DateTime.Now;
                order.TotalPrice = total;
                _unit.ClearTrack();
                _unit.OrderRepository.Update(order);
                await _unit.SaveChangeAsync();
                return order;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public async Task UpdateOrderStatusAsync(Guid orderId, OrderStatus orderStatus)
        {
            var order = await _unit.OrderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new Exception("Không tìm thấy");
            }
            if (orderStatus < order.OrderStatus)
            {
                throw new Exception("Trạng thái không hợp lệ.");
            }
            if (order.OrderStatus == OrderStatus.Delivered || order.OrderStatus == OrderStatus.DeliveryFailed || order.OrderStatus == OrderStatus.Failed) throw new Exception("Đơn hàng này đã kết thúc nên không thể cập nhật trạng thái.");
            order.OrderStatus = orderStatus;
            _unit.OrderRepository.Update(order);
            await _unit.SaveChangeAsync();
        }
        public async Task FinishDeliveryOrder(Guid orderId, FinishDeliveryOrderModel finishDeliveryOrderModel)
        {
            var order = await _unit.OrderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new Exception("Không tìm thấy");
            }
            try
            {
                _unit.BeginTransaction();
                foreach (var singleImage in finishDeliveryOrderModel.Image.Select((image, index) => (image, index)))
                {
                    string newImageName = order.Id + "_i" + singleImage.index;
                    string folderName = $"order/{order.Id}/Image";
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
                    DeliveryImage deliveryImage = new DeliveryImage()
                    {
                        OrderId = order.Id,
                        Image = url
                    };
                    await _unit.DeliveryImageRepository.AddAsync(deliveryImage);
                }
                order.OrderStatus = OrderStatus.Delivered;
                order.DeliveryDate = DateTime.Now;

                _unit.OrderRepository.Update(order);
                await _unit.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                _unit.RollbackTransaction();
                throw new Exception(ex.Message);
            }
        }
        public async Task ShipperAddition(Guid orderId, Guid shipperId)
        {
            var order = await _unit.OrderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new Exception("Không tìm thấy");
            }
            try
            {
                order.ShipperId = shipperId;
                _unit.OrderRepository.Update(order);
                await _unit.SaveChangeAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}


