using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>定义向门户运行时公开 Unity 容器访问的组合契约。</zh-CN>
    ///   <en>Defines the composition contract that exposes Unity-container access to portal runtime components.</en>
    /// </lang>
    /// </summary>
    public interface IContainerAccessor
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>获取当前组合根提供的 Unity 容器。</zh-CN>
        ///   <en>Gets the Unity container supplied by the current composition root.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该属性只暴露依赖解析入口，不代表调用方可以绕过页面授权、生命周期或注册契约直接创建任意服务。</zh-CN>
        ///   <en>This property exposes only the dependency-resolution entry point; callers must not treat it as permission to bypass page authorization, lifecycle rules, or registration contracts.</en>
        /// </lang>
        /// </remarks>
        IUnityContainer Container { get; }
    }
}
