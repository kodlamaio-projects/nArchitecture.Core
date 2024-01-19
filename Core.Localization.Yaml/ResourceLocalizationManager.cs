using Core.Localization.Abstraction;
using YamlDotNet.RepresentationModel;

namespace Core.Localization.Resource.Yaml;

public class ResourceLocalizationManager : ILocalizationService
{
    private const string _defaultLocale = "en";
    public ICollection<string>? AcceptLocales { get; set; }

    // <locale, <section, <path, content>>>
    private readonly Dictionary<string, Dictionary<string, (string path, YamlMappingNode? content)>> _resourceData = new();

    public ResourceLocalizationManager(Dictionary<string, Dictionary<string, string>> resources)
    {
        foreach ((string locale, Dictionary<string, string> sectionResources) in resources)
        {
            if (!_resourceData.ContainsKey(locale))
                _resourceData.Add(locale, new Dictionary<string, (string path, YamlMappingNode? value)>());

            foreach ((string sectionName, string sectionResourcePath) in sectionResources)
                _resourceData[locale].Add(sectionName, (sectionResourcePath, null));
        }
    }

    public string GetLocalized(string section, string key) => GetLocalized(section, key, AcceptLocales);

    public string GetLocalized(string section, string key, ICollection<string> acceptLocales)
    {
        string? localization;
        if (acceptLocales is not null)
            foreach (string locale in acceptLocales)
            {
                localization = getLocalizationFromResource(section, key, locale);
                if (localization is not null)
                    return localization;
            }

        localization = getLocalizationFromResource(section, key, _defaultLocale);
        if (localization is not null)
            return localization;

        return key;
    }

    private string? getLocalizationFromResource(string section, string key, string locale)
    {
        if (
            _resourceData.TryGetValue(locale, out Dictionary<string, (string path, YamlMappingNode? content)>? cultureNode)
            && cultureNode.TryGetValue(section, out (string path, YamlMappingNode? content) sectionNode)
        )
        {
            if (sectionNode.content is null)
                lazyLoadResource(sectionNode.path, out sectionNode.content);

            if (sectionNode.content!.Children.TryGetValue(new YamlScalarNode(key), out var cultureValueNode))
                return cultureValueNode.ToString();
        }

        return null;
    }

    private void lazyLoadResource(string path, out YamlMappingNode? content)
    {
        using StreamReader reader = new(path);
        YamlStream yamlStream = new();
        yamlStream.Load(reader);
        content = (YamlMappingNode)yamlStream.Documents[0].RootNode;
    }
}
