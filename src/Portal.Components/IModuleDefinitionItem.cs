namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>旧模块类型定义项契约。</zh-CN>
    ///     <en>Contract for a legacy module type definition item.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该接口表示旧模块定义表中的一行。P3.2 后新模块来源应来自受信任部署包和模块目录，
    ///       但运行时仍需要读取旧定义以装配既有模块实例。
    ///     </zh-CN>
    ///     <en>
    ///       This interface represents one row in the legacy module-definition table. After P3.2, new module sources should come from
    ///       trusted deployed packages and the module catalog, while runtime assembly still reads legacy definitions for existing module instances.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public interface IModuleDefinitionItem
    {
        /// <summary>
        ///   <l zh-CN="面向管理员展示的模块类型名称。" en="Module type name displayed to administrators." />
        /// </summary>
        string FriendlyName { get; set; }

        /// <summary>
        ///   <l zh-CN="历史移动端控件虚拟路径；当前主要作为兼容字段保留。" en="Legacy mobile control virtual path, currently retained mainly as a compatibility field." />
        /// </summary>
        string MobileSourceFile { get; set; }

        /// <summary>
        ///   <l zh-CN="桌面端控件虚拟路径；新增或修改时必须先经过受信任部署路径校验。" en="Desktop control virtual path; additions or updates must pass trusted deployment path validation first." />
        /// </summary>
        string DesktopSourceFile { get; set; }

        /// <summary>
        ///   <l zh-CN="模块定义的旧数据库主键。" en="Legacy database primary key of the module definition." />
        /// </summary>
        int ModuleDefId { get; set; }
    }
}
