using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    public class Tab : TabSettings 
    {
        public int TabIndex { get; private set; }

        public Tab(int tabIndex, ITabItem tab)
            : base(tab)
        {
            TabIndex = tabIndex;            
        }

    }

    /// <summary>
    ///   Class that encapsulates the detailed settings for a specific Tab 
    ///   in the Portal. Implements the IComparable interface so that a List of TabItems may be sorted
    ///   by TabOrder, using the List's Sort() method.
    /// </summary>
    public class TabSettings : IComparable<TabSettings>
    {
        public readonly List<ModuleSettings> Modules = new List<ModuleSettings>();
        public int TabOrder { get; set; }
        public string TabName { get; private set; }
        public int TabId { get; private set; }
        public string AuthorizedRoles { get; private set; }
        public string MobileTabName { get; private set; }
        public bool ShowMobile { get; private set; }

        public TabSettings(ITabItem item)
        {
            TabOrder = item.TabOrder.Value;
            TabName = item.TabName;
            TabId = item.TabId;
            AuthorizedRoles = item.AccessRoles;
            MobileTabName = item.MobileTabName;
            ShowMobile = item.ShowMobile.Value;
        }

        #region IComparable<TabItem> Members

        public int CompareTo(TabSettings value)
        {
            if (value == null)
            {
                return 1;
            }

            int compareOrder = value.TabOrder;

            if (TabOrder == compareOrder)
            {
                return 0;
            }
            if (TabOrder < compareOrder)
            {
                return -1;
            }
            if (TabOrder > compareOrder)
            {
                return 1;
            }
            return 0;
        }

        #endregion
    }
}