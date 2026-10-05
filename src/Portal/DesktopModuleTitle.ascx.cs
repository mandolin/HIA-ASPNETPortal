using System;
using System.Globalization;
using System.Web;
using System.Web.UI;
using Resources;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>桌面模块标题栏控件，负责显示模块标题和可选编辑入口。</zh-CN>
    ///   <en>Desktop module header control that renders a module title and optional edit action.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P7.4 起标记从旧 table 标题栏切换为语义化容器；权限判断和编辑链接生成仍沿用旧模块配置。</zh-CN>
    ///   <en>Starting with P7.4, the markup changes from the legacy table title bar to semantic containers, while permission checks and edit-link generation continue to use the legacy module configuration. P8.3 further separates the title and action areas so themes can style module actions consistently.</en>
    /// </lang>
    /// </remarks>
    public partial class DesktopModuleTitle : UserControl
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑页面打开目标窗口；为空时使用浏览器默认行为。</zh-CN>
        ///   <en>Target window for the edit page; when empty, the browser default behavior is used.</en>
        /// </lang>
        /// </summary>
        public string EditTarget;

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前用户可编辑模块时显示的编辑入口文本。</zh-CN>
        ///   <en>Edit-action text shown when the current user can edit the module.</en>
        /// </lang>
        /// </summary>
        public string EditText;

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块编辑页面的相对 URL，不包含当前模块 ID 查询参数。</zh-CN>
        ///   <en>Relative URL for the module edit page, excluding the current module-id query parameter.</en>
        /// </lang>
        /// </summary>
        public string EditUrl;

        /// <summary>
        /// <lang>
        ///   <zh-CN>根据父模块配置写入标题，并在用户具备编辑权限时显示编辑入口。</zh-CN>
        ///   <en>Writes the title from the parent module configuration and shows the edit action when the user has permission.</en>
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
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>PortalSettings 由页面生命周期预先放入当前请求上下文；标题控件只读取快照，不主动重新加载门户配置。</zh-CN>
            //   <en>PortalSettings is prepared earlier in the page lifecycle and stored in the current request context; the title control reads that snapshot instead of reloading Portal configuration.</en>
            // </lang>
            var portalSettings = PortalContext.GetPortalSettings();

            // <lang>
            //   <zh-CN>标题控件必须挂在模块控件下方，才能读取模块标题、模块 ID 和编辑角色；这是旧 Web Forms 模块容器契约的一部分。</zh-CN>
            //   <en>The title control must be hosted under a module control so it can read the module title, module id, and edit roles; this is part of the legacy Web Forms module-container contract.</en>
            // </lang>
            var portalModule = (IPortalModuleControl) Parent;

            // <lang>
            //   <zh-CN>每次加载都按当前模块配置重写标题，并默认隐藏动作区；后续条件满足时再显式打开编辑入口。</zh-CN>
            //   <en>Each load writes the title from the current module configuration and hides the action area by default; the edit action is then explicitly enabled only when all conditions pass.</en>
            // </lang>
            ModuleTitle.Text = portalModule.ModuleConfiguration.ModuleTitle;
            ModuleActions.Visible = false;
            EditButton.Visible = false;

            // <lang>
            //   <zh-CN>标题语义：把模块标题标注为 ARIA 标题并给出层级。这里用 `role="heading"` + `aria-level` 而不改用原生
            //   `h1`/`h2` 元素，原因是 Web Forms 的 `asp:Label` 把标签名写死为 `span`，要换成原生标题必须新建自定义渲染控件
            //   （重写 `Render` 并手工输出全部属性），风险与回归面远大于收益。ARIA 标题是 WCAG 认可的等价语义，且**不改动
            //   class 与元素名**，因此视觉零变化 —— 已核对 6 套皮肤 CSS 无任何 `[role]` / `[aria-]` 选择器。</zh-CN>
            //   <en>Heading semantics: the module title is marked up as an ARIA heading with a level. `role="heading"` plus
            //   `aria-level` is used instead of a native `h1`/`h2` because Web Forms hard-codes `span` as the `asp:Label` tag
            //   name, and a native heading would require a new custom-rendering control (overriding `Render` and emitting every
            //   attribute by hand) whose risk and regression surface far exceed the benefit. An ARIA heading is a
            //   WCAG-recognized equivalent and it leaves the class and element name untouched, so the visual result is
            //   unchanged — all six skins were checked and contain no `[role]` or `[aria-]` selectors.</en>
            // </lang>
            ModuleTitle.Attributes["role"] = "heading";

            // <lang>
            //   <zh-CN>层级按页面上下文二分：前台桌面（`DesktopDefault.aspx`）是页面主内容入口，用 1 级；后台管理页
            //   （`~/Admin/...`）本身已有一层页面主标题，模块标题降为 2 级，避免同一页面出现两个 1 级标题。</zh-CN>
            //   <en>The level is decided by the page context: the front-office desktop (`DesktopDefault.aspx`) is the page's
            //   primary content entry and uses level 1, while admin pages (`~/Admin/...`) already carry a page-level main title,
            //   so the module title drops to level 2 and a page never shows two level-1 headings.</en>
            // </lang>
            ModuleTitle.Attributes["aria-level"] = IsAdminPage() ? "2" : "1";

            // <lang>
            //   <zh-CN>编辑入口同时受控件配置、全局强制显示开关和模块编辑角色约束；没有实际文本时也必须隐藏，避免 P7 主题渲染空按钮。</zh-CN>
            //   <en>The edit action is constrained by control configuration, the global always-show switch, and module edit roles; it also stays hidden without text so P7 themes do not render empty buttons.</en>
            // </lang>
            if (!string.IsNullOrWhiteSpace(EditText) &&
                (portalSettings.AlwaysShowEditButton ||
                 PortalSecurity.IsInRoles(portalModule.ModuleConfiguration.AuthorizedEditRoles)))
            {
                // <lang>
                //   <zh-CN>旧模块编辑页通过 <c>mid</c> 查询参数定位模块实例；标题栏只追加当前模块 ID，不额外推断返回地址。</zh-CN>
                //   <en>Legacy module edit pages locate the module instance through the <c>mid</c> query parameter; the title bar only appends the current module id and does not infer return URLs.</en>
                // </lang>
                EditButton.Text = EditText;
                EditButton.NavigateUrl = EditUrl + "?mid=" + portalModule.ModuleId;
                EditButton.Target = EditTarget;
                EditButton.ToolTip = string.Format(CultureInfo.CurrentCulture, lang.ModuleTitle_EditToolTipFormat, EditText);
                EditButton.Visible = true;
                ModuleActions.Visible = true;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前页面是否位于后台管理区（`~/Admin/` 路径前缀）。</zh-CN>
        ///   <en>Determines whether the current page lives in the admin area (the `~/Admin/` path prefix).</en>
        /// </lang>
        /// </summary>
        private bool IsAdminPage()
        {
            // <lang>
            //   <zh-CN>取 `AppRelativeVirtualPath` 而不是请求 URL：后者含主机名与调试端口，前台换端口即失配；前者是稳定的应用相对路径。</zh-CN>
            //   <en>`AppRelativeVirtualPath` is read rather than the request URL: the latter carries the host name and debug
            //   port and would stop matching whenever the front-office port changes, while the former is a stable app-relative path.</en>
            // </lang>
            string path = Page != null ? Page.AppRelativeVirtualPath : string.Empty;
            return path.StartsWith("~/Admin/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
