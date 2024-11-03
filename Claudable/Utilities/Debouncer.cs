namespace Claudable.Utilities;

public class Debouncer : IDisposable
{
    private readonly Action _action;
    private readonly int _milliseconds;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly object _lockObject = new object();

    public Debouncer(Action action, int milliseconds = 1000)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _milliseconds = milliseconds;
    }

    public void Debounce()
    {
        lock (_lockObject)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            var token = _cancellationTokenSource.Token;
            Task.Delay(_milliseconds, token).ContinueWith(task =>
            {
                if (!task.IsCanceled)
                {
                    _action();
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}