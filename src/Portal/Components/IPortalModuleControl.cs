using System.Collections;
using System.ComponentModel;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 可由门户动态加载器初始化的桌面模块控件契约。
    /// Contract for a desktop module control initialized by the portal dynamic loader.
    /// </summary>
    /// <remarks>
    /// 实现此接口是动态模块加载的必要条件，但不授予任何访问或写入权限。页面和模块自身仍须在实际操作处
    /// 执行服务器端授权检查。
    /// Implementing this interface is required for dynamic module loading, but grants no access or write permission.
    /// Pages and modules must still enforce server-side authorization at the action being performed.
    /// </remarks>
    public interface IPortalModuleControl
    {
        /// <summary>
            /// 已绑定模块实例的标识；动态加载器设置 <see cref="ModuleConfiguration"/> 后才可读取。
            /// Identifier of the bound module instance; available after the dynamic loader sets <see cref="ModuleConfiguration"/>.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]// 设置属性不可浏览且不在设计器序列化中可见
        int ModuleId { get; }

        /// <summary>
            /// 当前门户的标识，由动态加载器在控件加入页面树前提供。
            /// Identifier of the current portal, supplied by the dynamic loader before the control joins the page tree.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        int PortalId { get; set; }

        /// <summary>
            /// 编辑入口的 UI 展示状态，不替代实际编辑动作的服务器端授权。
            /// UI display state for an edit entry point; it does not replace server-side authorization of an edit action.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        bool IsEditable { get; }

        /// <summary>
            /// 当前模块的运行时设置快照。
            /// Runtime settings snapshot for the current module.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        ModuleSettings ModuleConfiguration { get; set; }

        /// <summary>
            /// 模块实例的键值设置，由具体控件实现按需读取。
            /// Key-value settings for the module instance, loaded on demand by the concrete control.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        Hashtable Settings { get; }
    }
}
