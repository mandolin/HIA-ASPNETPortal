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
        /// <lang>
        ///   <zh-CN>在页面初始化阶段完成 Tab 入口检查，并把登录控件和可加载模块装配到对应窗格。</zh-CN>
        ///   <en>Performs Tab entry checks and assembles sign-in and loadable modules into their panes during page initialization.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender"><l zh-CN="Web Forms 初始化事件源；处理逻辑不依赖其具体类型。" en="Web Forms initialization event source; the handler does not depend on its concrete type." /></param>
        /// <param name="e"><l zh-CN="初始化事件参数，随后原样交由页面生命周期使用。" en="Initialization event arguments subsequently used by the page lifecycle as supplied." /></param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>Tab 角色入口检查和主页匿名 SignIn 注入是独立的页面访问边界。其后模块装配仅消费已经受控解析的运行描述符：解析或加载一个模块失败会记录诊断并跳过该模块，不终止整页；解析成功也不替代 Tab 授权或模块自身写操作授权。</zh-CN>
        ///   <en>Tab-role entry checks and home-page anonymous SignIn injection are separate page-access boundaries. The subsequent module assembly consumes only controlled runtime descriptors: failure to resolve or load one module records diagnostics and skips that module without terminating the page; successful resolution does not replace Tab authorization or a module's own write-operation authorization.</en>
        /// </lang>
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

            // <lang>
            //   <zh-CN>仅在当前 Tab 确有模块配置时进入动态装配；该区段运行在前置 Tab 角色入口检查之后，但不重新实施页面授权。</zh-CN>
            //   <en>Enter dynamic assembly only when the current Tab has module configuration; this region runs after the preceding Tab-role entry check and does not re-enforce page authorization.</en>
            // </lang>
            if (portalSettings.ActiveTab.Modules.Count > 0)
            {
                // <lang>
                //   <zh-CN>按 Tab 配置的既有顺序逐个消费模块设置，保留页面布局和诊断的稳定相对顺序。</zh-CN>
                //   <en>Consume module settings one by one in the Tab's existing configured order, preserving stable relative order for page layout and diagnostics.</en>
                // </lang>
                foreach (ModuleSettings _moduleSettings in portalSettings.ActiveTab.Modules)
                {
                    // <lang>
                    //   <zh-CN>运行描述符承载已完成的入口、包/Profile/状态事实；本装配层不从原始模块设置重新拼接路径，也不把物理文件存在当作加载许可。</zh-CN>
                    //   <en>The runtime descriptor carries completed entry, package/Profile/state facts; this assembly layer does not recompose a path from raw module settings or treat physical-file existence as load permission.</en>
                    // </lang>
                    PortalModuleRuntimeDescriptor moduleDescriptor;

                    // <lang>
                    //   <zh-CN>受控原因只用于服务器诊断类别和消息；它来自共享解析器，不能成为页面展示或授权依据。</zh-CN>
                    //   <en>The controlled reason is used only for server diagnostic category and message; it comes from the shared resolver and must not become a page-display or authorization basis.</en>
                    // </lang>
                    string moduleReason;

                    // <lang>
                    //   <zh-CN>解析失败的模块不进入控件树；根据固定前缀区分 Profile 拒绝与其它受控加载阻断，继续处理其它模块。</zh-CN>
                    //   <en>A module that fails resolution does not enter the control tree; distinguish Profile denial from other controlled load blocks by a fixed prefix, then continue with other modules.</en>
                    // </lang>
                    if (!PortalModuleCatalog.TryResolveModule(
                            _moduleSettings,
                            Context,
                            out moduleDescriptor,
                            out moduleReason))
                    {
                        // <lang>
                        //   <zh-CN>诊断键仅选择固定分类，不使用原始路径或请求输入；它让运维区分 Profile gate 与其它阻断而不改变页面结果。</zh-CN>
                        //   <en>The diagnostic key selects only a fixed category and uses no raw path or request input; it lets operations distinguish a Profile gate from other blocks without changing the page result.</en>
                        // </lang>
                        string diagnosticKey = moduleReason.StartsWith(
                            PortalModuleProfileResolver.NotAllowedReasonPrefix,
                            StringComparison.OrdinalIgnoreCase)
                            ? "ModuleProfile.NotAllowed"
                            : "ModulePackage.LoadBlocked";

                        // <lang>
                        //   <zh-CN>记录单模块的受控失败并继续，不将异常或解析详情直接写入响应，也不让失败模块阻断整个门户页。</zh-CN>
                        //   <en>Record the controlled failure for one module and continue without writing exception or resolution detail directly to the response, and without allowing the failed module to block the whole portal page.</en>
                        // </lang>
                        PortalDiagnostics.Warn(
                            diagnosticKey,
                            "Skipping module " + _moduleSettings.ModuleId + ": " + moduleReason,
                            Context);
                        continue;
                    }

                    // <lang>
                    //   <zh-CN>解析成功但显式禁用仍不得构造控件；禁用与解析失败保持不同诊断事实，且不会回退到 Legacy 或原始路径。</zh-CN>
                    //   <en>A successfully resolved but explicitly disabled module must still not construct a control; disablement remains a different diagnostic fact from resolution failure and does not fall back to Legacy or a raw path.</en>
                    // </lang>
                    if (!moduleDescriptor.IsEnabled)
                    {
                        // <lang>
                        //   <zh-CN>只记录稳定包标识和模块实例标识的运维事实，不改变状态、Profile 或页面授权。</zh-CN>
                        //   <en>Record operations facts only for the stable package and module-instance identifiers; do not change state, Profile, or page authorization.</en>
                        // </lang>
                        PortalDiagnostics.Info(
                            "ModulePackage.Disabled",
                            "Skipping disabled module package '" + moduleDescriptor.Package.PackageId +
                            "' for module " + _moduleSettings.ModuleId + ".",
                            Context);
                        continue;
                    }

                    // <lang>
                    //   <zh-CN>父容器默认固定为左窗格；未知 PaneName 保持历史默认而不根据任意字符串查找页面控件。</zh-CN>
                    //   <en>The parent container defaults to the fixed left pane; an unknown PaneName retains the historical default rather than locating a page control from an arbitrary string.</en>
                    // </lang>
                    Control parent = LeftPane; //default

                    // <lang>
                    //   <zh-CN>只接受三个代码定义的窗格名称以选择既有布局容器；该映射不重排模块，也不判定 Tab 或用户授权。</zh-CN>
                    //   <en>Accept only three code-defined pane names to select existing layout containers; this mapping neither reorders modules nor determines Tab or user authorization.</en>
                    // </lang>
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

                    // <lang>
                    //   <zh-CN>使用稳定 wrapper 承载模块 CSS scope；scope 来自受控模块实例、窗格和受管理包标识，缓存只保存内部模块输出，因此该展示容器不改变缓存键。</zh-CN>
                    //   <en>Use a stable wrapper to carry module CSS scope; the scope derives from the controlled module instance, pane, and managed-package identifier, while cache stores only inner module output, so this presentation container does not change the cache key.</en>
                    // </lang>
                    var moduleContainer = new Panel
                    {
                        CssClass = PortalThemeResolver.GetModuleCssClass(
                            _moduleSettings.ModuleId,
                            _moduleSettings.PaneName,
                            moduleDescriptor.IsManagedPackage
                                ? moduleDescriptor.Package.PackageId
                                : null)
                    };

                    // <lang>
                    //   <zh-CN>控件构造和配置注入可能因运行时 Web Forms/模块实现失败；失败局限于当前模块，catch 后不把尚未附加的 wrapper 交给父窗格。</zh-CN>
                    //   <en>Control construction and configuration injection can fail in Web Forms runtime or module implementation; confine failure to the current module, and after the catch do not give the not-yet-attached wrapper to the parent pane.</en>
                    // </lang>
                    try
                    {
                        // <lang>
                        //   <zh-CN>缓存时间为零时直接构造用户控件；非零分支委托缓存容器，当前页不自行读取或写入缓存输出。</zh-CN>
                        //   <en>When cache time is zero, construct the user control directly; the nonzero branch delegates to the cache container, and this page neither reads nor writes cached output itself.</en>
                        // </lang>
                        if (_moduleSettings.CacheTime == 0)
                        {
                            // <lang>
                            //   <zh-CN>只加载运行描述符交付的受控桌面入口，不再次读取原始数据库 DesktopSrc；接口检查阻止不符合门户模块契约的控件进入配置注入。</zh-CN>
                            //   <en>Load only the controlled desktop entry supplied by the runtime descriptor and do not reread raw database DesktopSrc; the interface check prevents controls outside the portal-module contract from receiving configuration injection.</en>
                            // </lang>
                            var portalModule = Page.LoadControl(moduleDescriptor.DesktopSource) as IPortalModuleControl;
                            if (portalModule == null)
                            {
                                throw new InvalidOperationException(
                                    "The module control does not implement IPortalModuleControl.");
                            }

                            // <lang>
                            //   <zh-CN>在模块加入控件树前写入当前 Portal 与模块设置快照，使其后续生命周期能取得已绑定运行时上下文；这不是授权或持久化操作。</zh-CN>
                            //   <en>Set the current Portal and module-settings snapshot before the module joins the control tree so its later lifecycle can obtain bound runtime context; this is neither authorization nor persistence.</en>
                            // </lang>
                            portalModule.PortalId = portalSettings.PortalId;
                            portalModule.ModuleConfiguration = _moduleSettings;

                            // <lang>
                            //   <zh-CN>仅将已通过接口和配置注入的用户控件加入本模块的 scope wrapper；父窗格尚未可见，直到当前模块完整装配成功。</zh-CN>
                            //   <en>Add only the user control that passed interface and configuration injection to this module's scope wrapper; the parent pane is not made visible until current-module assembly succeeds.</en>
                            // </lang>
                            moduleContainer.Controls.Add((UserControl)portalModule);
                        }
                        else
                        {
                            // <lang>
                            //   <zh-CN>非零缓存时间使用既有缓存容器，并传入受控入口与运行身份；缓存键/过期/输出语义由该容器独立处理。</zh-CN>
                            //   <en>A nonzero cache time uses the existing cache container and supplies the controlled entry and runtime identity; cache-key, expiry, and output semantics are handled independently by that container.</en>
                            // </lang>
                            var portalModule = new CachedPortalModuleControl
                            {
                                DesktopSource = moduleDescriptor.DesktopSource,
                                CacheIdentity = moduleDescriptor.CacheIdentity
                            };

                            // <lang>
                            //   <zh-CN>把同一 Portal 与模块设置快照交给缓存容器，使其在缓存未命中时能对已受控入口完成一致的子控件注入。</zh-CN>
                            //   <en>Give the same Portal and module-settings snapshot to the cache container so it can perform consistent child-control injection for the controlled entry on a cache miss.</en>
                            // </lang>
                            portalModule.PortalId = portalSettings.PortalId;
                            portalModule.ModuleConfiguration = _moduleSettings;

                            // <lang>
                            //   <zh-CN>将缓存容器而非原始模块直接加入 CSS scope wrapper，保留 wrapper 在页面树而缓存仅隔离内部模块输出的既有边界。</zh-CN>
                            //   <en>Add the cache container rather than the raw module directly to the CSS-scope wrapper, preserving the existing boundary where the wrapper remains in the page tree and caching isolates only inner module output.</en>
                            // </lang>
                            moduleContainer.Controls.Add(portalModule);
                        }
                    }
                    catch (Exception exception)
                    {
                        // <lang>
                        //   <zh-CN>诊断记录当前模块的加载故障但不把异常详情显示给页面；继续循环以避免一个模块阻断其它受控模块或整个门户请求。</zh-CN>
                        //   <en>Record the current module's load failure without displaying exception detail to the page; continue the loop so one module cannot block other controlled modules or the entire portal request.</en>
                        // </lang>
                        PortalDiagnostics.Error(
                            "ModulePackage.Load",
                            "Loading module " + _moduleSettings.ModuleId + " failed.",
                            exception,
                            Context);
                        continue;
                    }

                    // <lang>
                    //   <zh-CN>只在当前模块完整装配后把 scope wrapper 附加到固定父窗格，保持配置顺序且不接受动态父控件查找。</zh-CN>
                    //   <en>Append the scope wrapper to the fixed parent pane only after current-module assembly completes, preserving configuration order and accepting no dynamic parent-control lookup.</en>
                    // </lang>
                    parent.Controls.Add(moduleContainer);

                    // <lang>
                    //   <zh-CN>保留历史固定换行分隔符以维持既有模块布局；标记为代码常量，不拼接模块或请求内容。</zh-CN>
                    //   <en>Retain the historical fixed line-break separator to preserve existing module layout; the markup is a code constant and does not concatenate module or request content.</en>
                    // </lang>
                    parent.Controls.Add(new LiteralControl("<" + "br" + ">"));

                    // <lang>
                    //   <zh-CN>当前固定窗格只在至少一个模块成功附加后可见；不把可见性当作 Tab 或模块授权结论。</zh-CN>
                    //   <en>Make the current fixed pane visible only after at least one module attaches successfully; do not treat visibility as a Tab or module authorization conclusion.</en>
                    // </lang>
                    parent.Visible = true;
                }
            }
        }
    }
}
