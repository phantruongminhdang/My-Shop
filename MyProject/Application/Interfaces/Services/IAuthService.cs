using Application.ViewModels.AuthViewModel;
using Domain.Entities;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IAuthService
    {
        public Task<LoginViewModel> Login(string email, string pass, string callbackUrl);
        public Task<List<string>> Register(RegisterModel model);
        public Task CheckAccountExist(RegisterModel model);
        public Task ConfirmEmailAsync(string? code, string? userId);
        public Task<bool> SendEmailAsync(string username, string callbackUrl, string type);
        /* public Task<string> ResetPasswordAsync(ResetPassModel model);*/
        public Task<IList<string>> ValidateAsync(RegisterModel model);
        public Task<string> ResetPasswordAsync(ResetPassModel model);
    }
}
