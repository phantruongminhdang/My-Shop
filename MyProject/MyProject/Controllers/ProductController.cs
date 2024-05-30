﻿using Application.Interfaces.Services;
using Application.ViewModels.ProductViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IClaimsService _claims;

        public ProductController(IProductService productService,
            IClaimsService claimsService)
        {
            _productService = productService;
            _claims = claimsService;
        }
        [HttpGet("Pagination")]
        public async Task<IActionResult> GetPagination([FromQuery] int pageIndex, int pageSize)
        {
            try
            {
                var products = await _productService.GetPagination(pageIndex, pageSize, _claims.GetIsAdmin);
                if (products.Items.Count == 0)
                {
                    return BadRequest("Không tìm thấy!");
                }
                else
                {
                    return Ok(products);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var products = await _productService.GetAll(_claims.GetIsAdmin);
                if (products.Items.Count == 0)
                {
                    return BadRequest("Không tìm thấy!");
                }
                else
                {
                    return Ok(products);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("Filter")]
        public async Task<IActionResult> Post([FromQuery] int pageIndex, int pageSize, [FromBody] FilterProductModel filterBonsaiModel)
        {
            try
            {
                var products = await _productService.GetByFilter(pageIndex, pageSize, filterBonsaiModel, _claims.GetIsAdmin);
                return Ok(products);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Post([FromForm] ProductModel productModel)
        {
            try
            {
                await _productService.AddAsync(productModel);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok("Tạo sản phẩm thành công!");
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] Guid id)
        {
            try
            {
                var product = await _productService.GetById(id, _claims.GetIsAdmin);
                if (product == null)
                {
                    return NotFound();
                }
                return Ok(product);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Put([FromRoute] Guid id, [FromForm] ProductModel productModel)
        {
            try
            {
                await _productService.Update(id, productModel);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok("Cập nhật thành công!");
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            try
            {
                await _productService.Delete(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok("Xóa thành công!");
        }
        [HttpGet("BoughtProduct")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetBoughtBonsai()
        {
            try
            {
                var products = await _productService.GetBoughtProduct(_claims.GetCurrentUserId);
                if (products.Items.Count == 0)
                {
                    return BadRequest("Không tìm thấy!");
                }
                else
                {
                    return Ok(products);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("Category")]
        public async Task<IActionResult> GetByCategory([FromQuery] int pageIndex, int pageSize, Guid categoryId)
        {
            try
            {
                var products = await _productService.GetByCategory(pageIndex, pageSize, categoryId);
                if (products.Items.Count == 0)
                {
                    return BadRequest("Không tìm thấy!");
                }
                else
                {
                    return Ok(products);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut("Disable")]
        public async Task<IActionResult> DisableBonsai([FromQuery] Guid productId)
        {
            try
            {
                await _productService.DisableProduct(productId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("CurrentCart")]
        public async Task<IActionResult> GetCurrentCart([FromBody] List<Guid> productId)
        {
            try
            {
                var products = await _productService.getCurrentCart(productId);
                if (products.Count == 0)
                {
                    return BadRequest("Không tìm thấy!");
                }
                else
                {
                    return Ok(products);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}