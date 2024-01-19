using Core.Localization.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Core.Localization.Resource.Yaml.DependencyInjection;

public static class ServiceCollectionResourceLocalizationManagerExtension
{
    /// <summary>
    /// Adds <see cref="ResourceLocalizationManager"/> as <see cref="ILocalizationService"/> to <see cref="IServiceCollection"/>.
    /// <list type="bullet">
    ///    <item>
    ///        <description>Reads all yaml files in the "<see cref="Assembly.GetExecutingAssembly()"/>/Features/{featureName}/Resources/Locales/".</description>
    ///    </item>
    ///    <item>
    ///        <description>Yaml file names must be like {featureName}.{culture}.yaml</description>
    ///    </item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddYamlResourceLocalization(this IServiceCollection services)
    {
        services.AddScoped<ILocalizationService, ResourceLocalizationManager>(_ =>
        {
            // <locale, <featureName, resourceDir>>
            Dictionary<string, Dictionary<string, string>> resources = new();

            string[] featureDirs = Directory.GetDirectories(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Features")
            );
            foreach (string featureDir in featureDirs)
            {
                string featureName = Path.GetFileName(featureDir);
                string localeDir = Path.Combine(featureDir, "Resources", "Locales");
                if (Directory.Exists(localeDir))
                {
                    string[] localeFiles = Directory.GetFiles(localeDir);
                    foreach (string localeFile in localeFiles)
                    {
                        string localeName = Path.GetFileNameWithoutExtension(localeFile);
                        int separatorIndex = localeName.IndexOf('.');
                        string localeCulture = localeName[(separatorIndex + 1)..];

                        if (File.Exists(localeFile))
                        {
                            if (!resources.ContainsKey(localeCulture))
                                resources.Add(localeCulture, new Dictionary<string, string>());
                            resources[localeCulture].Add(featureName, localeFile);
                        }
                    }
                }
            }

            return new ResourceLocalizationManager(resources);
        });

        return services;
    }
}
