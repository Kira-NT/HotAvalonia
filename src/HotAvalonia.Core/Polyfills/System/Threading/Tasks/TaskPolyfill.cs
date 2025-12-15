#if !NET5_0_OR_GREATER
namespace System.Threading.Tasks;

internal static class TaskPolyfill
{
    extension(Task task)
    {
        public async Task WithCancellation(CancellationToken cancellationToken)
        {
            // This is a terrible hack, but it's still slightly better than hanging indefinitely.
            TaskCompletionSource<bool> cancellationTaskSource = new();
            using CancellationTokenRegistration callbackReg = cancellationToken.Register(() => cancellationTaskSource.TrySetException(new OperationCanceledException()));
            Task result = await Task.WhenAny(task, cancellationTaskSource.Task).ConfigureAwait(false);
            _ = (result == task ? cancellationTaskSource.Task : task).ContinueWith(static x => { });
            await result;
        }
    }

    extension<T>(Task<T> task)
    {
        public async Task<T> WithCancellation(CancellationToken cancellationToken)
        {
            // This is a terrible hack, but it's still slightly better than hanging indefinitely.
            TaskCompletionSource<T> cancellationTaskSource = new();
            using CancellationTokenRegistration callbackReg = cancellationToken.Register(() => cancellationTaskSource.TrySetException(new OperationCanceledException()));
            Task<T> result = await Task.WhenAny(task, cancellationTaskSource.Task).ConfigureAwait(false);
            _ = (result == task ? cancellationTaskSource.Task : task).ContinueWith(static x => { });
            return await result;
        }
    }
}
#endif
