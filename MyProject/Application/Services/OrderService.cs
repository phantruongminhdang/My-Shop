using Application.Commons;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Momo;
using Application.Utils;
using Application.Validations.Order;
using Application.ViewModels.OrderViewModels;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Base;
using Domain.Enums;
using Firebase.Auth;
using MailKit.Search;
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
        public OrderService(IConfiguration configuration, IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IMapper mapper, FirebaseService fireBaseService, IdUtil idUtil)
        {
            _configuration = configuration;
            _unit = unitOfWork;
            _userManager = userManager;
            _mapper = mapper;
            _fireBaseService = fireBaseService;
            _idUtil = idUtil;
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


        public async Task<string> CreateOrderAsync(OrderModel model, string userId)
        {
            var orderId = await CreateOrderByTransaction(model, userId);
            var momoUrl = await GetPaymentUrl(orderId);
            return momoUrl;
        }
        private async Task<string> GetPaymentUrl(Guid tempId)
        {
            var order = await _unit.OrderRepository.GetByIdAsync(tempId);
            if (order == null)
                throw new Exception("Đã xảy ra lỗi trong quá trình thanh toán. Vui lòng thanh toán lại sau");

            double totalPrice = Math.Round(order.TotalPrice);
            string endpoint = _configuration["MomoServices:endpoint"];
            string partnerCode = _configuration["MomoServices:partnerCode"];
            string accessKey = _configuration["MomoServices:accessKey"];
            string serectkey = _configuration["MomoServices:secretKey"];
            string orderInfo = "Thanh toán hóa đơn hàng tại Thanh Sơn Garden.";
            string redirectUrl = _configuration["MomoServices:redirectUrl"];
            string ipnUrl = _configuration["MomoServices:ipnUrl"];
            string requestType = "captureWallet";
            string amount = totalPrice.ToString();
            string orderId = Guid.NewGuid().ToString();
            string requestId = Guid.NewGuid().ToString();
            string extraData = order.Id.ToString();
            //captureWallet
            //Before sign HMAC SHA256 signature
            string rawHash = "accessKey=" + accessKey +
                "&amount=" + amount +
                "&extraData=" + extraData +
                "&ipnUrl=" + ipnUrl +
                "&orderId=" + orderId +
                "&orderInfo=" + orderInfo +
                "&partnerCode=" + partnerCode +
                "&redirectUrl=" + redirectUrl +
                "&requestId=" + requestId +
                "&requestType=" + requestType
            ;
            MoMoSecurity crypto = new MoMoSecurity();
            //sign signature SHA256
            string signature = crypto.signSHA256(rawHash, serectkey);
            //build body json request
            JObject message = new JObject
                 {
                { "partnerCode", partnerCode },
                { "partnerName", "Test" },
                { "storeId", "MomoTestStore1" },
                { "requestId", requestId },
                { "amount", amount },
                { "orderId", orderId },
                { "orderInfo", orderInfo },
                { "redirectUrl", redirectUrl },
                { "ipnUrl", ipnUrl },
                { "lang", "en" },
                { "extraData", extraData },
                { "requestType", requestType },
                { "signature", signature }

                };
            try
            {
                string responseFromMomo = PaymentRequest.sendPaymentRequest(endpoint, message.ToString());

                JObject jmessage = JObject.Parse(responseFromMomo);

                return jmessage.GetValue("payUrl").ToString();
            }
            catch
            {
                throw new Exception("Đã xảy ra lối trong qua trình thanh toán. Vui lòng thanh toán lại sau!");
            }
        }

        public async Task HandleIpnAsync(MomoRedirect momo)
        {
            string accessKey = _configuration["MomoServices:accessKey"];
            string IpnUrl = _configuration["MomoServices:ipnUrl"];
            string redirectUrl = _configuration["MomoServices:redirectUrl"];
            string partnerCode = _configuration["MomoServices:partnerCode"];
            string endpoint = _configuration["MomoServices:endpoint"];

            string rawHash = "accessKey=" + accessKey +
                    "&amount=" + momo.amount +
                    "&extraData=" + momo.extraData +
                    "&message=" + momo.message +
                    "&orderId=" + momo.orderId +
                    "&orderInfo=" + momo.orderInfo +
                    "&orderType=" + momo.orderType +
                    "&partnerCode=" + partnerCode +
                    "&payType=" + momo.payType +
                    "&requestId=" + momo.requestId +
                    "&responseTime=" + momo.responseTime +
                    "&resultCode=" + momo.resultCode +
                    "&transId=" + momo.transId;

            //hash rawData
            MoMoSecurity crypto = new MoMoSecurity();
            string secretKey = _configuration["MomoServices:secretKey"];
            string temp = crypto.signSHA256(rawHash, secretKey);
            TransactionStatus transactionStatus = TransactionStatus.Failed;
            OrderStatus orderStatus = OrderStatus.Failed;
            //check chữ ký
            if (temp != momo.signature)
                throw new Exception("Sai chữ ký");
            //lấy orderid
            Guid orderId = Guid.Parse(momo.extraData);
            try
            {
                if (momo.resultCode == 0)
                {
                    transactionStatus = TransactionStatus.Success;
                    orderStatus = OrderStatus.Paid;
                }
                var order = await _unit.OrderRepository.GetAllQueryable().FirstOrDefaultAsync(x => x.Id == orderId);
                if (order == null)
                    throw new Exception("Không tìm thấy đơn hàng.");
                var orderTransaction = new OrderTransaction();
                orderTransaction.OrderId = orderId;
                orderTransaction.Amount = momo.amount;
                orderTransaction.IpnURL = IpnUrl;
                orderTransaction.Information = momo.orderInfo;
                orderTransaction.PartnerCode = partnerCode;
                orderTransaction.RedirectUrl = redirectUrl;
                orderTransaction.RequestId = momo.requestId;
                orderTransaction.RequestType = "captureWallet";
                orderTransaction.TransactionStatus = transactionStatus;
                orderTransaction.PaymentMethod = "MOMO Payment";
                orderTransaction.OrderIdFormMomo = momo.orderId;
                orderTransaction.OrderType = momo.orderType;
                orderTransaction.TransId = momo.transId;
                orderTransaction.ResultCode = momo.resultCode;
                orderTransaction.Message = momo.message;
                orderTransaction.PayType = momo.payType;
                orderTransaction.ResponseTime = momo.responseTime;
                orderTransaction.ExtraData = momo.extraData;
                // Tạo transaction
                orderTransaction.Signature = momo.signature;
                await _unit.OrderTransactionRepository.AddAsync(orderTransaction);

                if (momo.resultCode != 0)
                {
                    var lists = new List<Product>();
                    var ListProduct = await _unit.OrderDetailRepository.GetAllQueryable()
                        .Include(x => x.Product)
                        .Where(x => x.IsDeleted == false && x.OrderId == orderId).ToListAsync();
                    foreach (var item in ListProduct)
                    {
                        item.Product.isDisable = false;
                        lists.Add(item.Product);
                    }
                    _unit.ClearTrack();
                    _unit.ProductRepository.UpdateRange(lists);
                    await _unit.SaveChangeAsync();
                }
                //Update Order Status
                order.OrderStatus = orderStatus;
                _unit.OrderRepository.Update(order);
                await _unit.SaveChangeAsync();
                if (momo.resultCode != 0)
                {
                    await UpdateProductFromOrder(orderId);
                }
            }
            catch (Exception exx)
            {
                throw new Exception($"tạo Transaction lỗi: {exx.Message}");
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
        public async Task<Guid> CreateOrderByTransaction(OrderModel model, string? userId)
        {
            var customer = await GetCustomerAsync(model, userId);
            _unit.BeginTransaction();
            try
            {
                Guid orderId = await CreateOrder(model, customer.Id);
                foreach (var item in model.ListProduct)
                {
                    await CreateOrderDetail(item, orderId);
                }
                await UpdateOrder(orderId, model.ListProduct.Distinct().ToList());
                await _unit.CommitTransactionAsync();
                return orderId;
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
            var customer = await _unit.CustomerRepository.GetAllQueryable().FirstOrDefaultAsync(x => x.UserId.ToLower().Equals(user.Id.ToLower()));
            if (customer == null)
                throw new Exception("Không tìm thấy thông tin người dùng");
            return customer;
        }

        public async Task<Guid> CreateOrder(OrderModel model, Guid customerId)
        {
            try
            {
                var order = _mapper.Map<Order>(model);
                order.OrderDate = DateTime.Now;
                order.CustomerId = customerId;
                order.Price = 0;
                order.DeliveryPrice = 0;
                order.TotalPrice = 0;
                order.OrderStatus = OrderStatus.Waiting;
                await _unit.OrderRepository.AddAsync(order);
                await _unit.SaveChangeAsync();
                return order.Id;
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


        public async Task UpdateOrder(Guid orderId, List<Guid> productsId)
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

                _unit.ClearTrack();
                _unit.OrderRepository.Update(order);
                await _unit.SaveChangeAsync();
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
                order.OrderStatus = OrderStatus.Preparing;
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


