using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

// 命名空间声明
namespace ASPNET.StarterKit.Portal
{
    // 定义一个Tabs类，继承自PortalModuleControl<Tabs>
    public partial class Tabs : PortalModuleControl<Tabs>
    {
        // 用于存储门户页设置的列表
        protected readonly List<TabSettings> PortalTabs = new List<TabSettings>();
        private int _tabId;
        private int _tabIndex;

        // 依赖注入配置
        [Dependency]
        public IModuleDefsDb ModuleDefConfig { private get; set; }

        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        [Dependency]
        public ITabsDb TabsConfig { private get; set; }

        [Dependency]
        public IGlobalsDb PortalConfig { private get; set; }

        //*******************************************************
        //
        // The Page_Load server event handler on this user control is used
        // to populate the current tab settings from the database
        //
        //*******************************************************
        // 页面加载事件处理器
        protected void Page_Load(object sender, EventArgs e)
        {
            // 验证当前用户是否有权限访问该页面
            if (PortalSecurity.IsInRoles("Admins") == false)
            {
                Response.Redirect("~/Admin/EditAccessDenied.aspx");
            }

            // 如果请求参数中存在tabid，则解析它
            if (Request.Params["tabid"] != null)
            {
                _tabId = Int32.Parse(Request.Params["tabid"]);
            }
            // 如果请求参数中存在tabindex，则解析它
            if (Request.Params["tabindex"] != null)
            {
                _tabIndex = Int32.Parse(Request.Params["tabindex"]);
            }

            // 从当前上下文获取PortalSettings
            var portalSettings = (PortalSettings)Context.Items["PortalSettings"];

            // 遍历门户设置中的所有桌面页，并将它们添加到PortalTabs列表中
            foreach (ITabItem tab in portalSettings.DesktopTabs)
            {
                PortalTabs.Add(new TabSettings(tab));
            }

            // 将管理员页的排序号设置为一个大的数值，以确保它始终位于末尾
            TabSettings adminTab = PortalTabs[PortalTabs.Count - 1];
            adminTab.TabOrder = 99999;

            // 如果不是回发请求，则绑定页数据到页面的ListBox
            if (Page.IsPostBack == false)
            {
                tabList.DataBind();
            }
        }

        //*******************************************************
        //
        // The UpDown_Click server event handler on this page is
        // used to move a portal module up or down on a tab's layout pane
        //
        //*******************************************************
        // 上下移动页的事件处理器
        protected void UpDown_Click(Object sender, ImageClickEventArgs e)
        {
            // 获取点击的ImageButton的命令名称
            string cmd = ((ImageButton)sender).CommandName;

            // 如果ListBox中有选中的项
            if (tabList.SelectedIndex != -1)
            {
                int delta;

                // 根据命令名称确定要应用于模块列表中的顺序号的变化量
                if (cmd == "down")
                {
                    delta = 3;
                }
                else
                {
                    delta = -3;
                }

                // 获取当前选中的页设置，并更新其排序号
                TabSettings t = PortalTabs[tabList.SelectedIndex];
                t.TabOrder += delta;

                // 重新设置页列表中的排序号
                OrderTabs();

                // 重定向刷新页面
                Response.Redirect("~/DesktopDefault.aspx?tabindex=" + (PortalTabs.Count - 1) + "&tabid=" + _tabId);
            }
        }

        // 删除页的事件处理器
        protected void DeleteBtn_Click(Object sender, ImageClickEventArgs e)
        {
            // 如果ListBox中有选中的项
            if (tabList.SelectedIndex != -1)
            {
                // 必须从数据库中删除页
                TabSettings t = PortalTabs[tabList.SelectedIndex];
                TabsConfig.DeleteTab(t.TabId);

                // 从列表中移除项目
                PortalTabs.RemoveAt(tabList.SelectedIndex);

                // 重新排序列表
                OrderTabs();

                // 重定向刷新页面
                Response.Redirect("~/DesktopDefault.aspx?tabindex=" + _tabIndex + "&tabid=" + _tabId);
            }
        }

        // 添加页的事件处理器
        protected void AddTab_Click(Object sender, EventArgs e)
        {
            // 从当前上下文获取PortalSettings
            var portalSettings = (PortalSettings)Context.Items["PortalSettings"];

            // 将页写入数据库
            int tabId = TabsConfig.AddTab(portalSettings.PortalId, "New Tab", 999);

            // 新页放在列表的末尾
            ITabItem tab = TabsConfig.GetSingleTab(tabId);
            PortalTabs.Add(new TabSettings(tab));

            // 重新加载PortalSettings
            HttpContext.Current.Items["PortalSettings"] = new PortalSettings(portalSettings.PortalId, tabId, PortalConfig, TabsConfig, ModulesConfig, ModuleDefConfig);

            // 重新设置页列表中的排序号
            OrderTabs();

            // 重定向到编辑页面
            Response.Redirect("~/Admin/TabLayout.aspx?tabid=" + tabId);
        }

        // 编辑页的事件处理器
        protected void EditBtn_Click(Object sender, ImageClickEventArgs e)
        {
            // 如果ListBox中有选中的项
            if (tabList.SelectedIndex != -1)
            {
                // 重定向到当前选择的页的编辑页面
                TabSettings t = PortalTabs[tabList.SelectedIndex];

                Response.Redirect("~/Admin/TabLayout.aspx?tabid=" + t.TabId);
            }
        }

        // 用于重新设置门户页显示顺序的帮助方法
        private void OrderTabs()
        {
            int i = 1;

            // 排序数组列表
            PortalTabs.Sort();

            // 重新编号排序号并保存到数据库
            foreach (TabSettings t in PortalTabs)
            {
                // 将项目编号为1, 3, 5等，以便在列表中移动项目时提供一个空的排序号。
                t.TabOrder = i;
                i += 2;

                // 将页重新写入数据库
                TabsConfig.UpdateTabOrder(t.TabId, t.TabOrder);
            }
        }
    }
}