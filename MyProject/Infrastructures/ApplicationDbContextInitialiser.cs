using Domain.Entities.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructures
{
    public class ApplicationDbContextInitialiser
    {
        private readonly ILogger<ApplicationDbContextInitialiser> _logger;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, AppDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task InitialiseAsync()
        {
            try
            {
                if (_context.Database.IsSqlServer())
                {
                    await _context.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initialising the database.");
                throw;
            }
        }

        public async Task SeedAsync()
        {
            try
            {
                await TrySeedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
        public async Task TrySeedAsync()
        {
            var managerRole = new IdentityRole("Manager");

            if (_roleManager.Roles.All(r => r.Name != managerRole.Name))
            {
                await _roleManager.CreateAsync(managerRole);
            }

            // customer roles
            var customerRole = new IdentityRole("Customer");

            if (_roleManager.Roles.All(r => r.Name != customerRole.Name))
            {
                await _roleManager.CreateAsync(customerRole);
            }

            // admin users
            var manager = new ApplicationUser { UserName = "manager@localhost", Email = "manager@localhost", Fullname = "Manager", AvatarUrl = "(null)"};

            if (_userManager.Users.All(u => u.UserName != manager.UserName))
            {
                await _userManager.CreateAsync(manager, "Manager@123");
                if (!string.IsNullOrWhiteSpace(managerRole.Name))
                {
                    await _userManager.AddToRolesAsync(manager, new[] { managerRole.Name });
                }
            }

        }
    }
}
