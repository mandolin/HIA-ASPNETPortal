using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /*
     * 本文件代码定义了一个 Tab 类和一个 TabSettings 类，它们都与管理门户应用中的标签页有关。TabSettings 类实现了 IComparable<TabSettings> 接口以允许根据 TabOrder 对标签页进行排序。
     */

    /// <summary>
    /// 表示门户中的一个标签页
    /// </summary>
    /// <seealso cref="ASPNET.StarterKit.Portal.TabSettings" />
    public class Tab : TabSettings 
    {
        /// <summary>
        /// 该标签页的索引
        /// </summary>
        public int TabIndex { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tab"/> class.
        /// 构造函数用于创建一个新的 <see cref="Tab"/> 实例。
        /// </summary>
        /// <param name="tabIndex">Index of the tab.标签页的索引</param>
        /// <param name="tab">The tab.标签页项</param>
        public Tab(int tabIndex, ITabItem tab)
            : base(tab)// 调用基类构造函数并传入 ITabItem。
        {
            TabIndex = tabIndex;
        }

    }

    /// <summary>
    ///   Class that encapsulates the detailed settings for a specific Tab 
    ///   in the Portal. Implements the IComparable interface so that a List of TabItems may be sorted
    ///   by TabOrder, using the List's Sort() method.
    ///   类封装了门户中特定标签页的详细设置。实现了 IComparable 接口以便可以通过 List 的 Sort() 方法对 TabItems 列表按 TabOrder 进行排序。
    /// </summary>
    public class TabSettings : IComparable<TabSettings>
    {
        /// <summary>
        /// 与该标签页关联的 ModuleSettings 列表。
        /// </summary>
        public readonly List<ModuleSettings> Modules = new List<ModuleSettings>();

        /// <summary>
        /// 标签页应该出现的顺序。
        /// </summary>
        public int TabOrder { get; set; }

        /// <summary>
        /// 标签页的名称。
        /// </summary>
        public string TabName { get; private set; }

        /// <summary>
        /// 标签页的唯一标识符。
        /// </summary>
        public int TabId { get; private set; }

        /// <summary>
        /// 可以查看此标签页的角色。
        /// </summary>
        public string AuthorizedRoles { get; private set; }

        /// <summary>
        /// 当在移动设备上显示时的标签页名称。
        /// </summary>
        public string MobileTabName { get; private set; }

        /// <summary>
        /// 表示该标签页是否应在移动设备上显示。
        /// </summary>
        public bool ShowMobile { get; private set; }

        /// <summary>
        /// 构造函数用于根据 ITabItem 初始化新的 TabSettings 实例。
        /// </summary>
        /// <param name="item">标签页项</param>
        public TabSettings(ITabItem item)
        {
            AuthorizedRoles = item.AccessRoles;
            MobileTabName   = item.MobileTabName;
            ShowMobile      = item.ShowMobile.Value;
            TabId           = item.TabId;
            TabName         = item.TabName;
            TabOrder        = item.TabOrder.Value;
        }

        #region IComparable<TabSettings> 成员

        // 根据 IComparable<TabSettings> 要求的方法来比较两个 TabSettings 实例。
        public int CompareTo(TabSettings other)
        {
            if (other == null)
            {
                return 1; // 如果另一个对象为空，则认为当前对象更大。
            }

            // 比较两个对象的 TabOrder 属性。
            if (TabOrder == other.TabOrder)
            {
                return 0; // 如果它们具有相同的 TabOrder，则认为相等。
            }
            else if (TabOrder < other.TabOrder)
            {
                return -1; // 当前对象具有较低的 TabOrder。
            }
            else
            {
                return 1; // 当前对象具有较高的 TabOrder。
            }
        }

        #endregion
    }
}