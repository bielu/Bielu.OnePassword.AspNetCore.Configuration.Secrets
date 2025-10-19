namespace Bielu.OnePassword.Connect.Aspnet.Secrets;

public static class OnepasswordConnectAspnetSecretsExtensions
{
    /// <summary>
    /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
    /// </summary>
    /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="vaultUri">The Azure Key Vault uri.</param>
    /// <param name="credential">The credential to use for authentication.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddOnePasswordConnect(
        this IConfigurationBuilder configurationBuilder,
        string? onePasswordConnectUrl,
        string credential)
    {
        if (string.IsNullOrEmpty(onePasswordConnectUrl))
        {
            return configurationBuilder;
        }

        var authToken = new BaseBearerTokenAuthenticationProvider(new SimpleBearerTokenAuthProvider(credential));
        configurationBuilder.Add(new OnePasswordConnectConfigurationSource(
            new OnePasswordConnectClient(new DefaultRequestAdapter(authToken,
                httpClient: new HttpClient()
                {
                    BaseAddress = new Uri($"{onePasswordConnectUrl.TrimEnd("/")}/v1/"),
                    Timeout = TimeSpan.FromSeconds(10),
                }) { })));

        return configurationBuilder;
    }
}