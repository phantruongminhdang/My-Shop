using Application.Interfaces;
using Application.Interfaces.Services;
using System.Security.Claims;

namespace WebAPI.Services
{
    public class ClaimsService : IClaimsService
    {
        public ClaimsService(IHttpContextAccessor httpContextAccessor)
        {
            // todo implementation to get the current userId
            var Id = httpContextAccessor.HttpContext?.User?.FindFirstValue("userId");
            var isAdmin = httpContextAccessor.HttpContext?.User?.FindFirstValue("isAdmin");
            var isCustomer = httpContextAccessor.HttpContext?.User?.FindFirstValue("isCustomer");

            GetCurrentUserId = string.IsNullOrEmpty(Id) ? Guid.Empty : Guid.Parse(Id);
            if (string.IsNullOrEmpty(isAdmin))
                GetIsAdmin = false;
            else if(isAdmin.Equals("True")) GetIsAdmin = true;
            else GetIsAdmin = false;

            if (string.IsNullOrEmpty(isCustomer))
                GetIsCustomer = false;
            else if (isCustomer.Equals("True")) GetIsCustomer = true;
            else GetIsCustomer = false;
        }

        public Guid GetCurrentUserId { get; }
        public bool GetIsAdmin { get; }
        public bool GetIsCustomer { get; }
    }
}
