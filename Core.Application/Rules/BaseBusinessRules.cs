using Core.CrossCuttingConcerns.Exceptions.Types;

namespace Core.Application.Rules;

public abstract class BaseBusinessRules
{
    public virtual Task ThrowBusinessException(string message)
    {
        throw new BusinessException(message);
    }
}
