using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
        /// 当前 Tab 中一个模块实例的运行时设置快照。
        /// Runtime settings snapshot for one module instance on the current Tab.
    /// </summary>
    public class ModuleSettings : IComparable<ModuleSettings>
    {
        /// <summary>
        /// 从模块实例记录及其模块定义创建运行时快照。
        /// Creates a runtime snapshot from a module-instance record and its module definition.
        /// </summary>
        /// <param name="module">非 null 的模块实例记录；其可空数据库字段由现有数据层解包。
        /// Non-null module-instance record; its nullable database fields are unwrapped by the existing data layer.</param>
        /// <param name="moduleDefConfig">用于读取模块定义及原始桌面入口的数据服务。
        /// Data service used to read the module definition and its raw desktop entry.</param>
        /// <remarks>
        /// <see cref="DesktopSrc"/> 在此处仍是定义表中的原始值。页面动态加载前必须再经
        /// <see cref="PortalModuleCatalog"/> 和 <see cref="PortalModulePathValidator"/> 解析，不能将本对象
        /// 本身作为路径已验证的证明。
        /// <see cref="DesktopSrc"/> remains the raw definition-table value here. A page must resolve it through
        /// <see cref="PortalModuleCatalog"/> and <see cref="PortalModulePathValidator"/> before dynamic loading;
        /// this object alone does not prove that the path was validated.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// 中文：当模块记录缺少模块定义，或模块定义的严格查询无法得到唯一结果时抛出。
        /// 该情形属于部署或配置完整性错误，调用方不得将其静默降级为“跳过模块”。
        /// English: Thrown when the module record has no module definition or when its strict lookup does not return exactly one result.
        /// This is a deployment or configuration-integrity error and must not be silently downgraded to skipping the module.
        /// </exception>
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

        /// <summary>
        /// 按模块显示顺序进行升序比较。
        /// Compares module display order in ascending order.
        /// </summary>
        /// <param name="value">待比较设置；为 null 时当前实例排在其后。
        /// Settings to compare; when null, the current instance sorts after it.</param>
        /// <returns>小于零表示当前模块应先显示，零表示顺序相同，大于零表示当前模块应后显示。
        /// A value less than zero means this module displays first, zero means equal order, and a value greater than zero means it displays later.</returns>
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
        /// 模块在所属 Tab 内的显示顺序。
        /// Display order of the module within its owning Tab.
        /// </summary>
        public int ModuleOrder { get; set; }

        /// <summary>
        /// 管理员配置的模块显示标题。
        /// Module display title configured by an administrator.
        /// </summary>
        public string ModuleTitle { get; private set; }

        /// <summary>
        /// 页面布局中承载模块的窗格名称。
        /// Name of the layout pane that hosts the module.
        /// </summary>
        public string PaneName { get; private set; }

        /// <summary>
        /// 模块实例的数据库标识。
        /// Database identifier of the module instance.
        /// </summary>
        public int ModuleId { get; private set; }

        /// <summary>
        /// 控制编辑入口展示的历史角色字符串。
        /// Legacy role string controlling edit-entry display.
        /// </summary>
        public string AuthorizedEditRoles { get; private set; }

        /// <summary>
        /// 模块输出缓存秒数；零表示不使用 <see cref="CachedPortalModuleControl"/>。
        /// Module output-cache duration in seconds; zero bypasses <see cref="CachedPortalModuleControl"/>.
        /// </summary>
        public int CacheTime { get; private set; }

        /// <summary>
        /// 历史移动端显示标志；当前不代表新的移动端呈现策略。
        /// Legacy mobile-display flag; it does not represent a future mobile presentation strategy.
        /// </summary>
        public bool ShowMobile { get; private set; }

        /// <summary>
        /// 模块定义记录提供的原始桌面用户控件路径。
        /// Raw desktop user-control path supplied by the module-definition record.
        /// </summary>
        public string DesktopSrc { get; private set; }
    }
}
