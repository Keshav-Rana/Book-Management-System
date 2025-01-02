using System.ComponentModel.DataAnnotations;

namespace Models.User;
public class User
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt;
    public DateTime LastModifiedAt;
    public DateTime LastLogin;
}
