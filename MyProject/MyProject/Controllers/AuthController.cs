using Application.Interfaces.Services;
using Application.Services;
using Application.ViewModels.AuthViewModel;
using Domain.Entities;
using Domain.Entities.Base;
using Firebase.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(IAuthService authService,UserManager<ApplicationUser> userManager)
        {
            _auth = authService;
            _userManager = userManager;
        }
        [HttpGet("OtpHandler")]
        public async Task<IActionResult> OtpHandler(string Email, string Otp)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(Email);
                if (user == null)
                {
                    return NotFound("Không tìm thấy người dùng.");
                }
                var result = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", Otp);
                if (result)
                {
                    return Ok("Mã OTP chính xác");
                }
                else
                {
                    return BadRequest("Mã OTP không chính xác");
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
           
            try
            {
                //var result = await _identityService.AuthenticateAsync(email, password);

                var user = await _userManager.FindByNameAsync(model.Email);
                if (user == null)
                {
                    user = await _userManager.FindByEmailAsync(model.Email);
                    if (user == null)
                    {
                        return NotFound("Tài khoản này không tồn tại!");
                    }
                }

                //lấy host để redirect về send email
                var referer = Request.Headers["Referer"].ToString().Trim();
                var callbackUrl = await GetCallbackUrlAsync(model.Email.Trim(), referer, "EmailConfirm");

                var result = await _auth.Login(model.Email, model.Password, callbackUrl);
                if (result == null)
                {
                    return NotFound("Đăng nhập không thành công!");
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }

        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            
            try
            {
                var validateResult = await _auth.ValidateAsync(model);
                if (validateResult != null)
                {
                    return BadRequest(validateResult);
                }
                //check account
                await _auth.CheckAccountExist(model);
                //kết thúc lấy host để redirect về và tạo link
                var temp = await _auth.Register(model);
                if (temp == null)
                {
                    //lấy host để redirect về
                    var referer = Request.Headers["Referer"].ToString().Trim();
                    var callbackUrl = await GetCallbackUrlAsync(model.Email.Trim(), referer, "EmailConfirm");
                    await _auth.SendEmailAsync(model.Email.Trim(), callbackUrl, "EmailConfirm");
                    return Ok("Đăng ký tài khoản Thanh Sơn Garden thành công. Vui lòng kiểm tra email để kích hoạt tài khoản!");
                }
                else
                {
                    return BadRequest(temp);
                }
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string? code, string? userId)
        {
            
            try
            {
                await _auth.ConfirmEmailAsync(code, userId);
                return Ok("Xác nhận Email thành công! Bây giờ bạn có thể đăng nhập vào tài khoản của mình bằng Email hoặc Username vừa xác thực !");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /*[HttpPost("Login/Google")]
        public async Task<IActionResult> LoginGoogle([FromBody] ExternalLoginModel model)
        {
            try
            {
                
                var result = await _auth.HandleExternalLoginAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
*//*
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPassModel model)
        {
            try
            {
                if(model.UserId == null)
                {
                    return BadRequest("Vui lòng không để trống id của người dùng.");
                }
                var _auth = new AuthService(_userManager, _signInManager, _configuration, _environment, _unit);
                var result = await _auth.ResetPasswordAsync(model);
                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return BadRequest("Không tìm thấy địa chỉ emal");
                }
                //lấy host để redirect về
                var _auth = new AuthService(_userManager, _signInManager, _configuration, _environment, _unit);
                var referer = Request.Headers["Referer"].ToString().Trim();
                var callbackUrl = await GetCallbackUrlAsync(email.Trim(), referer, "ResetPassword");
                await _auth.SendEmailAsync(email.Trim(), callbackUrl, "ResetPassword");
                return Ok("Yêu cầu đổi mật khẩu đã được gửi thành công đến địa chỉ email của bạn. Vui lòng kiểm tra hộp thư đến của bạn và xác thực email để tiến hành đổi mật khẩu.");
            }
            catch (Exception e)
            {
                return BadRequest("Xác nhận email không thành công: " + e.Message);
            }
        }*/

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            try
            {

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return BadRequest("Không tìm thấy địa chỉ emal");
                }
                //lấy host để redirect về
                
                var token = await _userManager.GenerateTwoFactorTokenAsync(user,"Email");
                await _auth.SendEmailAsync(email.Trim(), token, "ResetPasswordForMobile");
                return Ok("Yêu cầu đổi mật khẩu đã được gửi thành công đến địa chỉ email của bạn. Vui lòng kiểm tra hộp thư đến của bạn và xác thực email để tiến hành đổi mật khẩu.");
            }
            catch (Exception e)
            {
                return BadRequest("Xác nhận email không thành công: " + e.Message);
            }
        }
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPassModel model)
        {
            try
            {
                if (model.Email == null)
                {
                    return BadRequest("Vui lòng không để trống Email của người dùng.");
                }
               
                var result = await _auth.ResetPasswordAsync(model);
                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [NonAction]
        public async Task<string> GetCallbackUrlAsync(string email, string referer, string type)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                user = await _userManager.FindByNameAsync(email);
            string callbackUrl = "";
            string schema;
            string host;
            var code = "";
            var action = "";
            switch (type)
            {
                case "EmailConfirm":
                    code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    action = "ConfirmEmail";
                    break;

                case "ResetPassword":
                    code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    action = "ResetPassword";
                    break;
            }
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            if (!referer.Equals("") && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
            {
                schema = uri.Scheme; // Lấy schema (http hoặc https) của frontend
                host = uri.Host; // Lấy host của frontend
                callbackUrl = schema + "://" + host + Url.Action(action, "Auth", new { userId = user.Id, code = code });
            }
            if (referer.Equals("https://localhost:5001/swagger/index.html"))
            {
                callbackUrl = "https://localhost:5001" + Url.Action(action, "Auth", new { userId = user.Id, code = code });
            }
            else if(referer.Contains("http://localhost:5173"))
            {
                callbackUrl = "http://localhost:5173" + Url.Action(action, "Auth", new { userId = user.Id, code = code });
            }
            return callbackUrl;
        }

    }
}
