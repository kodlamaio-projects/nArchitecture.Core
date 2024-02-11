using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Security.EmailAuthenticator;
using NArchitecture.Core.Security.JWT;
using NArchitecture.Core.Security.OtpAuthenticator;
using NArchitecture.Core.Security.OtpAuthenticator.OtpNet;

namespace NArchitecture.Core.Security.DependencyInjection;

public static class SecurityServiceRegistration
{
    public static IServiceCollection AddSecurityServices<TUserId, TOperationClaimId>(this IServiceCollection services)
    {
        services.AddScoped<ITokenHelper<TUserId, TOperationClaimId>, JwtHelper<TUserId, TOperationClaimId>>();
        services.AddScoped<IEmailAuthenticatorHelper, EmailAuthenticatorHelper>();
        services.AddScoped<IOtpAuthenticatorHelper, OtpNetOtpAuthenticatorHelper>();

        return services;
    }
}
