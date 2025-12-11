using System.Collections;
using System.ComponentModel;

namespace ASPNET.StarterKit.Portal
{
    public interface IPortalModuleControl
    {
        /// <summary>
        /// 模块Id
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]// 设置属性不可浏览且不在设计器序列化中可见
        int ModuleId { get; }

        /// <summary>
        /// 获取或设置门户ID
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        int PortalId { get; set; }

        /// <summary>
        /// 是否可编辑的状态
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        bool IsEditable { get; }

        /// <summary>
        /// 模块配置
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        ModuleSettings ModuleConfiguration { get; set; }

        /// <summary>
        /// 模块设置
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        Hashtable Settings { get; }
    }
}