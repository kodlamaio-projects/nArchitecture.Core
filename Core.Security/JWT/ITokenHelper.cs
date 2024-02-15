using NArchitecture.Core.Security.Entities;

namespace NArchitecture.Core.Security.JWT;

public interface ITokenHelper<TUserId, TOperationClaimId>
{
    AccessToken CreateToken(User<TUserId> user, IList<OperationClaim<TOperationClaimId>> operationClaims);

    RefreshToken<TUserId> CreateRefreshToken(User<TUserId> user, string ipAddress);
}
