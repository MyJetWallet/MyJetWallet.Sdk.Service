using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyJetWallet.Sdk.Service.Tools
{
    public class MyLocker
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1,1);

        public async Task<IDisposable> GetLocker()
        {
            await _semaphore.WaitAsync();
            return new SemaphoreRelease(_semaphore);
        }
        
        private class SemaphoreRelease : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;
            public SemaphoreRelease(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore?.Release();
            }
        }
    }
}