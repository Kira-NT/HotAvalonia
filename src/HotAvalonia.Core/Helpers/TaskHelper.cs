namespace HotAvalonia.Helpers;

/// <summary>
/// Provides utility methods for interacting with tasks.
/// </summary>
internal static class TaskHelper
{
    /// <inheritdoc cref="WithCancellation{T}(Task{T}, CancellationToken)"/>
    public static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
    {
        // This is a terrible hack, but it's still slightly better than hanging indefinitely.
        TaskCompletionSource<bool> cancellationTaskSource = new();
        using CancellationTokenRegistration callbackReg = cancellationToken.Register(() => cancellationTaskSource.TrySetException(new OperationCanceledException()));
        Task result = await Task.WhenAny(task, cancellationTaskSource.Task).ConfigureAwait(false);
        _ = (result == task ? cancellationTaskSource.Task : task).ContinueWith(static x => { });
        await result;
    }

    /// <summary>
    /// Configures the specified <paramref name="task"/> to support cancellation.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the task.</typeparam>
    /// <param name="task">The task to configure.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The configured task.</returns>
    public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        // This is a terrible hack, but it's still slightly better than hanging indefinitely.
        TaskCompletionSource<T> cancellationTaskSource = new();
        using CancellationTokenRegistration callbackReg = cancellationToken.Register(() => cancellationTaskSource.TrySetException(new OperationCanceledException()));
        Task<T> result = await Task.WhenAny(task, cancellationTaskSource.Task).ConfigureAwait(false);
        _ = (result == task ? cancellationTaskSource.Task : task).ContinueWith(static x => { });
        return await result;
    }
}
