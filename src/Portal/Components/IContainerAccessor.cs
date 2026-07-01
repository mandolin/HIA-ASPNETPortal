using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 定义了一个接口，用于提供对 Unity 容器的访问。
    /// </summary>
    public interface IContainerAccessor
    {
        /// <summary>
        /// 获取 Unity 容器实例。
        /// </summary>
        IUnityContainer Container { get; }
    }
}