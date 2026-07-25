using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>表示导航列表中的一个门户 Tab。</zh-CN>
    ///   <en>Represents one portal tab in navigation lists.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该类型在继承 Tab 设置快照的同时额外保存当前列表索引，供旧 WebForms 导航控件绑定使用。</zh-CN>
    ///   <en>This type extends the tab settings snapshot with the current list index for legacy WebForms navigation binding.</en>
    /// </lang>
    /// </remarks>
    public class Tab : TabSettings
    {
        /// <summary>
        /// <l>
        ///   <zh-CN>该 Tab 在当前导航集合中的索引。</zh-CN>
        ///   <en>Index of this tab in the current navigation collection.</en>
        /// </l>
        /// </summary>
        public int TabIndex { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>基于数据访问层 Tab 项创建可绑定导航 Tab。</zh-CN>
        ///   <en>Creates a bindable navigation tab from a data-access tab item.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabIndex">
        /// <l zh-CN="当前导航集合中的索引。" en="Index in the current navigation collection." />
        /// </param>
        /// <param name="tab">
        /// <l zh-CN="旧门户 Tab 数据项。" en="Legacy portal tab data item." />
        /// </param>
        public Tab(int tabIndex, ITabItem tab)
            : base(tab)
        {
            TabIndex = tabIndex;
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>封装门户中一个 Tab 的运行期设置快照。</zh-CN>
    ///   <en>Encapsulates the runtime settings snapshot for one portal tab.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该对象由 `ITabItem` 投影而来，并实现排序接口，使旧代码可直接按 `TabOrder` 对 Tab 集合排序。</zh-CN>
    ///   <en>This object is projected from `ITabItem` and implements comparison so legacy code can sort tab collections by `TabOrder`.</en>
    /// </lang>
    /// </remarks>
    public class TabSettings : IComparable<TabSettings>
    {
        /// <summary>
        /// <l>
        ///   <zh-CN>与该 Tab 关联的模块设置集合。</zh-CN>
        ///   <en>Module settings associated with this tab.</en>
        /// </l>
        /// </summary>
        public readonly List<ModuleSettings> Modules = new List<ModuleSettings>();

        /// <summary>
        /// <l>
        ///   <zh-CN>Tab 在同级导航中的排序值。</zh-CN>
        ///   <en>Sort value of the tab within sibling navigation items.</en>
        /// </l>
        /// </summary>
        public int TabOrder { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>桌面端显示的 Tab 名称。</zh-CN>
        ///   <en>Tab name displayed on desktop navigation.</en>
        /// </l>
        /// </summary>
        public string TabName { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>旧门户 Tab 主键。</zh-CN>
        ///   <en>Legacy portal tab primary key.</en>
        /// </l>
        /// </summary>
        public int TabId { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>允许访问该 Tab 的旧分号角色串。</zh-CN>
        ///   <en>Legacy semicolon-delimited role string allowed to access this tab.</en>
        /// </l>
        /// </summary>
        public string AuthorizedRoles { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>历史移动端显示名称；当前保留为旧数据兼容字段。</zh-CN>
        ///   <en>Historical mobile display name; currently retained as a legacy-data compatibility field.</en>
        /// </l>
        /// </summary>
        public string MobileTabName { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>历史移动端可见标志；当前不驱动新的移动端展示方案。</zh-CN>
        ///   <en>Historical mobile visibility flag; it does not drive the new mobile presentation approach.</en>
        /// </l>
        /// </summary>
        public bool ShowMobile { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从旧门户 Tab 数据项创建运行期设置快照。</zh-CN>
        ///   <en>Creates a runtime settings snapshot from a legacy portal tab data item.</en>
        /// </lang>
        /// </summary>
        /// <param name="item">
        /// <l zh-CN="旧门户 Tab 数据项。" en="Legacy portal tab data item." />
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>调用方仍需保证 `item` 来自可信数据访问层；本构造函数只做字段投影，不额外执行授权判断。</zh-CN>
        ///   <en>The caller must still ensure `item` comes from a trusted data-access layer; this constructor only projects fields and does not perform authorization checks.</en>
        /// </lang>
        /// </remarks>
        public TabSettings(ITabItem item)
        {
            AuthorizedRoles = item.AccessRoles;
            MobileTabName   = item.MobileTabName;
            ShowMobile      = item.ShowMobile.Value;
            TabId           = item.TabId;
            TabName         = item.TabName;
            TabOrder        = item.TabOrder.Value;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按 `TabOrder` 比较两个 Tab 设置快照。</zh-CN>
        ///   <en>Compares two tab settings snapshots by `TabOrder`.</en>
        /// </lang>
        /// </summary>
        /// <param name="other">
        /// <l zh-CN="另一个 Tab 设置快照；为空时当前对象排在其后。" en="Another tab settings snapshot; when null, the current object sorts after it." />
        /// </param>
        /// <returns>
        /// <l zh-CN="与 `IComparable` 契约一致的排序比较结果。" en="Sort comparison result following the `IComparable` contract." />
        /// </returns>
        public int CompareTo(TabSettings other)
        {
            if (other == null)
            {
                return 1;
            }

            // <lang>
            //   <zh-CN>旧门户只按 `TabOrder` 排序；同序值保持相等，由调用侧集合稳定性决定最终显示顺序。</zh-CN>
            //   <en>The legacy portal sorts only by `TabOrder`; equal values remain equal and the caller collection decides final display order stability.</en>
            // </lang>
            if (TabOrder == other.TabOrder)
            {
                return 0;
            }

            if (TabOrder < other.TabOrder)
            {
                return -1;
            }

            return 1;
        }
    }
}
