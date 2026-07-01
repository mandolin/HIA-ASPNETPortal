using System;
using System.Web;
using System.Web.UI;

namespace ASPNET.StarterKit.Portal
{
    public partial class DesktopDefault : PortalPage<DesktopDefault>
    {
        protected void Page_Init(object sender, EventArgs e)
        {
            //*********************************************************************
            //
            // Page_Init Event Handler 事件处理程序
            //
            // The Page_Init event handler executes at the very beginning of each page
            // request (immediately before Page_Load).
            // 事件处理程序在每个页面请求的最开始执行（在 Page_Load 之前立即执行）
            //
            // The Page_Init event handler below determines the tab index of the currently
            // requested portal view, and then calls the PopulatePortalSection utility
            // method to dynamically populate the left, center and right hand sections
            // of the portal tab.
            // 下面的程序确定当前请求的门户视图的标签索引，
            // 然后调用 PopulatePortalSection 实用方法动态填充门户选项卡的左、中、右侧部分。
            //
            //*********************************************************************

            // Obtain PortalSettings from Current Context
            // 从当前上下文中获取 PortalSettings
            var portalSettings = PortalContext.GetPortalSettings();

            // Ensure that the visiting user has access to the current page
            // 确保访问用户有权限访问当前页面
            if (!PortalSecurity.IsInRoles(portalSettings.ActiveTab.AuthorizedRoles))
            {
                Response.Redirect("~/Admin/AccessDenied.aspx");
            }

            // Dynamically inject a signin login module into the top left-hand corner
            // of the home page if the client is not yet authenticated
            // 如果客户端尚未经过身份验证，并且当前标签的索引为 0，则在主页的左上角动态注入登录模块
            if (!Request.IsAuthenticated && portalSettings.ActiveTab.TabIndex == 0)
            {
                LeftPane.Controls.Add(Page.LoadControl("~/DesktopModules/SignIn.ascx"));
                LeftPane.Visible = true;
            }

            // Dynamically Populate the Left, Center and Right pane sections of the portal page
            // 动态填充门户页面的左、中、右窗格部分
            if (portalSettings.ActiveTab.Modules.Count > 0)
            {
                // Loop through each entry in the configuration system for this tab
                // 遍历此标签的配置系统中的每个条目
                foreach (ModuleSettings _moduleSettings in portalSettings.ActiveTab.Modules)
                {
                    Control parent = LeftPane; //default

                    switch (_moduleSettings.PaneName)
                    {
                        case "LeftPane":
                            parent = LeftPane;
                            break;
                        case "ContentPane":
                            parent = ContentPane;
                            break;
                        case "RightPane":
                            parent = RightPane;
                            break;
                    }
                    //Control parent = Page.FindControl(_moduleSettings.PaneName);

                    // If no caching is specified, create the user control instance and dynamically
                    // inject it into the page.  Otherwise, create a cached module instance that
                    // may or may not optionally inject the module into the tree
                    // 如果未指定缓存，则创建用户控件实例并将其动态注入页面。
                    // 否则，创建一个缓存的模块实例，该实例可能会或可能不会选择性地将模块注入到树中

                    // 检查缓存时间设置是否为 0（表示不缓存）
                    if (_moduleSettings.CacheTime == 0)
                    {
                        // 如果不缓存，动态加载模块
                        string desktopSource = PortalModulePathValidator.NormalizeDesktopSourceOrThrow(_moduleSettings.DesktopSrc);
                        var portalModule = (IPortalModuleControl)Page.LoadControl(desktopSource);

                        // 设置加载的模块的 Portal ID 和模块配置
                        portalModule.PortalId = portalSettings.PortalId;
                        portalModule.ModuleConfiguration = _moduleSettings;

                        // 将加载的模块添加到父控件容器中
                        parent.Controls.Add((UserControl)portalModule);
                    }
                    else
                    {
                        // 如果启用缓存，使用缓存的 Portal 模块控件
                        var portalModule = new CachedPortalModuleControl();

                        // 设置缓存模块的 Portal ID 和模块配置
                        portalModule.PortalId = portalSettings.PortalId;
                        portalModule.ModuleConfiguration = _moduleSettings;

                        // 将缓存的模块添加到父控件容器中
                        parent.Controls.Add(portalModule);
                    }

                    // Dynamically inject separator break between portal modules
                    // 在门户模块之间动态注入分隔符换行符
                    parent.Controls.Add(new LiteralControl("<" + "br" + ">"));
                    parent.Visible = true;
                }
            }
        }
    }
}
