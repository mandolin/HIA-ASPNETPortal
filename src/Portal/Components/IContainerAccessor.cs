using Microsoft.Practices.Unity;

namespace ASPNET.StarterKit.Portal
{
    public interface IContainerAccessor
    {
        IUnityContainer Container { get; }
    }
}