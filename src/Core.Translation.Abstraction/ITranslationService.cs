namespace NArchitecture.Core.Translation.Abstraction;

public interface ITranslationService
{
    public Task<string> TranslateAsync(string text, string to, string from = "en");
}
