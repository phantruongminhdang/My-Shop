using Application.Commons;
using Application.Interfaces.Services;
using Application.Validations.Auth;
using Application.Validations.User;
using Application.ViewModels.AuthViewModel;
using Application.ViewModels.UserViewModels;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Base;
using Firebase.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;
using System.Net.WebSockets;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentTime _currentTime;
        private readonly AppConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IClaimsService _claims;
        private readonly IFirebaseService _fireBaseService;

        private readonly RoleManager<IdentityRole> _roleManager;

        public UserService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentTime currentTime,
            AppConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IClaimsService claims,
            IFirebaseService fireBaseService,
            RoleManager<IdentityRole> roleManager
            )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentTime = currentTime;
            _configuration = configuration;
            _userManager = userManager;
            _claims = claims;
            _fireBaseService = fireBaseService;
            _roleManager = roleManager;
        }
        public async Task<List<string>> ChangePasswordAsync(ChangePassModel model, string userId)
        {
            if (!model.NewPassword.Equals(model.ConfirmPassword))
            {
                throw new Exception("Mật khẩu xác nhận không trùng khớp!");

            }
            if (model.NewPassword.Equals(model.OldPassword))
            {
                throw new Exception("Mật khẩu mới phải khác mật khẩu cũ!");
            }
            try
            {

                var user = await _userManager.FindByIdAsync(userId);
                var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    return null;
                }
                else
                {
                    List<string> err = new List<string>();
                    foreach (var item in result.Errors)
                    {
                        err.Add(item.Description);
                    }
                    return err;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Đã xảy ra lỗi: {ex.Message}");
            }
        }
        public async Task<IList<string>> UpdateUserAsync(UserRequestModel model, string userId)
        {
            var validateResult = await ValidateUserUpdateModelAsync(model);
            if (validateResult != null)
            {
                return validateResult;
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new Exception("Không tìm thấy người dùng!");
            }
            else
            {
                var temp = await _userManager.Users.Where(x => !x.Id.ToLower().Equals(user.Id.ToLower()) && x.UserName.ToLower().Equals(model.Username.ToLower())).FirstOrDefaultAsync();
                if (temp != null)
                    throw new Exception("Tên đăng nhập này đã được sử dụng!");
                try
                {
                    string url = null;
                    if (model.Avatar != null)
                    {
                        Random random = new Random();
                        string newImageName = user.Id + "_i" + model.Avatar.Name.Trim() + random.Next(1, 10000).ToString();
                        string folderName = $"user/{user.Id}/Image";
                        string imageExtension = Path.GetExtension(model.Avatar.FileName);
                        //Kiểm tra xem có phải là file ảnh không.
                        string[] validImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                        const long maxFileSize = 20 * 1024 * 1024;
                        if (Array.IndexOf(validImageExtensions, imageExtension.ToLower()) == -1 || model.Avatar.Length > maxFileSize)
                        {
                            throw new Exception("Có chứa file không phải ảnh hoặc quá dung lượng tối đa(>20MB)!");
                        }
                        url = await _fireBaseService.UploadFileToFirebaseStorage(model.Avatar, newImageName, folderName);
                        if (url == null)
                            throw new Exception("Lỗi khi đăng ảnh lên firebase!");
                        user.AvatarUrl = url;
                    }
                    user.UserName = model.Username;
                    user.NormalizedUserName = model.Username.ToUpper();
                    user.Fullname = model.Fullname;
                    user.PhoneNumber = model.PhoneNumber;
                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                        return null;

                    var errors = new List<string>();
                    errors.AddRange(result.Errors.Select(x => x.Description));
                    return errors;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }
        public async Task<IList<string>> ValidateUserUpdateModelAsync(UserRequestModel model)
        {
            var validator = new UserModelValidator();
            var result = await validator.ValidateAsync(model);
            if (!result.IsValid)
            {
                var errors = new List<string>();
                errors.AddRange(result.Errors.Select(x => x.ErrorMessage));
                return errors;
            }
            return null;
        }
        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("Không tìm thấy người dùng.");
            return user;
        }
        public async Task<Pagination<UserViewModel>> GetListUserAsync(int pageIndex = 0, int pageSize = 20)
        {
            var manager = await _userManager.GetUsersInRoleAsync("Manager");
            var listUser = await _userManager.Users.AsNoTracking().Where(x => x.Id != manager.FirstOrDefault().Id).OrderBy(x => x.Email).ToListAsync();
            var itemCount = listUser.Count();
            var items = listUser.Skip(pageIndex * pageSize)
                                    .Take(pageSize)
                                    .ToList();
            var result = new Pagination<ApplicationUser>()
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalItemsCount = itemCount,
                Items = items,
            };
            var paginationList = _mapper.Map<Pagination<UserViewModel>>(result);
            foreach (var item in paginationList.Items)
            {
                var user = await _userManager.FindByIdAsync(item.Id);
                var isLockout = await _userManager.IsLockedOutAsync(user);
                var roles = await _userManager.GetRolesAsync(user);
                string role = "";
                if (roles != null && roles.Count > 0)
                {
                    role = roles[0];
                }
                item.IsLockout = isLockout;
                item.Role = role;
            }
            return paginationList;
        }
        public async Task<UserViewModel> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            var result = _mapper.Map<UserViewModel>(user);


            var isLockout = await _userManager.IsLockedOutAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            string role = "";
            if (roles != null && roles.Count > 0)
            {
                role = roles[0];
            }
            result.IsLockout = isLockout;
            result.Role = role;
            return result;
        }

        public async Task<string> LockOrUnlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToLower());
            if (user == null)
                throw new Exception("Không tìm thấy người dùng.");
            var isLockout = await _userManager.IsLockedOutAsync(user);
            if (!isLockout)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                return "Khóa tài khoản thành công!";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                return "Mở khóa tài khoản thành công!";

            }
        }
        public async Task<IList<string>> CreateUserAccount(UserCreateModel model)
        {
            var validateResult = await ValidateUserCreateModelAsync(model);
            if (validateResult != null)
            {
                return validateResult;
            }
            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user != null)
            {
                throw new Exception("Địa chỉ email này đã được sử dụng!");
            }
            else
            {
                var temp = await _userManager.Users.Where(x => x.UserName.ToLower().Equals(model.Email.ToLower())).FirstOrDefaultAsync();
                if (temp != null)
                    throw new Exception("Tên đăng nhập này đã được sử dụng!");
                try
                {
                    string url = null;
                    ApplicationUser newUser = new ApplicationUser()
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        Fullname = model.Fullname,
                        PhoneNumber = model.PhoneNumber,
                        IsRegister = true
                    };
                    if (model.Role.Equals("Gardener"))
                    {
                        newUser.EmailConfirmed = true;
                    }
                    if (model.Avatar != null)
                    {
                        Random random = new Random();
                        string newImageName = newUser.Id + "_i" + model.Avatar.Name.Trim() + random.Next(1, 10000).ToString();
                        string folderName = $"user/{newUser.Id}/Image";
                        string imageExtension = Path.GetExtension(model.Avatar.FileName);
                        //Kiểm tra xem có phải là file ảnh không.
                        string[] validImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                        const long maxFileSize = 20 * 1024 * 1024;
                        if (Array.IndexOf(validImageExtensions, imageExtension.ToLower()) == -1 || model.Avatar.Length > maxFileSize)
                        {
                            throw new Exception("Có chứa file không phải ảnh hoặc quá dung lượng tối đa(>20MB)!");
                        }
                        url = await _fireBaseService.UploadFileToFirebaseStorage(model.Avatar, newImageName, folderName);
                        if (url == null)
                            throw new Exception("Lỗi khi đăng ảnh lên firebase!");
                    }
                    newUser.AvatarUrl = url;
                    var result = await _userManager.CreateAsync(newUser, "NewAccount1!");
                    if (result.Succeeded)
                    {
                        var tempUser = await _userManager.FindByIdAsync(newUser.Id);
                        try
                        {
                            await CreateAccountAsync(tempUser, model.Role);
                            return null;
                        }
                        catch (Exception ex)
                        {
                            await _userManager.DeleteAsync(tempUser);
                            throw new Exception("Đã xảy ra lỗi trong quá trình tạo tài khoản: " + ex.Message);
                        }
                    }
                    var errors = new List<string>();
                    errors.AddRange(result.Errors.Select(x => x.Description));
                    return errors;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }
        public async Task<IList<string>> ValidateUserCreateModelAsync(UserCreateModel model)
        {
            var validator = new UserCreateModelValidator();
            var result = await validator.ValidateAsync(model);
            if (!result.IsValid)
            {
                var errors = new List<string>();
                errors.AddRange(result.Errors.Select(x => x.ErrorMessage));
                return errors;
            }
            return null;
        }
        public async Task CreateAccountAsync(ApplicationUser user, string role)
        {
            switch (role)
            {
                case "Customer":
                    Customer customer = new Customer { UserId = user.Id };
                    await _unitOfWork.CustomerRepository.AddAsync(customer);
                    await _unitOfWork.SaveChangeAsync();
                    var cusResult = await _userManager.AddToRoleAsync(user, "Customer");
                    if (!cusResult.Succeeded)
                    {
                        throw new Exception("Thêm vai trò bị lỗi");
                    }
                    break;
                default: throw new Exception("Vai trò không hợp lệ");
            }
        }
        public async Task<List<string>> GetListRoleAsync()
        {
            var roles = await _roleManager.Roles.Where(x => !x.Name.Equals("Manager")).Select(x => x.Name).ToListAsync();
            return roles;
        }

        public async Task Delete(string role, ApplicationUser user)
        {
            switch (role)
            {
                case "Customer":

                    var Customer = await _unitOfWork.CustomerRepository.GetAllAsync();
                    var temp3 = Customer.Where(x => x.UserId.ToLower().Equals(user.Id.ToLower())).ToList();

                    _unitOfWork.CustomerRepository.HardDeleteRange(temp3);
                    break;
            }
            await _unitOfWork.SaveChangeAsync();
        }
    }    
}
