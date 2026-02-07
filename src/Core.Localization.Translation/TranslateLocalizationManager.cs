using System.Data;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Translation.Abstraction;

namespace NArchitecture.Core.Localization.Translation;

public class TranslateLocalizationManager : ILocalizationService
{
    private const string _defaultLocale = "en";
    public ICollection<string>? AcceptLocales { get; set; }

    private readonly ITranslationService _translationService;

    public TranslateLocalizationManager(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    public Task<string> GetLocalizedAsync(string key, string? keySection = null)
    {
        return GetLocalizedAsync(key, AcceptLocales ?? throw new NoNullAllowedException(nameof(AcceptLocales)));
    }

    public async Task<string> GetLocalizedAsync(string key, ICollection<string> acceptLocales, string? keySection = null)
    {
        string? localization;

        if (acceptLocales is not null)
            foreach (string locale in acceptLocales)
            {
                localization = await _translationService.TranslateAsync(key, locale);
                if (!string.IsNullOrWhiteSpace(localization))
                    return localization;
            }

        localization = await _translationService.TranslateAsync(key, _defaultLocale);
        if (!string.IsNullOrWhiteSpace(localization))
            return localization;

        return key;
    }

    public Task<string> GetLocalizedAsync(string key, string? keySection = null, params object[] args)
    {
        return GetLocalizedAsync(key, AcceptLocales ?? throw new NoNullAllowedException(nameof(AcceptLocales)), keySection, args);
    }

    public async Task<string> GetLocalizedAsync(string key, ICollection<string> acceptLocales, string? keySection = null, params object[] args)
    {
        string? localization;

        if (acceptLocales is not null)
            foreach (string locale in acceptLocales)
            {
                localization = await _translationService.TranslateAsync(key, locale);
                if (!string.IsNullOrWhiteSpace(localization))
                    return formatMessage(localization, args);
            }

        localization = await _translationService.TranslateAsync(key, _defaultLocale);
        if (!string.IsNullOrWhiteSpace(localization))
            return formatMessage(localization, args);

        return key;
    }

    private static string formatMessage(string message, params object[] args)
    {
        return args is { Length: > 0 } ? string.Format(message, args) : message;
    }
}
