using System.Text.Json.Serialization;

namespace NArchitecture.Core.Application.Dtos;

public class UserForRegisterDto : IDto
{
    public required string Email { get; set; }

    [JsonIgnore]
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
