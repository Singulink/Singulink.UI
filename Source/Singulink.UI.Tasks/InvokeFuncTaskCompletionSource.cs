namespace Singulink.UI.Tasks;

internal class InvokeFuncTaskCompletionSource<TResult>(Func<TResult> func) : TaskCompletionSource<TResult>, IInvokable
{
    public void Invoke()
    {
        try
        {
            SetResult(func.Invoke());
        }
        catch (Exception ex)
        {
            SetException(ex);
        }
    }
}

internal class InvokeFuncTaskCompletionSource<T, TResult>(T state, Func<T, TResult> func) : TaskCompletionSource<TResult>, IInvokable
{
    public void Invoke()
    {
        try
        {
            SetResult(func.Invoke(state));
        }
        catch (Exception ex)
        {
            SetException(ex);
        }
    }
}
