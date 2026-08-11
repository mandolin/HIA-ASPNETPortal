namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>旧门户全局配置项契约。</zh-CN>
    ///     <en>Contract for a legacy portal global configuration item.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>该接口对应旧全局配置表的一行，用于站点设置控件读取和保存门户名称、编辑按钮显示策略。</zh-CN>
    ///     <en>This interface corresponds to one row in the legacy global configuration table and is used by the site-settings control to read and save the portal name and edit-button display policy.</en>
    ///   </lang>
    /// </remarks>
    public interface IGlobalItem
    {
        /// <summary>
        ///   <l>
        ///     <zh-CN>门户标识；旧站点通常使用单门户记录。</zh-CN>
        ///     <en>Portal identifier; the legacy site usually uses one portal row.</en>
        ///   </l>
        /// </summary>
        int PortalId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>展示在站点外壳中的门户名称。</zh-CN>
        ///     <en>Portal name displayed in the site shell.</en>
        ///   </l>
        /// </summary>
        string PortalName { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>是否始终显示模块编辑按钮；空值按旧数据兼容策略由调用方解释。</zh-CN>
        ///     <en>Whether module edit buttons are always shown; callers interpret null according to legacy-data compatibility policy.</en>
        ///   </l>
        /// </summary>
        bool? AlwaysShowEditButton { get; set; }
    }
}
