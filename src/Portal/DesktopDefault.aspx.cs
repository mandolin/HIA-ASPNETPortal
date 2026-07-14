using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 将当前 Tab 的已授权模块动态装配到门户页面窗格的主页面。
    /// Main page that dynamically assembles authorized modules for the current Tab into portal layout panes.
    /// </summary>
    /// <remarks>
    /// 模块入口会先通过 <see cref="PortalModuleCatalog"/> 解析。已验证部署包遵从启用状态；旧模块仍以受限路径兼容加载。
    /// 单个模块解析或加载失败只记录诊断并跳过该模块，不应终止整个页面请求。
    /// Each module entry is first resolved by <see cref="PortalModuleCatalog"/>. Validated deployment packages obey
    /// enabled state, while legacy modules remain compatible through constrained paths. A single module resolution or
    /// load failure records diagnostics and skips that module; it should not terminate the entire page request.
    /// </remarks>
    public partial class DesktopDefault : PortalPage<DesktopDefault>
    {
        /// <summary>
        /// 在页面初始化阶段检查 Tab 访问权，并将登录控件和可加载模块放入对应窗格。
        /// Checks Tab access and places the sign-in control and loadable modules into their panes during page initialization.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">初始化事件参数。Initialization event arguments.</param>
        /// <remarks>
        /// 未通过当前 Tab 角色检查的请求会重定向到拒绝访问页。主页匿名访问会额外放置登录模块；这不是模块包注册流程的一部分。
        /// A request failing the current Tab role check redirects to the access-denied page. Anonymous home-page visits
        /// additionally receive the sign-in module; this is not part of module-package registration.
        /// </remarks>
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
                var signInContainer = new Panel
                {
                    CssClass = "portal-module portal-module-signin portal-pane-leftpane"
                };
                signInContainer.Controls.Add(Page.LoadControl("~/DesktopModules/SignIn.ascx"));
                LeftPane.Controls.Add(signInContainer);
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
                    // P3.2 先把旧模块和已验证部署包统一解析到受控描述中。旧模块保持可用；
                    // 已声明 module.json 但校验失败的目录不能回退为普通路径加载。
                    // P3.2 first resolves legacy modules and validated deployment packages into one controlled descriptor.
                    // Legacy modules remain available; a directory declaring module.json but failing validation must not
                    // fall back to an ordinary path load.
                    PortalModuleRuntimeDescriptor moduleDescriptor;
                    string moduleReason;
                    if (!PortalModuleCatalog.TryResolveModule(
                            _moduleSettings,
                            Context,
                            out moduleDescriptor,
                            out moduleReason))
                    {
                        PortalDiagnostics.Warn(
                            "ModulePackage.LoadBlocked",
                            "Skipping module " + _moduleSettings.ModuleId + ": " + moduleReason,
                            Context);
                        continue;
                    }

                    if (!moduleDescriptor.IsEnabled)
                    {
                        PortalDiagnostics.Info(
                            "ModulePackage.Disabled",
                            "Skipping disabled module package '" + moduleDescriptor.Package.PackageId +
                            "' for module " + _moduleSettings.ModuleId + ".",
                            Context);
                        continue;
                    }

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

                    // 使用稳定包装元素承载模块 CSS scope。缓存只保存模块内部输出，因此纯 CSS scope 不改变缓存键。
                    // Use a stable wrapper element for module CSS scope. The cache stores only inner module output,
                    // so a CSS-only scope does not change the cache key.
                    var moduleContainer = new Panel
                    {
                        CssClass = PortalThemeResolver.GetModuleCssClass(
                            _moduleSettings.ModuleId,
                            _moduleSettings.PaneName,
                            moduleDescriptor.IsManagedPackage
                                ? moduleDescriptor.Package.PackageId
                                : null)
                    };

                    try
                    {
                        // 检查缓存时间设置是否为 0（表示不缓存）
                        if (_moduleSettings.CacheTime == 0)
                        {
                            // 对已解析的受控入口执行动态加载，而不是再次读取原始数据库路径。
                            // Dynamically load the resolved controlled entry instead of rereading the raw database path.
                            var portalModule = Page.LoadControl(moduleDescriptor.DesktopSource) as IPortalModuleControl;
                            if (portalModule == null)
                            {
                                throw new InvalidOperationException(
                                    "The module control does not implement IPortalModuleControl.");
                            }

                            // 设置加载的模块的 Portal ID 和模块配置
                            portalModule.PortalId = portalSettings.PortalId;
                            portalModule.ModuleConfiguration = _moduleSettings;

                            // 将加载的模块添加到 CSS scope 容器中。
                            // Add the loaded module to the CSS-scope container.
                            moduleContainer.Controls.Add((UserControl)portalModule);
                        }
                        else
                        {
                            // 缓存仍使用既有机制，但缓存键同时隔离已验证包版本和状态修订。
                            // Caching still uses the existing mechanism, while its key also isolates package version
                            // and state revision.
                            var portalModule = new CachedPortalModuleControl
                            {
                                DesktopSource = moduleDescriptor.DesktopSource,
                                CacheIdentity = moduleDescriptor.CacheIdentity
                            };

                            // 设置缓存模块的 Portal ID 和模块配置
                            portalModule.PortalId = portalSettings.PortalId;
                            portalModule.ModuleConfiguration = _moduleSettings;

                            // 将缓存模块添加到 CSS scope 容器中。
                            // Add the cached module to the CSS-scope container.
                            moduleContainer.Controls.Add(portalModule);
                        }
                    }
                    catch (Exception exception)
                    {
                        PortalDiagnostics.Error(
                            "ModulePackage.Load",
                            "Loading module " + _moduleSettings.ModuleId + " failed.",
                            exception,
                            Context);
                        continue;
                    }

                    parent.Controls.Add(moduleContainer);

                    // Dynamically inject separator break between portal modules
                    // 在门户模块之间动态注入分隔符换行符
                    parent.Controls.Add(new LiteralControl("<" + "br" + ">"));
                    parent.Visible = true;
                }
            }
        }
    }
}
