namespace Core.Application.Dtos;

public class UserForRegisterDto : IDto
{
    public string Email { get; set; }
    public string Password { get; set; }

    public UserForRegisterDto()
    {
        Email = string.Empty;
        Password = string.Empty;
    }

    public UserForRegisterDto(string email, string password)
    {
        Email = email;
        Password = password;
    }
}
