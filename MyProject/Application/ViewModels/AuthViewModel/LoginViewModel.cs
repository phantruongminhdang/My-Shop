namespace Application.ViewModels.AuthViewModel
{
    public class LoginViewModel
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }

        public string Token { get; set; }
        public string Role { get; set; }
    }
}
