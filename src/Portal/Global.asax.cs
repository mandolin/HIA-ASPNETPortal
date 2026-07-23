using ASPNET.StarterKit.Portal.Sys;
using ASPNET.StarterKit.Portal.Util;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Xml;
using Unity;
using Unity.Configuration;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户 ASP.NET 应用程序生命周期入口与请求级上下文装配器。</zh-CN>
    ///   <en>ASP.NET application-lifecycle entry point and request-context composer for the Portal.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类初始化 Unity、外置连接串和运行级服务，并处理请求上下文、认证角色和未处理异常。它不是业务授权或配置写入 API；新增全局行为时必须评估启动失败、错误泄漏和所有请求的兼容影响。</zh-CN>
    ///   <en>This class initializes Unity, external connection strings, and runtime services, and handles request context, authenticated roles, and unhandled errors. It is not a business-authorization or configuration-write API; new global behavior must consider startup failure, error disclosure, and compatibility across every request.</en>
    /// </lang>
    /// </remarks>
    public class Global : HttpApplication, IContainerAccessor
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前应用程序的 Unity 容器。</zh-CN>
        ///   <en>Unity container for the current application.</en>
        /// </lang>
        /// </summary>
        public static IUnityContainer Container { get; set; }

        #region IContainerAccessor Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回当前应用程序的 Unity 容器。</zh-CN>
        ///   <en>Returns the Unity container of the current application.</en>
        /// </lang>
        /// </summary>
        IUnityContainer IContainerAccessor.Container
        {
            get { return Container; }
        }

        #endregion

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理应用程序启动事件，并装配配置、容器和运行级基础服务。</zh-CN>
        ///   <en>Handles application startup and composes configuration, the container, and runtime infrastructure services.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void Application_Start(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>先解析当前环境标识；后续 appSettings、Unity 覆盖和外置连接串都以它作为选择维度。</zh-CN>
            //   <en>Resolve the current environment marker first; later appSettings, Unity overrides, and external connection strings all use it as the selection dimension.</en>
            // </lang>
            var env = (EnvSection)ConfigurationManager.GetSection("env") ?? "dev";
            GlobalInfo.Environment = env;

            //System.Diagnostics.Debug.WriteLine($"当前env：{env}");

            //ConfigurationManager.AppSettings.Set("TestKey", "xxxxxxxxxx");


            // <lang>
            //   <zh-CN>按“基础配置 + 环境覆盖”的顺序加载 JSON appSettings；真实敏感值仍由外置配置或环境变量处理。</zh-CN>
            //   <en>Load JSON appSettings in "base configuration plus environment override" order; real sensitive values remain handled by external configuration or environment variables.</en>
            // </lang>
            AppSettingsLoader.LoadConfig("Config/appSettings.json");
            AppSettingsLoader.LoadConfig($"Config/appSettings.{env}.json");


            // <lang>
            //   <zh-CN>创建 Unity 根容器。旧 Web Forms 页面和控件依赖 BuildUp/Resolve，因此容器初始化失败应尽早暴露。</zh-CN>
            //   <en>Create the Unity root container. Legacy Web Forms pages and controls depend on BuildUp/Resolve, so container initialization failures should surface early.</en>
            // </lang>
            Container = new UnityContainer();

            // <lang>
            //   <zh-CN>先加载主 Unity 配置与环境覆盖 XML，再注册外置连接串命名实例；这样既保留旧数据访问层依赖名，也避免真实连接串继续写在 UnityCfg*.xml 中。</zh-CN>
            //   <en>Load the main Unity configuration and environment override XML before registering the external connection-string named instance; this keeps legacy data-layer dependency names while preventing real connection strings from remaining in UnityCfg*.xml.</en>
            // </lang>
            var section = (UnityConfigurationSection)ConfigurationManager.GetSection("unity");
            section.Configure(Container);
            UnityConfigLoader.LoadUnityConfig(Container, $"Config/UnityCfg.{env}.xml");

            // <lang>
            //   <zh-CN>从仓库外部读取语义化连接串 Portal，并映射为旧代码仍使用的 connectionString。加载器会校验外置文件存在；环境变量只覆盖连接串值，不绕过文件校验。</zh-CN>
            //   <en>Read the semantic Portal connection string outside the repository and map it to the legacy connectionString name. The loader verifies that the external file exists; environment variables override only the connection-string value and do not bypass file validation.</en>
            // </lang>
            ExternalConnectionStringLoadResult portalConnectionString =
                ExternalConnectionStringLoader.LoadPortalConnectionString(env);
            Container.RegisterInstance(
                ExternalConnectionStringLoader.UnityConnectionStringName,
                portalConnectionString.ConnectionString);
            // <lang>
            //   <zh-CN>新代码通过 profile 获取 provider invariant，不再把连接串文本当成数据库类型标识。</zh-CN>
            //   <en>New code receives a profile with the provider invariant instead of treating a connection string as a database type.</en>
            // </lang>
            Container.RegisterInstance(portalConnectionString.DatabaseProfile);
            Container.RegisterType<IPortalDbConnectionFactory, PortalDbConnectionFactory>();
            PortalDiagnostics.Info(
                "Startup",
                $"Loaded connection string '{ExternalConnectionStringLoader.LogicalConnectionStringName}' with provider '{portalConnectionString.ProviderInvariantName}' from {portalConnectionString.Source}; ConfigFile={portalConnectionString.ConfigFile}");
            PortalDiagnostics.CheckSqlConnection(portalConnectionString.ConnectionString);
            RegisterPasswordPolicyOptionsProvider();

            // <lang>
            //   <zh-CN>启动期最小自检只解析关键服务和环境覆盖字符串，验证容器 wiring；不在这里执行业务迁移或破坏性数据修复。</zh-CN>
            //   <en>The startup smoke check only resolves key services and the environment override string to verify container wiring; it does not perform business migrations or destructive data fixes.</en>
            // </lang>
            var usersDbService = Container.Resolve<IUsersDb>();
            string testStr = Container.Resolve<string>("testStr");
            PortalDiagnostics.Info(
                "Startup",
                $"Resolved IUsersDb: {usersDbService?.GetType().Name}; Resolved testStr: {!string.IsNullOrEmpty(testStr)}");


        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把 Web 层运行期系统设置接入组件层密码策略。</zh-CN>
        ///   <en>Connects Web-layer runtime system settings to the component-layer password policy.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN><see cref="PortalPasswordPolicy"/> 位于独立组件项目，不能反向依赖 Web 配置读取器；因此启动期通过委托注入当前有效策略。读取失败时策略类自身会回退到硬下限默认值。</zh-CN>
        ///   <en><see cref="PortalPasswordPolicy"/> lives in the independent components project and must not depend back on the Web configuration resolver, so startup injects a delegate for the effective policy. The policy class falls back to its hard lower-bound defaults if reads fail.</en>
        /// </lang>
        /// </remarks>
        private static void RegisterPasswordPolicyOptionsProvider()
        {
            PortalPasswordPolicy.ConfigureOptionsProvider(
                () => new PortalPasswordPolicyOptions(
                    PortalRuntimeSettings.GetInt32(PortalSettingsRegistry.PasswordMinimumLength),
                    PortalRuntimeSettings.GetInt32(PortalSettingsRegistry.PasswordRequiredCategoryCount),
                    PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.PasswordWeakDictionaryEnabled),
                    PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.PasswordDisallowContextTerms)));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理应用程序结束事件，并释放 Unity 容器。</zh-CN>
        ///   <en>Handles application shutdown and disposes the Unity container.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void Application_End(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>容器可能因启动失败而保持为空；关闭路径必须允许这种半初始化状态。</zh-CN>
            //   <en>The container may remain null after startup failure, so shutdown must tolerate this partially initialized state.</en>
            // </lang>
            if (Container != null)
            {
                Container.Dispose();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用当前 Unity 容器为既有 Web Forms 控件实例执行后期依赖注入。</zh-CN>
        ///   <en>Uses the current Unity container to perform late dependency injection on an existing Web Forms control instance.</en>
        /// </lang>
        /// </summary>
        /// <typeparam name="T">
        /// <l>
        ///   <zh-CN>控件或页面的实际类型，用于 Unity BuildUp。</zh-CN>
        ///   <en>Actual control or page type used for Unity BuildUp.</en>
        /// </l>
        /// </typeparam>
        /// <param name="ctrl">
        /// <l>
        ///   <zh-CN>已由 Web Forms 生命周期创建的控件实例。</zh-CN>
        ///   <en>Control instance already created by the Web Forms lifecycle.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>Web Forms 通常先由 ASP.NET 创建页面和控件，再进入项目代码；因此构造函数注入在旧页面里不可用。本方法把 Unity 的 <c>BuildUp</c> 限定为“补充已注册属性依赖”的兼容入口，不负责创建控件，也不改变控件生命周期。</zh-CN>
        ///   <en>Web Forms usually lets ASP.NET create pages and controls before project code runs, so constructor injection is unavailable in legacy pages. This method limits Unity <c>BuildUp</c> to the compatibility role of filling registered property dependencies; it does not create controls or change their lifecycle.</en>
        /// </lang>
        /// </remarks>
        public static void BuildItemWithCurrentContext<T>(Control ctrl)
        {
            Container.BuildUp(typeof(T), ctrl);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理每个请求的 BeginRequest 事件，建立当前 PortalSettings 与请求文化。</zh-CN>
        ///   <en>Handles the BeginRequest event for each request and establishes the current PortalSettings and request culture.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>PortalSettings 会写入当前 <c>HttpContext</c>，供同一请求内的页面、控件和组件读取。查询参数只用于选择活动 Tab，上下文构造阶段必须使用安全默认值，具体页面再处理非法目标。</zh-CN>
        ///   <en>PortalSettings is stored in the current <c>HttpContext</c> for pages, controls, and components in the same request. Query parameters select the active Tab only; context construction must use safe defaults, and individual pages then handle invalid targets.</en>
        /// </lang>
        /// </remarks>
        /// <param name = "sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name = "e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void Application_BeginRequest(Object sender, EventArgs e)
        {
            //string TestKey = ConfigurationManager.AppSettings.Get("TestKey").ToString();
            //System.Diagnostics.Debug.WriteLine($"获取TestKey：{TestKey}");

            if (IsGenericErrorPageRequest(Request))
            {
                return;
            }

            // <lang>
            //   <zh-CN>BeginRequest 必须先使用安全的活动 Tab 上下文。无效查询参数由具体页面随后拒绝，避免门户设置构造阶段因不存在的 Tab 直接进入全局错误页。</zh-CN>
            //   <en>BeginRequest must first use a safe active-Tab context. Individual pages reject invalid query parameters afterwards, preventing a nonexistent Tab from reaching the global error page during settings construction.</en>
            // </lang>
            int tabIndex = GetNonNegativeIntParam("tabindex", 0);
            int tabId = GetPositiveIntParam("tabid", 1);

            var portalConfig = Container.Resolve<IGlobalsDb>();
            var tabsConfig = Container.Resolve<ITabsDb>();
            var modulesConfig = Container.Resolve<IModulesDb>();
            var moduleDefConfig = Container.Resolve<IModuleDefsDb>();

            // <lang>
            //   <zh-CN>PortalSettings 是旧门户请求级对象图的核心；先集中构造，再通过 PortalContext 挂入当前请求。</zh-CN>
            //   <en>PortalSettings is the core request-level object graph of the legacy portal; build it centrally and attach it to the current request through PortalContext.</en>
            // </lang>
            PortalContext.SetPortalSettings(new PortalSettings(tabIndex, tabId, portalConfig, tabsConfig, modulesConfig, moduleDefConfig), Context);

            SetLanguage();

            // <lang>
            //   <zh-CN>文化名来自 Cookie 或浏览器请求头，必须先确认是 .NET 支持的 culture，再写入线程上下文。</zh-CN>
            //   <en>Culture names come from cookies or browser headers and must be confirmed as .NET-supported cultures before being applied to the thread context.</en>
            // </lang>
            bool IsValidCulture(string cultureName)
            {
                return CultureInfo.GetCultures(CultureTypes.AllCultures)?.Any(c => c.Name == cultureName) ?? false;
            }

            // <lang>
            //   <zh-CN>同时设置 CurrentCulture 与 CurrentUICulture，避免格式化文化和资源文化在同一请求内分裂。</zh-CN>
            //   <en>Set both CurrentCulture and CurrentUICulture so formatting culture and resource culture do not diverge within the same request.</en>
            // </lang>
            void SetCulture(string cultureName)
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(cultureName);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(cultureName);
            }

            // <lang>
            //   <zh-CN>活动 Tab 索引允许零；负数、非数字和缺失值使用安全默认值。</zh-CN>
            //   <en>Active Tab indexes may be zero; negative, nonnumeric, and missing values use the safe default.</en>
            // </lang>
            int GetNonNegativeIntParam(string paramName, int defaultValue)
            {
                string rawValue = Request.QueryString[paramName] ?? Request.Unvalidated.Form[paramName];
                if (int.TryParse(rawValue, out int value) && value >= 0)
                {
                    return value;
                }

                return defaultValue;
            }

            // <lang>
            //   <zh-CN>活动 Tab 标识必须为正数；负数、零、非数字和缺失值使用安全默认值。</zh-CN>
            //   <en>Active Tab identifiers must be positive; negative, zero, nonnumeric, and missing values use the safe default.</en>
            // </lang>
            int GetPositiveIntParam(string paramName, int defaultValue)
            {
                string rawValue = Request.QueryString[paramName] ?? Request.Unvalidated.Form[paramName];
                if (int.TryParse(rawValue, out int value) && value > 0)
                {
                    return value;
                }

                return defaultValue;
            }

            // <lang>
            //   <zh-CN>语言选择优先显式 Cookie，其次浏览器首选语言，最后回退 en-US；此处只影响当前请求线程。</zh-CN>
            //   <en>Language selection prefers the explicit cookie, then the browser's first preferred language, and finally falls back to en-US; it affects only the current request thread.</en>
            // </lang>
            void SetLanguage()
            {
                var langCookie = Request.Cookies["lang"];
                var userLanguages = Request.UserLanguages;

                if (IsValidCulture(langCookie?.Value))
                {
                    SetCulture(langCookie.Value);
                }
                else if (userLanguages?.Length > 0 && IsValidCulture(userLanguages[0]))
                {
                    SetCulture(userLanguages[0]);
                }
                else
                {
                    SetCulture("en-US"); // 设置默认语言
                }
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理未捕获的应用程序错误，记录诊断事件编号，并按部署开关转向通用错误页。</zh-CN>
        ///   <en>Handles unhandled application errors, records a diagnostics event id, and redirects to the generic error page according to deployment switches.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>通用错误页仅展示符合运行时编号格式的事件 ID；详细错误仅能在显式配置且本地请求时保留，不能把异常正文、连接串或其他运行时细节直接返回给普通访问者。</zh-CN>
        ///   <en>The generic error page displays only event ids matching the runtime format. Detailed errors are retained only for explicitly configured local requests; exception bodies, connection strings, and other runtime details must not be returned directly to ordinary visitors.</en>
        /// </lang>
        /// </remarks>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void Application_Error(object sender, EventArgs e)
        {
            Exception exception = Server.GetLastError();
            string eventId = PortalDiagnostics.Unhandled(exception, Context);

            if (IsGenericErrorPageRequest(Request))
            {
                return;
            }

            // <lang>
            //   <zh-CN>开发环境可用显式开关保留 ASP.NET 详细错误页；后续应升级为密码或角色授权查看，避免误把生产详细错误暴露给普通用户。</zh-CN>
            //   <en>Development can keep detailed ASP.NET errors by explicit switch; later this should require password or role authorization to avoid exposing production details to ordinary users.</en>
            // </lang>
            if (PortalDiagnostics.AreDetailedErrorsEnabled() && Request.IsLocal)
            {
                return;
            }

            Server.ClearError();
            Response.Clear();
            Response.TrySkipIisCustomErrors = true;
            Response.Redirect("~/GenericErrorPage.aspx?id=" + HttpUtility.UrlEncode(eventId), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理应用程序的 AuthenticateRequest 事件，为已认证请求构造包含角色的 <see cref="GenericPrincipal"/>。</zh-CN>
        ///   <en>Handles the application's AuthenticateRequest event and builds a role-bearing <see cref="GenericPrincipal"/> for authenticated requests.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>先校验身份票据中的安全版本是否与数据库一致，再读取加密的 <c>portalroles</c> Cookie；角色 Cookie 缺失、过期、无法解密或安全版本不匹配时从数据库读取角色并重建。此方法不创建 HIA 用户模型，只维持当前 Web Forms 请求级身份兼容。</zh-CN>
        ///   <en>The security version in the authentication ticket is validated against the database before the encrypted <c>portalroles</c> cookie is read. When the role cookie is missing, expired, undecryptable, or security-version mismatched, roles are loaded from the database and the cookie is rebuilt. This method does not create an HIA user model; it maintains current Web Forms request-identity compatibility.</en>
        /// </lang>
        /// </remarks>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {
            if (Request.IsAuthenticated)
            {
                string[] roles;
                var usersDb = Container.Resolve<IUsersDb>();
                var formsIdentity = Context.User?.Identity as FormsIdentity;
                long ticketSecurityVersion;
                long databaseSecurityVersion = usersDb.GetSecurityVersionByUserName(Context.User.Identity.Name);

                if (!PortalAuthenticationService.TryReadSecurityVersion(formsIdentity, out ticketSecurityVersion) ||
                    ticketSecurityVersion != databaseSecurityVersion)
                {
                    PortalOperationAudit.Record(
                        "SecuritySession",
                        "Revoke",
                        "User",
                        Context.User.Identity.Name,
                        "Authentication ticket security version mismatch.",
                        Context);
                    PortalAuthenticationService.SignOut(Response, Request);
                    Context.User = null;
                    Response.Redirect(Request.ApplicationPath, false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                // <lang>
                //   <zh-CN>优先使用安全版本匹配的角色 Cookie；缺失或失效时从数据库重建，避免旧会话保留被撤销角色。</zh-CN>
                //   <en>Prefer a role cookie whose security version matches; rebuild from the database when absent or invalid so old sessions do not retain revoked roles.</en>
                // </lang>
                if (!PortalAuthenticationCookies.TryReadRoles(Request, databaseSecurityVersion, out roles))
                {
                    // <lang>
                    //   <zh-CN>只将角色名称写入加密票据，不写入密码、用户资料或其他敏感业务数据。</zh-CN>
                    //   <en>Write role names only into the encrypted ticket, never passwords, profile data, or other sensitive business data.</en>
                    // </lang>
                    roles = PortalRoleParser.Parse(string.Join(";", usersDb.GetRoleNamesByUser(User.Identity.Name)));

                    bool isPersistent = formsIdentity?.Ticket?.IsPersistent ?? false;
                    PortalAuthenticationCookies.WriteRolesCookie(
                        Response,
                        Request,
                        Context.User.Identity.Name,
                        databaseSecurityVersion,
                        roles,
                        isPersistent);
                }

                // <lang>
                //   <zh-CN>当前只构造请求级 GenericPrincipal；SysUser/BizUser 等 HIA 模型保留给后续边界设计。</zh-CN>
                //   <en>Build only a request-level GenericPrincipal for now; HIA models such as SysUser/BizUser remain for later boundary design.</en>
                // </lang>
                Context.User = new GenericPrincipal(Context.User.Identity, roles);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回当前应用在根站点或虚拟目录下都可使用的相对应用路径。</zh-CN>
        ///   <en>Returns the relative application path that works for both root-site and virtual-directory deployment.</en>
        /// </lang>
        /// </summary>
        /// <param name = "request">
        /// <l>
        ///   <zh-CN>当前 HTTP 请求。</zh-CN>
        ///   <en>Current HTTP request.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>不带末尾斜杠的应用路径；根站点返回空字符串。</zh-CN>
        ///   <en>Application path without a trailing slash; root-site deployment returns an empty string.</en>
        /// </l>
        /// </returns>
        public static string GetApplicationPath(HttpRequest request)
        {
            // <lang>
            //   <zh-CN>旧代码经常把返回值直接拼进站内 URL；统一去除末尾斜杠可减少根站点和虚拟目录差异。</zh-CN>
            //   <en>Legacy code often concatenates this value into site-local URLs; removing the trailing slash consistently reduces root-site and virtual-directory differences.</en>
            // </lang>
            return request.ApplicationPath?.TrimEnd('/') ?? "";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前请求是否正在访问通用错误页。</zh-CN>
        ///   <en>Determines whether the current request is for the generic error page.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>当前 HTTP 请求，可为 <c>null</c>。</zh-CN>
        ///   <en>Current HTTP request, which may be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求目标为 <c>GenericErrorPage.aspx</c> 时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the request target is <c>GenericErrorPage.aspx</c>.</en>
        /// </l>
        /// </returns>
        private static bool IsGenericErrorPageRequest(HttpRequest request)
        {
            string appRelativePath = request?.AppRelativeCurrentExecutionFilePath;
            return string.Equals(appRelativePath, "~/GenericErrorPage.aspx", StringComparison.OrdinalIgnoreCase);
        }
    }
}
