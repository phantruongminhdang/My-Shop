using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Momo;
using Application.ViewModels;
using Application.ViewModels.OrderViewModels;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IClaimsService _claimsService;

        public OrderController(IOrderService orderService, IClaimsService claimsService)
        {
            _orderService = orderService;
            _claimsService = claimsService;
        }
        [HttpPost]
        public async Task<IActionResult> CreateOrderAsync([FromBody] OrderModel model)
        {
            try
            {
                var userId = _claimsService.GetCurrentUserId.ToString().ToLower();
                var resultValidate = await _orderService.ValidateOrderModel(model, userId);
                if (resultValidate == null)
                {
                    var result = await _orderService.CreateOrderAsync(model, userId);
                    if (result != null)
                        return Ok(result);
                    else return BadRequest(result);
                }
                else
                {
                    return BadRequest(resultValidate);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("IpnHandler")]
        public async Task<IActionResult> IpnAsync([FromBody] MomoRedirect momo)
        {
            try
            {
                await _orderService.HandleIpnAsync(momo);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAsync([FromQuery] int pageIndex, [FromQuery] int pageSize)
        {
            try
            {
                var userId = _claimsService.GetCurrentUserId.ToString().ToLower();
                var orders = await _orderService.GetPaginationAsync(userId, pageIndex, pageSize);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        [HttpGet("{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetByIdAsync(Guid orderId)
        {
            try
            {
                var userId = _claimsService.GetCurrentUserId.ToString().ToLower();
                var orders = await _orderService.GetByIdAsync(userId, orderId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [Authorize(Roles = "Manager,Staff,Gardener")]
        [HttpPut("{orderId}")]
        [Authorize]
        public async Task<IActionResult> UpdateStatusAsync(Guid orderId, OrderStatus orderStatus)
        {
            try
            {
                await _orderService.UpdateOrderStatusAsync(orderId, orderStatus);
                return Ok("Cập nhật trạng thái đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("OrderStatus")]
        [Authorize]
        public async Task<IActionResult> GetOrderStatusAsync()
        {
            try
            {
                List<EnumModel> enums = ((OrderStatus[])Enum.GetValues(typeof(OrderStatus))).Select(c => new EnumModel() { Value = (int)c, Display = c.ToString() }).ToList();
                return Ok(enums);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "Manager")]
        [HttpPut("DeliveryFinishing/{orderId}")]
        [Authorize]
        public async Task<IActionResult> FinishDeliveryOrder(Guid orderId, [FromForm] FinishDeliveryOrderModel finishDeliveryOrderModel)
        {
            try
            {
                await _orderService.FinishDeliveryOrder(orderId, finishDeliveryOrderModel);
                return Ok("Cập nhật trạng thái đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        //[Authorize(Roles = "Manager")]
        [HttpPut("ShipperAddition/{orderId}")]
        //[Authorize]
        public async Task<IActionResult> ShipperAddition(Guid orderId, Guid shipperId)
        {
            try
            {
                await _orderService.ShipperAddition(orderId, shipperId);
                return Ok("Cập nhật trạng thái đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
