using Amazon.Translate;
using Amazon.Translate.Model;
using NArchitecture.Core.Translation.Abstraction;

namespace NArchitecture.Core.Translation.AmazonTranslate;

public class AmazonTranslateLocalizationManager : ITranslationService
{
    private readonly AmazonTranslateClient _client;

    public AmazonTranslateLocalizationManager(AmazonTranslateConfiguration configuration)
    {
        _client = new AmazonTranslateClient(configuration.AccessKey, configuration.SecretKey, configuration.RegionEndpoint);
    }

    public async Task<string> TranslateAsync(string text, string to, string from = "en")
    {
        TranslateTextRequest request =
            new()
            {
                SourceLanguageCode = from,
                TargetLanguageCode = to,
                Text = text,
            };

        TranslateTextResponse response = await _client.TranslateTextAsync(request);
        return response.TranslatedText;
    }
}
