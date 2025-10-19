namespace Bielu.OnePassword.Connect.Aspnet.Secrets;

public class OnePasswordConnectConfigurationSource : IConfigurationSource
{
    private readonly OnePasswordConnectClient _client;

      public OnePasswordConnectConfigurationSource(OnePasswordConnectClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new OnePasswordConnectConfigurationProvider(_client);
    }
}
