namespace Core.Localization.Abstraction;

public interface ILocalizationService
{
    public ICollection<string>? AcceptLocales { get; set; }

    /// <summary>
    /// Gets the localized string for the given key by <see cref="AcceptLocales"/>.
    /// </summary>
    public string GetLocalized(string section, string key);

    public string GetLocalized(string section, string key, ICollection<string> acceptLocales);
}
