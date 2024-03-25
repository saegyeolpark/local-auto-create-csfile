using System.Threading;
using Cysharp.Threading.Tasks;

namespace Glider.Util
{
    public static class CtsHelper
    {
        public static void CancelAndDispose(this CancellationTokenSource cts)
        {
            if (cts == null || cts.IsCancellationRequested)
                return;

            cts.Cancel();
            DisposeAfterOneFrame(cts).Forget();
        }
        private static async UniTask DisposeAfterOneFrame(CancellationTokenSource cts)
        {
            await UniTask.NextFrame();
            cts.Dispose();
        }
    }
}