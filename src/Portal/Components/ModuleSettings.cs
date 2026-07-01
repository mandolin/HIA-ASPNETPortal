using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   该类封装了门户中特定标签页的详细设置。ModuleSettings 实现了
    ///   IComparable 接口，以便可以通过 ModuleOrder 对 ModuleSettings 列表进行排序，
    ///   使用 List 的 Sort() 方法。
    /// 
    ///   Class that encapsulates the detailed settings for a specific Tab 
    ///   in the Portal. ModuleSettings implements 
    ///   the IComparable interface so that a List of ModuleItems may be sorted
    ///   by ModuleOrder, List's Sort() method.
    /// </summary>
    public class ModuleSettings : IComparable<ModuleSettings>
    {
        /// <summary>
        /// 初始化 <see cref="ModuleSettings"/> 实例.
        /// </summary>
        /// <param name="module">模块项</param>
        /// <param name="moduleDefConfig">模块定义的配置</param>
        public ModuleSettings(IModuleItem module, IModuleDefsDb moduleDefConfig)
        {
            ModuleTitle         = module.ModuleTitle;
            ModuleId            = module.ModuleId;            
            ModuleOrder         = module.ModuleOrder.Value;            
            PaneName            = module.PaneName;
            AuthorizedEditRoles = module.EditRoles;
            CacheTime           = module.CacheTimeout.Value;
            ShowMobile          = module.ShowMobile.Value;

            // 获取模块定义数据
            IModuleDefinitionItem moduleDefinitionItem = moduleDefConfig.GetSingleModuleDefinition(module.ModuleDefId.Value);

            DesktopSrc = moduleDefinitionItem.DesktopSourceFile;            
        }

        #region IComparable<ModuleItem> Members

        public int CompareTo(ModuleSettings value)
        {
            if (value == null)
            {
                return 1;
            }

            int compareOrder = value.ModuleOrder;

            if (ModuleOrder == compareOrder)
            {
                return 0;
            }
            if (ModuleOrder < compareOrder)
            {
                return -1;
            }
            if (ModuleOrder > compareOrder)
            {
                return 1;
            }
            return 0;
        }

        #endregion

        /// <summary>
        /// 模块显示顺序
        /// </summary>
        public int ModuleOrder { get; set; }

        /// <summary>
        /// 模块标题
        /// </summary>
        public string ModuleTitle { get; private set; }

        /// <summary>
        /// 窗格名称
        /// </summary>
        public string PaneName { get; private set; }

        /// <summary>
        /// 模块ID
        /// </summary>
        public int ModuleId { get; private set; }

        /// <summary>
        /// 授权编辑角色
        /// </summary>
        public string AuthorizedEditRoles { get; private set; }

        /// <summary>
        /// 缓存时间
        /// </summary>
        public int CacheTime { get; private set; }

        /// <summary>
        /// 是否在移动设备上显示
        /// </summary>
        public bool ShowMobile { get; private set; }

        /// <summary>
        /// 桌面版源文件路径
        /// </summary>
        public string DesktopSrc { get; private set; }
    }
}