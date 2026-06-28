namespace CoreAPI.Models
{
    public class UserAdminResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class CreateUserAdminRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateUserAdminRequest
    {
        public string Username { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
