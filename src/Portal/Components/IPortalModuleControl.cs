using System.Collections;
using System.ComponentModel;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>定义门户动态加载器在桌面模块加入控件树前必须绑定的运行时上下文契约。</zh-CN>
    ///   <en>Defines the runtime-context contract that the portal dynamic loader must bind before a desktop module joins the control tree.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>实现此接口只是通过动态加载器接口门禁的必要条件，不证明入口、包、Tab、Portal 或用户权限。加载器负责提供 <see cref="ModuleConfiguration"/> 与 <see cref="PortalId"/>；模块仍须在每个读写动作处实施服务器端授权、输入验证和输出编码。</zh-CN>
    ///   <en>Implementing this interface is only a prerequisite for passing the dynamic loader's interface gate; it proves neither entry/package trust nor Tab, Portal, or user authorization. The loader supplies <see cref="ModuleConfiguration"/> and <see cref="PortalId"/>, while the module must still enforce server-side authorization, input validation, and output encoding at each read or write action.</en>
    /// </lang>
    /// </remarks>
    public interface IPortalModuleControl
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>获取已绑定模块配置中的模块实例标识。</zh-CN>
        ///   <en>Gets the module-instance identifier from the bound module configuration.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>动态加载器必须先设置 <see cref="ModuleConfiguration"/>；该标识只定位模块实例，不证明 Portal 归属或当前用户授权，契约也不规定未绑定时的安全默认值。</zh-CN>
        ///   <en>The dynamic loader must set <see cref="ModuleConfiguration"/> first; the identifier only locates a module instance, proves neither Portal ownership nor current-user authorization, and the contract defines no safe default before binding.</en>
        /// </lang>
        /// </remarks>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]// 设置属性不可浏览且不在设计器序列化中可见
        int ModuleId { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取或设置动态加载器为当前控件树绑定的 Portal 标识。</zh-CN>
        ///   <en>Gets or sets the Portal identifier bound by the dynamic loader for the current control tree.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该值传递运行上下文，不验证 Portal 存在性、模块归属、成员关系或权限；实现不得把赋值本身视为授权。</zh-CN>
        ///   <en>The value conveys runtime context and validates neither Portal existence, module ownership, membership, nor permission; implementations must not treat assignment itself as authorization.</en>
        /// </lang>
        /// </remarks>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        int PortalId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取编辑入口的 UI 展示提示。</zh-CN>
        ///   <en>Gets the UI-display hint for an edit entry point.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该布尔值只控制界面可见性，可能由全局显示选项或角色提示产生；它不替代保存、删除、管理或数据读取动作的服务器端授权。</zh-CN>
        ///   <en>This Boolean controls UI visibility only and may derive from a global display option or role hint; it does not replace server-side authorization for save, delete, administration, or data-read actions.</en>
        /// </lang>
        /// </remarks>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        bool IsEditable { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取或设置动态加载器绑定的当前模块运行时设置快照。</zh-CN>
        ///   <en>Gets or sets the current module runtime-settings snapshot bound by the dynamic loader.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>加载器应在模块依赖其它契约属性前提供非 null 值。该快照来自既有门户配置，不因接口赋值而被复制、持久化、重新解析或授权。</zh-CN>
        ///   <en>The loader should provide a non-null value before the module depends on other contract properties. The snapshot comes from existing portal configuration and is not copied, persisted, re-resolved, or authorized by interface assignment.</en>
        /// </lang>
        /// </remarks>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        ModuleSettings ModuleConfiguration { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取具体控件实现按需读取的模块实例键值设置。</zh-CN>
        ///   <en>Gets module-instance key-value settings loaded on demand by the concrete control implementation.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>返回的 <see cref="Hashtable"/> 是旧兼容数据结构，可能可变且由实现按控件实例缓存；接口不保证并发数据库更新可见、线程安全、值净化或调用方授权。</zh-CN>
        ///   <en>The returned <see cref="Hashtable"/> is a legacy compatibility structure that may be mutable and cached per control instance by the implementation; the interface guarantees neither visibility of concurrent database changes, thread safety, value sanitization, nor caller authorization.</en>
        /// </lang>
        /// </remarks>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        Hashtable Settings { get; }
    }
}
