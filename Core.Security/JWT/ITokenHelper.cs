using NArchitecture.Core.Security.Entities;

namespace NArchitecture.Core.Security.JWT;

public interface ITokenHelper<TUserId, TOperationClaimId>
{
    AccessToken CreateToken(User<TUserId, TOperationClaimId> user, IList<OperationClaim<TOperationClaimId, TUserId>> operationClaims);

    RefreshToken<TUserId, TOperationClaimId> CreateRefreshToken(User<TUserId, TOperationClaimId> user, string ipAddress);
}
