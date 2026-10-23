using System.Text.Json;
using NArchitecture.Core.Application.Dtos;

namespace Core.Application.Tests.Dtos;

/// <summary>
/// The auth DTOs must accept the password from a JSON request body while never writing it back out.
/// </summary>
public class UserDtoSerializationTests
{
    private static readonly JsonSerializerOptions _webOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void UserForLoginDto_ReadsPasswordAndAuthenticatorCodeFromJson()
    {
        const string json = """{"email":"user@kodlama.io","password":"Passw0rd!","authenticatorCode":"123456"}""";

        UserForLoginDto dto = JsonSerializer.Deserialize<UserForLoginDto>(json, _webOptions)!;

        Assert.Equal("user@kodlama.io", dto.Email);
        Assert.Equal("Passw0rd!", dto.Password);
        Assert.Equal("123456", dto.AuthenticatorCode);
    }

    [Fact]
    public void UserForLoginDto_NeverWritesSecretsToJson()
    {
        UserForLoginDto dto = new()
        {
            Email = "user@kodlama.io",
            Password = "Passw0rd!",
            AuthenticatorCode = "123456",
        };

        string json = JsonSerializer.Serialize(dto, _webOptions);

        Assert.DoesNotContain("Passw0rd!", json);
        Assert.DoesNotContain("123456", json);
        Assert.Contains("user@kodlama.io", json);
    }

    [Fact]
    public void UserForRegisterDto_ReadsPasswordFromJsonButNeverWritesIt()
    {
        const string json = """{"email":"new@kodlama.io","password":"Passw0rd!"}""";

        UserForRegisterDto dto = JsonSerializer.Deserialize<UserForRegisterDto>(json, _webOptions)!;

        Assert.Equal("Passw0rd!", dto.Password);
        Assert.DoesNotContain("Passw0rd!", JsonSerializer.Serialize(dto, _webOptions));
    }
}
