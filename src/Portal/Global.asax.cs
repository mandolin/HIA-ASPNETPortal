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
    public class Global : HttpApplication, IContainerAccessor
    {
        /// <summary>
        ///   The Unity container for the current application
        ///   当前应用程序的Unity容器
        /// </summary>
        public static IUnityContainer Container { get; set; }

        #region IContainerAccessor Members

        /// <summary>
        ///   Returns the Unity container of the application
        ///   返回应用程序的Unity容器
        /// </summary>
        IUnityContainer IContainerAccessor.Container
        {
            get { return Container; }
        }

        #endregion

        /// <summary>
        /// Handles the Start event of the Application control.
        /// 处理应用程序控件的开始事件。
        /// </summary>
        /// <param name="sender">The source of the event.事件源</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.包含数据的事件实例</param>
        protected void Application_Start(object sender, EventArgs e)
        {
            // 确定当前环境
            var env = (EnvSection)ConfigurationManager.GetSection("env") ?? "dev";
            GlobalInfo.Environment = env;

            //System.Diagnostics.Debug.WriteLine($"当前env：{env}");

            //ConfigurationManager.AppSettings.Set("TestKey", "xxxxxxxxxx");


            //先处理appSettings.json文件和Config/appSettings.{env}.json，加载AppSettings
            AppSettingsLoader.LoadConfig("Config/appSettings.json");
            AppSettingsLoader.LoadConfig($"Config/appSettings.{env}.json");


            // Register the relevant types for the
            // container here through classes or configuration
            // register the container in the container property
            // 通过类或配置来针对容器注册相关类型
            // 在容器属性中注册容器
            Container = new UnityContainer();

            // 加载主 Unity 配置 + 当前环境覆盖配置。
            // 注意：这里先让 XML 完成类型映射注册，再把外置连接串注册成 Unity 命名实例。
            // 这样既保留旧数据访问层的构造函数依赖名，也避免真实连接串继续写在 UnityCfg*.xml 中。
            var section = (UnityConfigurationSection)ConfigurationManager.GetSection("unity");
            section.Configure(Container);
            UnityConfigLoader.LoadUnityConfig(Container, $"Config/UnityCfg.{env}.xml");

            // 从仓库外部读取语义化连接串 Portal，并映射为旧代码仍使用的 connectionString。
            // LoadPortalConnectionString 内部会校验外置文件存在；环境变量只覆盖连接串值，不绕过文件校验。
            ExternalConnectionStringLoadResult portalConnectionString =
                ExternalConnectionStringLoader.LoadPortalConnectionString(env);
            Container.RegisterInstance(
                ExternalConnectionStringLoader.UnityConnectionStringName,
                portalConnectionString.ConnectionString);
            System.Diagnostics.Debug.WriteLine(
                $"Loaded connection string '{ExternalConnectionStringLoader.LogicalConnectionStringName}' from {portalConnectionString.Source}");

            // 启动期最小自检：确认关键数据服务和环境覆盖字符串都能从容器解析。
            var usersDbService = Container.Resolve<IUsersDb>();
            System.Diagnostics.Debug.WriteLine($"Resolved IUsersDb: {usersDbService?.GetType().Name}");
            string testStr = Container.Resolve<string>("testStr");
            System.Diagnostics.Debug.WriteLine($"Resolved testStr: '{testStr}'");


        }

        /// <summary>
        /// Handles the End event of the Application control.
        /// 处理应用程序的结束事件。
        /// </summary>
        /// <param name="sender">The source of the event.事件源</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.包含数据的事件实例</param>
        protected void Application_End(object sender, EventArgs e)
        {
            // 释放容器
            if (Container != null)
            {
                Container.Dispose();
            }
        }

        /// <summary>
        /// Builds the item with current context.
        /// 使用当前上下文构建条目。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ctrl">
        /// The control.控件
        /// </param>
        /// <remarks>
        ///   <para>这段代码定义了一个静态方法 `BuildItemWithCurrentContext&lt;T&gt;`，其中 `T` 是泛型类型参数。这个方法利用了Unity容器（`Container`）的 `BuildUp` 方法，来进行对已存在的对象（在这里是 `Control` 类型的 `ctrl` 参数）的依赖注入。</para>
        ///   <para>`BuildUp` 方法的作用是根据已注册的类型映射和服务定位策略，动态地向已存在的对象注入依赖关系。换句话说，即使对象已经被创建，Unity仍然可以为其添加或更新依赖属性或字段的值。</para>
        ///   <para>在以下情况下，可能需要调用这个方法：</para>
        ///   <para>1. **后期依赖注入**：当你有一个已存在的对象实例（如页面上的控件 `ctrl`），但你希望在其原有基础上添加或更新依赖关系时，可以调用此方法。</para>
        ///   <para>2. **对象重构**：当对象的结构发生变化，新增了依赖属性，而这些属性在创建对象时未被注入，可以通过 `BuildUp` 方法来补充注入。</para>
        ///   <para>3. **刷新依赖**：在运行时，如果某些依赖项的值发生了变化，可以通过 `BuildUp` 来重新注入依赖，使对象使用最新的依赖项。</para>
        ///   <para>举个例子，如果你的 `T` 是一个实现了特定接口的自定义控件类，这个控件有一些服务依赖（例如，`ILogger` 或 `IDataService`），尽管控件已经创建，但还没有注入这些依赖，此时调用 `BuildItemWithCurrentContext&lt;YourCustomControlType&gt;(yourControlInstance)`，就可以利用Unity容器将这些依赖注入到已存在的控件实例中。</para>
        /// </remarks>
        public static void BuildItemWithCurrentContext<T>(Control ctrl)
        {
            Container.BuildUp(typeof(T), ctrl);
        }

        /// <summary>
        ///   Handles the BeginRequest event of the Application control.
        ///   The Application_BeginRequest method is an ASP.NET event that executes
        ///   on each web request into the portal application.  The below method
        ///   obtains the current tabIndex and TabId from the querystring of the
        ///   request -- and then obtains the configuration necessary to process
        ///   and render the request.
        ///
        ///   处理 Application 控件的 BeginRequest 事件。
        ///   Application_BeginRequest 方法是 ASP.NET 事件，它在每个 Web 请求到达门户应用程序时执行。
        ///   下面的方法从请求的查询字符串中获取当前的 tabIndex 和 TabId，然后获取处理和呈现请求所需的配置。
        /// </summary>
        /// <remarks>
        ///   This portal configuration is stored within the application's "Context"
        ///   object -- which is available to all pages, controls and components
        ///   during the processing of a single request.
        ///
        ///   此门户配置存储在应用程序的 "Context" 对象中，该对象在处理单个请求期间对所有页面、控件和组件可用。
        /// </remarks>
        /// <param name = "sender">The source of the event. 事件源。</param>
        /// <param name = "e">The <see cref = "System.EventArgs" /> instance containing the event data. 包含事件数据的 <see cref="System.EventArgs" /> 实例。</param>
        protected void Application_BeginRequest(Object sender, EventArgs e)
        {
            //string TestKey = ConfigurationManager.AppSettings.Get("TestKey").ToString();
            //System.Diagnostics.Debug.WriteLine($"获取TestKey：{TestKey}");


            int tabIndex = GetIntParam("tabindex", 0);
            int tabId = GetIntParam("tabid", 1);

            var portalConfig = Container.Resolve<IGlobalsDb>();
            var tabsConfig = Container.Resolve<ITabsDb>();
            var modulesConfig = Container.Resolve<IModulesDb>();
            var moduleDefConfig = Container.Resolve<IModuleDefsDb>();

            // 构建并将 PortalSettings 对象添加到当前 Context
            Context.Items["PortalSettings"] = new PortalSettings(tabIndex, tabId, portalConfig, tabsConfig, modulesConfig, moduleDefConfig);

            SetLanguage();

            // 辅助方法，用于验证有效的区域性
            bool IsValidCulture(string cultureName)
            {
                return CultureInfo.GetCultures(CultureTypes.AllCultures)?.Any(c => c.Name == cultureName) ?? false;
            }

            // 辅助方法，用于设置区域性
            void SetCulture(string cultureName)
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(cultureName);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(cultureName);
            }

            // 辅助方法，用于获取带有默认值的整数参数
            int GetIntParam(string paramName, int defaultValue)
            {
                string rawValue = Request.QueryString[paramName] ?? Request.Unvalidated.Form[paramName];
                if (int.TryParse(rawValue, out int value))
                {
                    return value;
                }
                return defaultValue;
            }

            // 辅助方法，用于设置语言
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
        ///   Handles the AuthenticateRequest event of the Application control.
        ///   If the client is authenticated with the application, then determine
        ///   which security roles he/she belongs to and replace the "User" intrinsic
        ///   with a custom IPrincipal security object that permits "User.IsInRole"
        ///   role checks within the application
        ///
        ///   处理应用程序的 AuthenticateRequest 事件。
        ///   如果客户端已在应用程序中进行身份验证，则确定其所属的安全角色，
        ///   并用自定义的 IPrincipal 安全对象替换 "User" 内置对象，
        ///   允许应用程序内的 "User.IsInRole" 角色检查。
        /// </summary>
        /// <remarks>
        ///   Roles are cached in the browser in an in-memory encrypted cookie.  If the
        ///   cookie doesn't exist yet for this session, create it.
        ///
        ///   角色以内存中的加密Cookie形式缓存在浏览器中。如果此会话尚未存在该Cookie，将创建它。
        /// </remarks>
        /// <param name = "sender">The source of the event. 事件源。</param>
        /// <param name = "e">The <see cref = "System.EventArgs" /> instance containing the event data. 包含事件数据的 <see cref="System.EventArgs" /> 实例。</param>
        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {
            if (Request.IsAuthenticated)
            {
                string[] roles;

                // 如果角色 Cookie 不存在或为空，创建角色 Cookie
                if (string.IsNullOrEmpty(Request.Cookies["portalroles"]?.Value))
                {
                    // 从 UserRoles 表获取角色并添加到 Cookie
                    var usersDb = Container.Resolve<IUsersDb>();
                    roles = usersDb.GetRoleNamesByUser(User.Identity.Name).ToArray();

                    // 创建角色字符串
                    string roleStr = string.Join(";", roles);

                    // 创建并加密票据 //#todo 此处后期需要调整，使其可HIA方式配置化
                    var ticket = new FormsAuthenticationTicket(
                        1, // 版本
                        Context.User.Identity.Name, // 用户名
                        DateTime.Now, // 发行时间
                        DateTime.Now.AddHours(1), // 每小时过期
                        false, // 不保持 Cookie
                        roleStr // 角色
                    );

                    string encryptedTicket = FormsAuthentication.Encrypt(ticket);

                    // 发送 Cookie 到客户端
                    Response.Cookies.Add(new HttpCookie("portalroles", encryptedTicket)
                    {
                        //#todo 此处后期需要调整，包括Domain设置。因为需要兼容子站点以及虚拟目录/应用程序
                        Path = "/",
                        Expires = DateTime.Now.AddMinutes(1)
                    });
                }
                else
                {
                    // 从角色 Cookie 获取角色
                    FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(Context.Request.Cookies["portalroles"].Value);

                    // 将角色数据的字符串表示转换为字符串数组
                    roles = ticket.UserData.Split(';');

                    // 移除空白项
                    roles = roles.Where(r => !string.IsNullOrWhiteSpace(r)).ToArray();
                }

                // 向请求中添加自定义的 Principal，其中包含身份验证票据中的角色信息 #todo 建立自定义Principal，引入HIA的SysUser和BizUser
                Context.User = new GenericPrincipal(Context.User.Identity, roles);
            }
        }

        /// <summary>
        ///   This method returns the correct relative path when installing
        ///   the portal on a root web site instead of virtual directory
        ///
        ///   当在根网站而不是虚拟目录上安装时，此方法返回正确的相对路径。
        ///
        /// </summary>
        /// <param name = "request">The request. 请求对象。</param>
        /// <returns>The application path 应用程序路径</returns>
        public static string GetApplicationPath(HttpRequest request)
        {
            // 使用 null 合并运算符，如果为 null 或空，则返回 ""
            return request.ApplicationPath?.TrimEnd('/') ?? "";
        }
    }
}
