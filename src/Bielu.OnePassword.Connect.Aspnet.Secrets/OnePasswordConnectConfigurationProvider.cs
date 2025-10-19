namespace Bielu.OnePassword.Connect.Aspnet.Secrets;

public class OnePasswordConnectConfigurationProvider(
    OnePasswordConnectClient client)
    : ConfigurationProvider, IConfigurationProvider, IDisposable
{
    private readonly TimeSpan _reloadInterval = TimeSpan.FromMinutes(5);
    private OnePasswordConnectClient Client => client;
    private Task? _pollingTask;
    private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();
    private bool _disposed;
    private Dictionary<string, FullItem>? _loadedSecrets;

    public override void Load() => LoadAsync().GetAwaiter().GetResult();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        using var secretLoader = new ParallelSecretLoader(Client);
        var newLoadedSecrets = new Dictionary<string, FullItem>();
        var oldLoadedSecrets = Interlocked.Exchange(ref _loadedSecrets, null);

        var loadedSecrets = await secretLoader.Load();

        UpdateSecrets(loadedSecrets, newLoadedSecrets, oldLoadedSecrets);

        // schedule a polling task only if none exists and a valid delay is specified
        if (_pollingTask == null)
        {
            _pollingTask = PollForSecretChangesAsync();
        }
    }

    private async Task? PollForSecretChangesAsync()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            await WaitForReload().ConfigureAwait(false);
            try
            {
                await LoadAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Ignore
            }
        }
    }

    // WaitForReload is only called when the _reloadInterval has a value.
    internal virtual Task WaitForReload() => Task.Delay(_reloadInterval, _cancellationToken.Token);

    private void UpdateSecrets(IEnumerable<FullItem> loadedSecrets, Dictionary<string, FullItem> newLoadedSecrets,
        Dictionary<string, FullItem>? oldLoadedSecrets)
    {
        foreach (var secretBundle in loadedSecrets)
        {
            var secretKey = secretBundle.Title ?? secretBundle.Id;
            if (secretKey != null)
            {
                newLoadedSecrets.Add(secretKey, secretBundle);
            }

            _loadedSecrets = newLoadedSecrets;

            // Reload is needed if we are loading secrets that were not loaded before or
            // secret that was loaded previously is not available anymore
            if (loadedSecrets.Any() || oldLoadedSecrets?.Count > 0)
            {
                //
                Data = ConvertToEnvSecrets(newLoadedSecrets);
                if (oldLoadedSecrets != null)
                {
                    OnReload();
                }
            }
        }
    }

    private static Dictionary<string, string?> ConvertToEnvSecrets(Dictionary<string, FullItem> values) => values.ToDictionary(x => KeyToDotnetFormat(x.Key), x => x.Value.Fields?.FirstOrDefault(x=>x.Label == "dotnetSecret")?.Value);

    private static string KeyToDotnetFormat(string argKey) => argKey.Replace(".", ":").Replace(" ", ":");

    public virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!_disposed)
            {
                _cancellationToken.Cancel();
                _cancellationToken.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
