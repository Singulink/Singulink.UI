namespace Singulink.UI.Tasks;

internal class ActionTaskCompletionSource(Action action) : TaskCompletionSource, IInvokeAction
{
    private readonly Action _action = action;

    public void InvokeAction() => _action.Invoke();
}

internal class ActionTaskCompletionSource<T>(Action<T> action, T state) : TaskCompletionSource, IInvokeAction
{
    private readonly Action<T> _action = action;
    private readonly T _state = state;

    public void InvokeAction() => _action.Invoke(_state);
}
