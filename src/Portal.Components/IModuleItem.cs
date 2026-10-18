namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>描述一个页面模块实例的基础字段契约。</zh-CN>
    ///   <en>Describes the base field contract for a page module instance.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该接口由旧后台管理页和数据层共同使用，只表达模块实例的轻量元数据，不负责加载控件、执行授权或保存模块专属设置。</zh-CN>
    ///   <en>This interface is shared by legacy administration pages and the data layer. It expresses lightweight module-instance metadata only and does not load controls, enforce authorization, or persist module-specific settings.</en>
    /// </lang>
    /// </remarks>
    public interface IModuleItem
    {
        /// <summary>
        /// <l>
        ///   <zh-CN>模块在目标 pane 内的排序值；为空时由旧数据层或页面保存流程决定默认顺序。</zh-CN>
        ///   <en>Sort value of the module within its target pane; when null, the legacy data layer or page-save flow decides the default order.</en>
        /// </l>
        /// </summary>
        int? ModuleOrder { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>模块实例标题，通常展示在容器标题栏或后台模块列表中；多语言体系下它同时是**中文列**与**默认回退列**。</zh-CN>
        ///   <en>Module-instance title, typically shown in the container title bar or administration module lists; within the
        ///   multilingual scheme it is also the **Chinese column** and the **default fallback column**.</en>
        /// </l>
        /// </summary>
        string ModuleTitle { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>模块标题的英文列；为空时渲染回退到中文列。</zh-CN>
        ///   <en>English column for the module title; rendering falls back to the Chinese column when it is blank.</en>
        /// </l>
        /// </summary>
        string ModuleTitleEn { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>模块标题的其它语言列，取值为以文化名为键的 JSON 对象（如 `{"ja-JP":"休暇申請"}`）。
        ///   本契约只约定"是 JSON 文本"，键名是否为受支持的文化由解析器判定。</zh-CN>
        ///   <en>Other-languages column for the module title, holding a JSON object keyed by culture name (for example
        ///   `{"ja-JP":"休暇申請"}`). This contract only requires JSON text; whether a key is a supported culture is decided by
        ///   the resolver.</en>
        /// </l>
        /// </summary>
        string ModuleTitleLocalizedJson { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>模块所在的页面 pane 名称，用于把实例放回布局区域。</zh-CN>
        ///   <en>Name of the page pane containing the module, used to place the instance back into its layout region.</en>
        /// </l>
        /// </summary>
        string PaneName { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>模块实例主键；新增前通常为数据层约定的默认值。</zh-CN>
        ///   <en>Module-instance primary key; before insertion it usually carries the data-layer default value.</en>
        /// </l>
        /// </summary>
        int ModuleId { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>模块定义标识，指向受信任部署或旧定义表中的模块类型。</zh-CN>
        ///   <en>Module-definition identifier pointing to the module type in the trusted-deployment or legacy definition table.</en>
        /// </l>
        /// </summary>
        int? ModuleDefId { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>允许编辑该实例的角色串，沿用旧门户分号分隔格式。</zh-CN>
        ///   <en>Role string allowed to edit this instance, using the legacy portal semicolon-delimited format.</en>
        /// </l>
        /// </summary>
        string EditRoles { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>模块输出缓存秒数；为空时表示使用旧模块保存流程的默认策略。</zh-CN>
        ///   <en>Module output-cache duration in seconds; null means the legacy module-save flow keeps its default policy.</en>
        /// </l>
        /// </summary>
        int? CacheTimeout { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>旧移动端展示开关，当前仅保留为历史兼容字段。</zh-CN>
        ///   <en>Legacy mobile-display flag, currently retained only as a backward-compatibility field.</en>
        /// </l>
        /// </summary>
        bool? ShowMobile { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>模块所属页面标识，用于后台保存和实例列表查询。</zh-CN>
        ///   <en>Identifier of the page that owns the module, used by administration save flows and instance-list queries.</en>
        /// </l>
        /// </summary>
        int? TabId { get; set; }
    }
}
