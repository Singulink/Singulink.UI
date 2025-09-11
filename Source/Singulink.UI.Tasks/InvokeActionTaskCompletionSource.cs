namespace Singulink.UI.Tasks;

internal sealed class InvokeActionTaskCompletionSource(Action action) : TaskCompletionSource, IInvokable
{
    public void Invoke()
    {
        try
        {
            action.Invoke();
            SetResult();
        }
        catch (Exception ex)
        {
            SetException(ex);
        }
    }
}

internal sealed class InvokeActionTaskCompletionSource<T>(T state, Action<T> action) : TaskCompletionSource, IInvokable
{
    public void Invoke()
    {
        try
        {
            action.Invoke(state);
            SetResult();
        }
        catch (Exception ex)
        {
            SetException(ex);
        }
    }
}
