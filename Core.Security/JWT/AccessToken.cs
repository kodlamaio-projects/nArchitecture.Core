namespace NArchitecture.Core.Security.JWT;

public class AccessToken
{
    public string Token { get; set; }
    public DateTime ExpirationDate { get; set; }

    public AccessToken()
    {
        Token = string.Empty;
    }

    public AccessToken(string token, DateTime expirationDate)
    {
        Token = token;
        ExpirationDate = expirationDate;
    }
}
