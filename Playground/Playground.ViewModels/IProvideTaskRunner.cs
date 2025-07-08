using Singulink.UI.Tasks;

namespace Playground.ViewModels;

public interface IProvideTaskRunner
{
    public ITaskRunner TaskRunner { get; set; }
}
