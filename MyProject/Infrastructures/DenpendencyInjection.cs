using Application;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services;
using Application.Utils;
using Domain.Entities.Base;
using Infrastructures.Mappers;
using Infrastructures.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructures
{
    public static class DenpendencyInjection
    {
        public static IServiceCollection AddInfrastructuresService(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IOrderDetailService, OrderDetailService>();
            services.AddScoped<IOrderTransactionService, OrderTransactionService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IDashBoardService, DashBoardService>();
            services.AddScoped<IAuthService, AuthService>();

            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();
            services.AddScoped<IOrderTransactionRepository, OrderTransactionRepository>();
            services.AddScoped<IProductImageRepository, ProductImageRepository>();
            
            services.AddScoped<IFirebaseService, FirebaseService>();
            services.AddSingleton<FirebaseService>();
            services.AddScoped<IdUtil>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddSingleton<ICurrentTime, CurrentTime>();


            /*// ATTENTION: if you do migration please check file README.md
            if (configuration.GetValue<bool>("UseInMemoryDatabase"))
            {
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("mentor_v1Db"));
            }
            else
            {

            }*/
            services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(GetConnection(configuration, env),
                        builder => builder.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                
            }).AddDefaultTokenProviders().AddEntityFrameworkStores<AppDbContext>();
            // this configuration just use in-memory for fast develop
            //services.AddDbContext<AppDbContext>(option => option.UseInMemoryDatabase("test"));

            services.AddAutoMapper(typeof(MapperConfigurationsProfile).Assembly);
            services.Configure<IdentityOptions>(options => options.SignIn.RequireConfirmedEmail = true);


            return services;
        }

        private static string GetConnection(IConfiguration configuration, IWebHostEnvironment env)
        {
#if DEVELOPMENT
        return configuration.GetConnectionString("DefaultConnection") 
            ?? throw new Exception("DefaultConnection not found");
#else
            return configuration[$"ConnectionStrings:{env.EnvironmentName}"]
                ?? throw new Exception($"ConnectionStrings:{env.EnvironmentName} not found");
#endif
        }
    }


}
