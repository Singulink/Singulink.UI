namespace Singulink.UI.Tasks;

internal class InvokeActionTaskCompletionSource(Action action) : TaskCompletionSource, IInvokable
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

internal class InvokeActionTaskCompletionSource<T>(T state, Action<T> action) : TaskCompletionSource, IInvokable
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
