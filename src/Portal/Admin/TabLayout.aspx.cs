using System;
using System.Collections.Generic;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class TabLayout : PortalPage<TabLayout>
    {
        protected List<ModuleSettings> contentList;
        protected List<ModuleSettings> leftList;
        protected List<ModuleSettings> rightList;
        private int tabId;

        private const string LeftPaneName = "LeftPane";
        private const string ContentPaneName = "ContentPane";
        private const string RightPaneName = "RightPane";

        [Dependency]
        public IRolesDb RolesDB { private get; set; }

        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        [Dependency]
        public ITabsDb TabsConfig { private get; set; }

        [Dependency]
        public IModuleDefsDb ModuleDefConfig { private get; set; }

        [Dependency]
        public IGlobalsDb PortalConfig { private get; set; }


        //*******************************************************
        //
        // The Page_Load server event handler on this page is used
        // to populate a tab's layout settings on the page
        //
        //*******************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            // Verify that the current user has access to access this page
            if (PortalSecurity.IsInRoles("Admins") == false)
            {
                Response.Redirect("~/Admin/EditAccessDenied.aspx");
            }

            // Determine Tab to Edit
            if (Request.Params["tabid"] != null)
            {
                tabId = Int32.Parse(Request.Params["tabid"]);
            }

            // If first visit to the page, update all entries
            if (Page.IsPostBack == false)
            {
                BindData();
            }
        }

        //*******************************************************
        //
        // The AddModuleToPane_Click server event handler on this page is used
        // to add a new portal module into the tab
        //
        //*******************************************************

        protected void AddModuleToPane_Click(Object sender, EventArgs e)
        {
            // 新增的模块都将被添加到ContentPane的末尾
            // 将新的模块信息保存到数据库中
            ModulesConfig.AddModule(
                tabId,                  // 当前标签的ID
                999,                    // 最高的顺序号，表示新模块将被添加到最后
                "ContentPane",           // 目标窗格的名称
                moduleTitle.Text,       // 模块标题，从输入框获取
                Int32.Parse(moduleType.SelectedItem.Value), // 模块类型ID，从下拉菜单中选定的项获取
                0,                      // 默认缓存设置为0
                "Admins",               // 默认授权给管理员角色
                false                   // 默认showmobile配置为false
            );

            // 从当前上下文获取PortalSettings
            var portalSettings = (PortalSettings)Context.Items["PortalSettings"];

            // 从数据库中重新加载PortalSettings
            // 使用新的PortalSettings实例替换当前上下文中的PortalSettings
            HttpContext.Current.Items["PortalSettings"] = new PortalSettings(
                portalSettings.PortalId, // 门户ID
                tabId,                   // 当前标签ID
                PortalConfig,            // 门户配置对象
                TabsConfig,              // 标签配置对象
                ModulesConfig,           // 模块配置对象
                ModuleDefConfig          // 模块定义配置对象
            );

            // 获取ContentPane内的所有模块
            List<ModuleSettings> modules = GetModules("ContentPane");

            // 对ContentPane内的模块重新排序
            OrderModules(modules);

            // 重新保存模块顺序到数据库
            foreach (ModuleSettings item in modules)
            {
                ModulesConfig.UpdateModuleOrder(item.ModuleId, item.ModuleOrder, "ContentPane");
            }

            // 重新定向到同一页面以获取更改
            // 这样可以刷新页面并显示新的模块
            Response.Redirect(Request.RawUrl);
        }

        protected void UpDown_Click(Object sender, ImageClickEventArgs e)
        {
            // Get the command name and pane argument from the image button's properties
            // 从按钮的属性中获取命令名称和窗格参数。
            string cmd = ((ImageButton)sender).CommandName;
            string pane = NormalizePaneName(((ImageButton)sender).CommandArgument);

            // Find the ListBox control corresponding to the specified pane
            // 查找对应于指定窗格的 ListBox 控件。
            var _listbox = GetPaneListBox(pane);

            // Get the list of modules for the specified pane
            // 获取指定窗格内的模块列表。
            List<ModuleSettings> modules = GetModules(pane);

            // If a module is selected in the ListBox
            if (_listbox.SelectedIndex != -1)
            {
                int delta;
                int selection = -1;

                // Determine whether to move the module up or down and calculate the delta accordingly
                // 根据命令确定模块是向上还是向下移动，并相应地计算delta值。
                if (cmd == "down")
                {
                    delta = 3;
                    if (_listbox.SelectedIndex < _listbox.Items.Count - 1)
                    {
                        selection = _listbox.SelectedIndex + 1;
                    }
                }
                else
                {
                    delta = -3;
                    if (_listbox.SelectedIndex > 0)
                    {
                        selection = _listbox.SelectedIndex - 1;
                    }
                }

                // Get the selected module and adjust its order
                // 获取选中的模块并调整其顺序。
                ModuleSettings m = modules[_listbox.SelectedIndex];
                m.ModuleOrder += delta;

                // Reorder the modules in the content pane
                // 重新排列窗格内的模块顺序。
                OrderModules(modules);

                // Resave the order of the modules to the database
                // 将模块的新顺序保存回数据库。
                foreach (ModuleSettings item in modules)
                {
                    ModulesConfig.UpdateModuleOrder(item.ModuleId, item.ModuleOrder, pane);
                }
            }

            // Redirect to the same page to pick up changes
            // 重新定向到同一页面以获取更改。
            Response.Redirect(Request.RawUrl);
        }

        //*******************************************************
        //
        // The RightLeft_Click server event handler on this page is
        // used to move a portal module between layout panes on
        // the tab page
        //
        //*******************************************************

        protected void RightLeft_Click(Object sender, ImageClickEventArgs e)
        {
            // Get the source and target pane names from the button attributes
            // 从按钮属性中获取源窗格和目标窗格的名称。
            string sourcePane = NormalizePaneName(((ImageButton)sender).Attributes["sourcepane"]);
            string targetPane = NormalizePaneName(((ImageButton)sender).Attributes["targetpane"]);

            // Find the ListBox controls corresponding to the source and target panes
            // 查找对应于源窗格和目标窗格的 ListBox 控件。
            var sourceBox = GetPaneListBox(sourcePane);
            var targetBox = GetPaneListBox(targetPane);

            if (sourceBox.SelectedIndex != -1)
            {
                // Get the list of modules for the source pane
                // 获取源窗格内的模块列表。
                List<ModuleSettings> sourceList = GetModules(sourcePane);

                // Get a reference to the module to move and set a high order number to place it at the end of the target list
                // 获取要移动的模块引用，并设置一个高的顺序号以将其放置在目标列表的末尾。
                ModuleSettings m = sourceList[sourceBox.SelectedIndex];

                // Update the module order in the database to reflect the move
                // 在数据库中更新模块顺序以反映移动。
                ModulesConfig.UpdateModuleOrder(m.ModuleId, 998, targetPane);

                // Remove the module from the source list
                // 从源列表中移除模块。
                sourceList.RemoveAt(sourceBox.SelectedIndex);

                // Reload the portal settings from the database
                // 从数据库中重新加载门户设置。
                var portalSettings = (PortalSettings)Context.Items["PortalSettings"];
                HttpContext.Current.Items["PortalSettings"] = new PortalSettings(portalSettings.PortalId, tabId, PortalConfig, TabsConfig, ModulesConfig, ModuleDefConfig);

                // Reorder the modules in the source pane
                // 重新排列源窗格内的模块顺序。
                sourceList = GetModules(sourcePane);
                OrderModules(sourceList);

                // Resave the order of the modules in the source pane to the database
                // 将源窗格内模块的新顺序保存回数据库。
                foreach (ModuleSettings item in sourceList)
                {
                    ModulesConfig.UpdateModuleOrder(item.ModuleId, item.ModuleOrder, sourcePane);
                }

                // Reorder the modules in the target pane
                // 重新排列目标窗格内的模块顺序。
                List<ModuleSettings> targetList = GetModules(targetPane);
                OrderModules(targetList);

                // Resave the order of the modules in the target pane to the database
                // 将目标窗格内模块的新顺序保存回数据库。
                foreach (ModuleSettings item in targetList)
                {
                    ModulesConfig.UpdateModuleOrder(item.ModuleId, item.ModuleOrder, targetPane);
                }

                // Redirect to the same page to pick up changes
                // 重新定向到同一页面以获取更改。
                Response.Redirect(Request.RawUrl);
            }
        }

        //*******************************************************
        //
        // The Apply_Click server event handler on this page is
        // used to save the current tab settings to the database and 
        // then redirect back to the main admin page.
        //
        //*******************************************************

        protected void Apply_Click(Object Sender, EventArgs e)
        {
            // Save changes then navigate back to admin.
            // 保存更改后导航回管理页面。
            string id = ((LinkButton)Sender).ID;

            SaveTabData();

            // Obtain PortalSettings from Current Context
            // 从当前上下文获取门户设置。
            var portalSettings = (PortalSettings)Context.Items["PortalSettings"];
            int adminIndex = portalSettings.DesktopTabs.Count - 1;

            // Redirect back to the admin page
            // 重新定向回管理页面。
            Response.Redirect("~/DesktopDefault.aspx?tabindex=" + adminIndex + "&tabid=" +
                              (portalSettings.DesktopTabs[adminIndex]).TabId);
        }

        //*******************************************************
        //
        // The TabSettings_Change server event handler on this page is
        // invoked any time the tab name or access security settings
        // change.  The event handler in turn calls the "SaveTabData"
        // helper method to ensure that these changes are persisted
        // to the portal configuration file.
        //
        //*******************************************************

        protected void TabSettings_Change(Object sender, EventArgs e)
        {
            // Ensure that settings are saved
            SaveTabData();
        }

        //*******************************************************
        //
        // The SaveTabData helper method is used to persist the
        // current tab settings to the database.
        //
        //*******************************************************

        private void SaveTabData()
        {
            // Construct Authorized User Roles String
            // 构建授权用户角色字符串。
            string authorizedRoles = "";

            // Iterate through each item in the authRoles CheckBoxList
            // 遍历 authRoles 复选框列表中的每一项。
            foreach (ListItem item in authRoles.Items)
            {
                if (item.Selected)
                {
                    authorizedRoles = authorizedRoles + item.Text + ";"; // 如果该项被选中，则将其添加到授权角色字符串中。
                }
            }

            // Obtain PortalSettings from Current Context
            // 从当前上下文获取门户设置。
            var portalSettings = (PortalSettings)Context.Items["PortalSettings"];

            // Update Tab info in the database
            // 更新数据库中的标签信息。
            TabsConfig.UpdateTab(
                portalSettings.PortalId, // 门户ID
                tabId,                   // 标签ID
                tabName.Text,            // 标签名
                portalSettings.ActiveTab.TabOrder, // 标签顺序
                authorizedRoles,         // 授权角色字符串
                mobileTabName.Text,      // 移动设备上的标签名
                showMobile.Checked       // 是否显示在移动设备上
            );
        }

        //*******************************************************
        //
        // The EditBtn_Click server event handler on this page is
        // used to edit an individual portal module's settings
        //
        //*******************************************************

        protected void EditBtn_Click(Object sender, ImageClickEventArgs e)
        {
            // Get the pane name from the button's CommandArgument property
            // 从按钮的 CommandArgument 属性获取窗格名称。
            string pane = NormalizePaneName(((ImageButton)sender).CommandArgument);

            // Find the ListBox control corresponding to the specified pane
            // 查找对应于指定窗格的 ListBox 控件。
            var _listbox = GetPaneListBox(pane);

            if (_listbox.SelectedIndex != -1)
            {
                // Parse the value of the selected item to get the module ID
                // 解析选中项的值以获取模块ID。
                int mid = Int32.Parse(_listbox.SelectedItem.Value);

                // Redirect to module settings page
                // 重新定向到模块设置页面。
                Response.Redirect("ModuleSettings.aspx?mid=" + mid + "&tabid=" + tabId);
            }
        }

        //*******************************************************
        //
        // The DeleteBtn_Click server event handler on this page is
        // used to delete an portal module from the page
        //
        //*******************************************************

        protected void DeleteBtn_Click(Object sender, ImageClickEventArgs e)
        {
            // Get the pane name from the button's CommandArgument property
            // 从按钮的 CommandArgument 属性获取窗格名称。
            string pane = NormalizePaneName(((ImageButton)sender).CommandArgument);

            // Find the ListBox control corresponding to the specified pane
            // 查找对应于指定窗格的 ListBox 控件。
            var _listbox = GetPaneListBox(pane);

            // Get the list of modules for the specified pane
            // 获取指定窗格内的模块列表。
            List<ModuleSettings> modules = GetModules(pane);

            if (_listbox.SelectedIndex != -1)
            {
                // Get the selected module settings
                // 获取选中的模块设置。
                ModuleSettings m = modules[_listbox.SelectedIndex];

                // Check if the module has a valid ID before attempting to delete
                // 在尝试删除之前检查模块是否有有效的ID。
                if (m.ModuleId > -1)
                {
                    // Must also delete the module from the database
                    // 必须从数据库中删除模块。
                    ModulesConfig.DeleteModule(m.ModuleId);
                }
            }

            // 重新排列窗格内的模块顺序。
            modules = GetModules(pane);
            OrderModules(modules);

            // Resave the order of the modules in the target pane to the database
            // 将窗格内模块的新顺序保存回数据库。
            foreach (ModuleSettings item in modules)
            {
                ModulesConfig.UpdateModuleOrder(item.ModuleId, item.ModuleOrder, pane);
            }

            // Redirect to the same page to pick up changes
            // 重新定向到同一页面以获取更改。
            Response.Redirect(Request.RawUrl);
        }


        //*******************************************************
        //
        // The BindData helper method is used to update the tab's
        // layout panes with the current configuration information
        //
        //*******************************************************

        private void BindData()
        {
            // Obtain PortalSettings from Current Context
            // 从当前上下文获取门户设置对象。
            var portalSettings = (PortalSettings)Context.Items["PortalSettings"];
            TabSettings tab = portalSettings.ActiveTab;

            // Populate Tab Names, etc.
            // 填充标签名等信息。
            tabName.Text = tab.TabName; // 设置文本框中标签的名字。
            mobileTabName.Text = tab.MobileTabName; // 设置文本框中移动设备上标签的名字。
            showMobile.Checked = tab.ShowMobile; // 设置复选框以指示标签是否对移动用户可见。

            // Populate checkbox list with all security roles for this portal
            // and "check" the ones already configured for this tab
            // 填充复选框列表以包含此门户的所有安全角色，并且选中那些已经为此标签配置的角色。
            IEnumerable<IRoleItem> roles = RolesDB.GetPortalRoles(portalSettings.PortalId); // 获取门户的所有角色。

            // Clear existing items in checkboxlist
            // 清除复选框列表中的现有项。
            authRoles.Items.Clear();

            // Add 'All Users' option to the checkbox list
            // 向复选框列表添加“所有用户”选项。
            var allItem = new ListItem(); // 创建一个新的列表项。
            allItem.Text = "All Users"; // 设置列表项的文本为“所有用户”。

            // Check if 'All Users' is authorized for this tab
            // 检查“所有用户”是否对此标签已授权。
            if (tab.AuthorizedRoles.LastIndexOf("All Users") > -1)
            {
                allItem.Selected = true; // 如果已授权，则选中该选项。
            }

            authRoles.Items.Add(allItem); // 将“所有用户”选项添加到复选框列表中。

            // Loop through each role and add it to the checkbox list
            // 遍历每个角色并将其添加到复选框列表中。
            foreach (IRoleItem role in roles)
            {
                var item = new ListItem(); // 创建一个新的列表项。
                item.Text = role.RoleName; // 设置列表项的文本为角色名。
                item.Value = role.RoleId.ToString(); // 设置列表项的值为角色ID。

                // Check if the current role is authorized for this tab
                // 检查当前角色是否对此标签已授权。
                if ((tab.AuthorizedRoles.LastIndexOf(item.Text)) > -1)
                {
                    item.Selected = true; // 如果已授权，则选中该选项。
                }

                authRoles.Items.Add(item); // 将角色添加到复选框列表中。
            }

            // Populate the "Add Module" Data
            // 填充“添加模块”的数据。
            moduleType.DataSource = ModuleDefConfig.GetModuleDefinitions(); // 设置下拉列表的数据源为模块定义。
            moduleType.DataBind(); // 绑定数据源到下拉列表。

            // Populate Right Hand Module Data
            // 填充右侧模块数据。
            rightList = GetModules("RightPane"); // 获取右侧模块列表。
            rightPane.DataBind(); // 绑定数据到右侧模块列表框。

            // Populate Content Pane Module Data
            // 填充内容面板模块数据。
            contentList = GetModules("ContentPane"); // 获取内容面板模块列表。
            contentPane.DataBind(); // 绑定数据到内容面板模块列表框。

            // Populate Left Hand Pane Module Data
            // 填充左侧模块数据。
            leftList = GetModules("LeftPane"); // 获取左侧模块列表。
            leftPane.DataBind(); // 绑定数据到左侧模块列表框。
        }
        //*******************************************************
        //
        // The GetModules helper method is used to get the modules
        // for a single pane within the tab
        //
        //*******************************************************

        private List<ModuleSettings> GetModules(String pane)
        {
            // Obtain PortalSettings from Current Context
            // 从当前上下文获取门户设置对象。
            var portalSettings = (PortalSettings)Context.Items["PortalSettings"];
            var paneModules = new List<ModuleSettings>(); // 创建一个新的模块设置列表。

            // Iterate over all modules of the active tab
            // 遍历活动标签下的所有模块。
            foreach (ModuleSettings module in portalSettings.ActiveTab.Modules)
            {
                // Check if the module belongs to the specified pane
                // 检查模块是否属于指定的窗格。
                if ((module.PaneName).ToLower() == pane.ToLower())
                {
                    paneModules.Add(module); // 如果属于指定窗格，则将模块添加到列表中。
                }
            }

            return paneModules; // 返回包含指定窗格内所有模块的列表。
        }

        private ListBox GetPaneListBox(string pane)
        {
            switch (NormalizePaneName(pane))
            {
                case LeftPaneName:
                    return leftPane;
                case ContentPaneName:
                    return contentPane;
                case RightPaneName:
                    return rightPane;
                default:
                    throw new ArgumentException("Unknown pane name: " + pane, nameof(pane));
            }
        }

        private static string NormalizePaneName(string pane)
        {
            if (string.Equals(pane, "leftPane", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pane, LeftPaneName, StringComparison.OrdinalIgnoreCase))
            {
                return LeftPaneName;
            }

            if (string.Equals(pane, "contentPane", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pane, ContentPaneName, StringComparison.OrdinalIgnoreCase))
            {
                return ContentPaneName;
            }

            if (string.Equals(pane, "rightPane", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pane, RightPaneName, StringComparison.OrdinalIgnoreCase))
            {
                return RightPaneName;
            }

            return pane;
        }

        //*******************************************************
        //
        // The OrderModules helper method is used to reset the display
        // order for modules within a pane
        //
        //*******************************************************

        private static void OrderModules(List<ModuleSettings> list)
        {
            int i = 1; // 初始化一个计数器用于设置模块顺序。

            // Sort the list of module settings
            // 对模块设置列表进行排序。
            list.Sort();

            // Renumber the order of modules, setting every other position to be empty
            // 重新编号模块的顺序，每隔一个位置留空。
            foreach (ModuleSettings m in list)
            {
                // Set the module order to odd numbers only (1, 3, 5, etc.)
                // 这样做是为了在移动项目上下时提供一个空的顺序号。
                m.ModuleOrder = i;
                i += 2; // 每次迭代增加2，确保下一次迭代时是下一个奇数。
            }
        }
    }
}
