using Application.Interfaces;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MyProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashBoardController : ControllerBase
    {
        private readonly IDashBoardService _dashBoardService;

        public DashBoardController(IDashBoardService dashBoardService)
        {
            _dashBoardService = dashBoardService;
        }
        [HttpGet]
        [Authorize("Manager")]
        public async Task<IActionResult> Get()
        {
            try
            {
                var dashboard = await _dashBoardService.GetDashboardAsync();
                if (dashboard == null)
                {
                    return BadRequest("Không tìm thấy!");
                }
                else
                {
                    return Ok(dashboard);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
