namespace Bielu.OnePassword.Connect.Aspnet.Secrets;

public class ParallelSecretLoader : IDisposable
{
    private const int ParallelismLevel = 32;
    private readonly OnePasswordConnectClient _client;
    private readonly SemaphoreSlim _semaphore;

    public ParallelSecretLoader(OnePasswordConnectClient client)
    {
        _client = client;
        _semaphore = new SemaphoreSlim(ParallelismLevel, ParallelismLevel);
    }


    public void Dispose()
    {
        _semaphore?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<IEnumerable<FullItem>> Load()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
            var vaults = await _client.Vaults.GetAsync(cancellationToken: cancellationToken);
            if (vaults == null)
            {
                throw new InvalidOperationException("No Vaults available");
            }
            var items = vaults.Select(x => _client.Vaults[x.Id].Items.GetAsync(cancellationToken: cancellationToken))
                .ToList();
            var itemsList = await Task.WhenAll(items);
            var itemsList2WithDetails = itemsList.SelectMany(x=>x.Select(y=>_client.Vaults[y.Vault!.Id].Items[y.Id].GetAsync(cancellationToken: cancellationToken)));
            IEnumerable<FullItem> itemsList2 = await Task.WhenAll(itemsList2WithDetails);

            return itemsList2;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
