using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>显示讨论主题并允许已授权编辑者发布主题或回复的页面。</zh-CN>
    ///   <en>Page that displays a discussion topic and allows authorized editors to post a topic or reply.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>此页面是旧 DesktopModule 的典型 code-behind：读取请求中的模块和消息标识，按模块编辑权限切换编辑 UI，并在展示前对历史 HTML 编码文本做规范化。P15.2 将它作为模块页面注释样例之一。</zh-CN>
    ///   <en>This page is a typical legacy DesktopModule code-behind: it reads module and message identifiers from the request, switches editing UI by module edit permission, and normalizes historically HTML-encoded text before display. P15.2 uses it as one module-page annotation sample.</en>
    /// </lang>
    /// </remarks>
    public partial class DiscussDetails : PortalPage<DiscussDetails>
    {
        private int itemId;
        private int moduleId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>讨论数据访问服务。</zh-CN>
        ///   <en>Discussion data-access service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IDiscussionsDb DiscussionDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块编辑权限服务。</zh-CN>
        ///   <en>Module edit-authorization service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化讨论请求；任何访客可读取现有主题，只有模块编辑者可创建或回复。</zh-CN>
        ///   <en>Initializes a discussion request. Any visitor may read an existing topic, while only module editors may create or reply.</en>
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
            //   <zh-CN>所有路径先统一读取并校验请求标识；非法模块或消息标识直接进入受控拒绝页，不继续触碰数据绑定。</zh-CN>
            //   <en>All paths first read and validate request identifiers; invalid module or message identifiers go to the controlled denial page before any data binding continues.</en>
            // </lang>
            if (!TryReadRequestIdentifiers())
            {
                return;
            }

            // <lang>
            //   <zh-CN>新增主题或回复需要模块编辑权限；读现有消息不要求编辑权限，但稍后会隐藏回复入口。</zh-CN>
            //   <en>Creating a topic or reply requires module edit permission; reading an existing message does not, but the reply entry is hidden later.</en>
            // </lang>
            bool canEdit = PortalSecurity.HasEditPermissions(moduleId);
            if (itemId == 0)
            {
                if (!canEdit)
                {
                    PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                    return;
                }

                EditPanel.Visible = true;
                ButtonPanel.Visible = false;
                return;
            }

            // <lang>
            //   <zh-CN>首次加载时绑定当前消息；回发时保留控件状态，避免覆盖用户正在编辑的内容。</zh-CN>
            //   <en>Bind the current message only on first load; on postback, keep control state so user edits are not overwritten.</en>
            // </lang>
            if (!Page.IsPostBack && !BindData())
            {
                return;
            }

            if (!canEdit)
            {
                ReplyBtn.Visible = false;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>打开回复编辑区；仅模块编辑者可执行。</zh-CN>
        ///   <en>Opens the reply editor; only module editors may perform this action.</en>
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
        protected void ReplyBtn_Click(object sender, EventArgs e)
        {
            if (!TryAuthorizeEditorForCurrentItem())
            {
                return;
            }

            EditPanel.Visible = true;
            ButtonPanel.Visible = false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建主题或回复，并使用 HTML 编码存储普通用户文本。</zh-CN>
        ///   <en>Creates a topic or reply and stores ordinary user text HTML-encoded.</en>
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
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            if (!TryAuthorizeEditorForCurrentItem())
            {
                return;
            }

            // <lang>
            //   <zh-CN>旧讨论模块以“写入时编码、读取时规范化”的方式保存普通文本；这里继续保持该兼容语义，不允许把原始 HTML 作为讨论正文写入。</zh-CN>
            //   <en>The legacy discussion module stores ordinary text using "encode on write, normalize on read"; this keeps that compatibility behavior and does not allow raw HTML as discussion body content.</en>
            // </lang>
            itemId = DiscussionDB.AddMessage(moduleId, itemId, User.Identity.Name,
                Server.HtmlEncode(TitleField.Text), Server.HtmlEncode(BodyField.Text));
            EditPanel.Visible = false;
            ButtonPanel.Visible = true;
            BindData();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>取消回复编辑；仅模块编辑者可执行。</zh-CN>
        ///   <en>Cancels reply editing; only module editors may perform this action.</en>
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
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            if (!TryAuthorizeEditorForCurrentItem())
            {
                return;
            }

            EditPanel.Visible = false;
            ButtonPanel.Visible = true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取并校验当前请求中的模块标识和可选消息标识。</zh-CN>
        ///   <en>Reads and validates the module identifier and optional message identifier from the current request.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>标识可用于后续授权和数据读取时为 <c>true</c>；非法请求已重定向时为 <c>false</c>。</zh-CN>
        ///   <en><c>true</c> when identifiers may be used for later authorization and data reads; <c>false</c> after invalid requests are redirected.</en>
        /// </l>
        /// </returns>
        private bool TryReadRequestIdentifiers()
        {
            // <lang>
            //   <zh-CN>模块标识是权限检查的根；缺失或非法时不能继续读取消息，避免跨模块探测。</zh-CN>
            //   <en>The module identifier is the root of permission checks; when it is missing or invalid, message reads must not continue, preventing cross-module probing.</en>
            // </lang>
            if (!PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["Mid"], out moduleId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            string requestedItemId = Request.Params["ItemId"];
            if (string.IsNullOrWhiteSpace(requestedItemId))
            {
                itemId = 0;
                return true;
            }

            // <lang>
            //   <zh-CN>消息标识只接受正整数；新增主题使用空 ItemId 表达，不接受零或负数伪装。</zh-CN>
            //   <en>The message identifier accepts positive integers only; new topics are represented by an empty ItemId, not by zero or negative values.</en>
            // </lang>
            if (!PortalNavigationPolicy.TryReadPositiveInt32(requestedItemId, out itemId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>确认当前请求可对目标模块或消息执行编辑动作。</zh-CN>
        ///   <en>Confirms that the current request may perform an edit action against the target module or message.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>已通过模块权限和消息归属校验时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when module permission and message ownership checks have passed.</en>
        /// </l>
        /// </returns>
        private bool TryAuthorizeEditorForCurrentItem()
        {
            if (!TryReadRequestIdentifiers() || !PortalSecurity.HasEditPermissions(moduleId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            // <lang>
            //   <zh-CN>新增主题没有既有消息可校验归属，前面的模块编辑权限就是完整授权条件。</zh-CN>
            //   <en>A new topic has no existing message whose ownership can be checked, so the earlier module edit permission is the complete authorization condition.</en>
            // </lang>
            if (itemId == 0)
            {
                return true;
            }

            // <lang>
            //   <zh-CN>回复必须确认消息真实存在并归属于当前模块，避免用合法权限编辑其他模块的消息。</zh-CN>
            //   <en>Replies must confirm that the message exists and belongs to the current module, avoiding edits to another module's message with otherwise valid permissions.</en>
            // </lang>
            IDiscussionItem item = DiscussionDB.GetSingleMessage(itemId);
            if (item == null || item.ModuleID != moduleId)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定当前消息详情，并准备回复标题和导航按钮状态。</zh-CN>
        ///   <en>Binds the current message detail and prepares the reply title and navigation button state.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>消息存在且属于当前模块时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the message exists and belongs to the current module.</en>
        /// </l>
        /// </returns>
        private bool BindData()
        {
            IDiscussionItem item = DiscussionDB.GetSingleMessage(itemId);
            if (item == null || item.ModuleID != moduleId)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            // <lang>
            //   <zh-CN>历史讨论数据可能已经 HTML 编码；展示前先解码再编码，尽量得到稳定普通文本输出。</zh-CN>
            //   <en>Legacy discussion data may already be HTML-encoded; decode and then encode before display to produce stable ordinary-text output.</en>
            // </lang>
            Subject.Text = EncodeDisplayText(item.Title);
            Body.Text = EncodeDisplayText(item.Body);
            CreatedByUser.Text = EncodeDisplayText(item.CreatedByUser ?? "匿名");
            CreatedDate.Text = item.CreatedDate.HasValue ? item.CreatedDate.Value.ToString("d") : "未知时间";
            TitleField.Text = ReTitle(Server.HtmlDecode(item.Title ?? string.Empty));
            prevItem.Visible = false;
            nextItem.Visible = false;
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为回复标题补充历史约定的 <c>Re: </c> 前缀。</zh-CN>
        ///   <en>Adds the legacy <c>Re: </c> prefix for reply titles.</en>
        /// </lang>
        /// </summary>
        /// <param name="title">
        /// <l>
        ///   <zh-CN>原始或已解码标题。</zh-CN>
        ///   <en>Raw or decoded title.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>空标题或已带前缀的标题原样返回；其他标题补充回复前缀。</zh-CN>
        ///   <en>Empty titles or titles already carrying the prefix are returned unchanged; other titles receive the reply prefix.</en>
        /// </l>
        /// </returns>
        private string ReTitle(string title)
        {
            return string.IsNullOrEmpty(title) || title.StartsWith("Re: ", StringComparison.Ordinal)
                ? title
                : "Re: " + title;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把历史可能已编码的讨论文本规范化为安全显示文本。</zh-CN>
        ///   <en>Normalizes discussion text that may have been historically encoded into safe display text.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>来自数据库或业务对象的候选显示值。</zh-CN>
        ///   <en>Candidate display value from the database or business object.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可赋给 Label 的 HTML 编码文本。</zh-CN>
        ///   <en>HTML-encoded text suitable for assignment to a Label.</en>
        /// </l>
        /// </returns>
        private string EncodeDisplayText(string value)
        {
            return Server.HtmlEncode(Server.HtmlDecode(value ?? string.Empty));
        }
    }
}
