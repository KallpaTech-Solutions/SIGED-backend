namespace Siged.Application.DTOs.Security
{
    public class ChangePasswordDto
    {
        public required string CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}