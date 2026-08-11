using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>编辑公告模块条目的页面。</zh-CN>
    ///   <en>Page for editing announcement-module items.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该页面属于旧内容模块编辑入口，继续使用 WebForms 事件模型；安全边界集中在模块编辑权限、条目归属校验、可选链接归一化和站内安全回跳。</zh-CN>
    ///   <en>This page is a legacy content-module editing entry and continues to use the WebForms event model; its safety boundary centers on module edit permission, item ownership checks, optional-link normalization and safe in-app return.</en>
    /// </lang>
    /// </remarks>
    public partial class EditAnnouncements : PortalPage<EditAnnouncements>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前请求中的公告条目标识；0 表示创建新公告。</zh-CN>
        ///   <en>Announcement item identifier for the current request; 0 means a new announcement is being created.</en>
        /// </lang>
        /// </summary>
        private int itemId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前请求中的模块实例标识，是编辑权限和条目归属校验的共同边界。</zh-CN>
        ///   <en>Module instance identifier for the current request, forming the shared boundary for edit permission and item ownership checks.</en>
        /// </lang>
        /// </summary>
        private int moduleId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>公告数据访问服务。</zh-CN>
        ///   <en>Announcement data-access service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IAnnouncementsDb AnnouncementsDB { private get; set; }

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
        ///   <zh-CN>初始化请求上下文、核验编辑权限和现有条目归属，并在首次访问时绑定表单。</zh-CN>
        ///   <en>Initializes request context, verifies edit permission and existing-item ownership, and binds the form on the first request.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发页面加载的 Web Forms 事件源。</zh-CN>
        ///   <en>The Web Forms event source that triggered page loading.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>页面加载事件参数；当前实现不读取其内容。</zh-CN>
        ///   <en>The page-load event arguments; the current implementation does not read them.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>item 是经过请求参数、权限和归属校验后的旧公告快照；新增路径保持为空。</zh-CN>
            //   <en>item is the legacy announcement snapshot after request-parameter, permission, and ownership validation; it remains null on the creation path.</en>
            // </lang>
            IAnnouncementItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                // <lang>
                //   <zh-CN>首次加载只在归属校验通过后回填旧公告字段，避免通过 ItemId 读取其他模块的条目。</zh-CN>
                //   <en>Initial binding fills legacy announcement fields only after ownership validation, avoiding reads of items from another module via ItemId.</en>
                // </lang>
                if (item != null)
                {
                    TitleField.Text = item.Title;
                    MoreLinkField.Text = item.MoreLink;
                    MobileMoreField.Text = item.MobileMoreLink;
                    DescriptionField.Text = item.Description;
                    // <lang>
                    //   <zh-CN>过期日期按旧页面使用短日期文本回填，保持当前区域性输入/显示兼容。</zh-CN>
                    //   <en>The expiry date is filled using the legacy page's short-date text, preserving current-culture input and display compatibility.</en>
                    // </lang>
                    ExpireField.Text = item.ExpireDate.HasValue ? item.ExpireDate.Value.ToShortDateString() : string.Empty;
                    CreatedBy.Text = EncodeDisplayText(item.CreatedByUser);
                    CreatedDate.Text = item.CreatedDate.HasValue ? item.CreatedDate.Value.ToShortDateString() : string.Empty;
                }

                // <lang>
                //   <zh-CN>回跳地址只记录经策略清洗后的站内地址；更新、删除和取消共用该安全回跳。</zh-CN>
                //   <en>The return URL stores only a policy-cleaned in-app address; update, delete and cancel share this safe return.</en>
                // </lang>
                ViewState["UrlReferrer"] = PortalNavigationPolicy.GetSafeReturnUrl(Request);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建或更新已授权公告，并回跳到当前应用内的安全地址。</zh-CN>
        ///   <en>Creates or updates an authorized announcement and returns to a safe URL inside the current application.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发保存命令的提交控件。</zh-CN>
        ///   <en>The submit control that raised the save command.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>保存命令事件参数；当前实现不读取额外状态。</zh-CN>
        ///   <en>The save-command event arguments; no additional state is read by the current implementation.</en>
        /// </l>
        /// </param>
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>保存前重新初始化请求，确保回发期间的模块权限和条目归属没有被 ViewState 替代。</zh-CN>
            //   <en>The request is initialized again before saving so postback authorization and item ownership are not replaced by ViewState.</en>
            // </lang>
            IAnnouncementItem item;
            if (!TryInitializeRequest(out item) || !Page.IsValid)
            {
                return;
            }

            // <lang>
            //   <zh-CN>expireDate 是当前文化解析后的公告到期时间，只在本次保存命令内使用。</zh-CN>
            //   <en>expireDate is the announcement expiry time parsed with the current culture and is used only within this save command.</en>
            // </lang>
            DateTime expireDate;
            if (!DateTime.TryParse(ExpireField.Text, out expireDate))
            {
                // <lang>
                //   <zh-CN>旧页面仍用当前文化解析日期；本轮只补低敏校验提示，不改变历史输入格式。</zh-CN>
                //   <en>The legacy page still parses dates using current culture; this pass only adds a low-sensitivity validation message and does not change historical input format.</en>
                // </lang>
                ShowValidationMessage("请输入有效的到期日期。");
                return;
            }

            // <lang>
            //   <zh-CN>moreLink 和 mobileMoreLink 保存已归一化的可选浏览地址；空输入会被折叠为空字符串。</zh-CN>
            //   <en>moreLink and mobileMoreLink hold normalized optional browse URLs; blank input is folded into an empty string.</en>
            // </lang>
            string moreLink;
            string mobileMoreLink;
            if (!TryNormalizeOptionalBrowseUrl(MoreLinkField.Text, out moreLink) ||
                !TryNormalizeOptionalBrowseUrl(MobileMoreField.Text, out mobileMoreLink))
            {
                ShowValidationMessage("“查看更多”链接只能使用站内地址或 HTTP(S) 地址。");
                return;
            }

            if (itemId == 0)
            {
                // <lang>
                //   <zh-CN>新增公告使用当前模块标识，创建人来自服务器端认证身份，不接受浏览器传入。</zh-CN>
                //   <en>New announcements use the current module id, and creator identity comes from the server-side authenticated principal rather than browser input.</en>
                // </lang>
                AnnouncementsDB.AddAnnouncement(moduleId, Context.User.Identity.Name, TitleField.Text, expireDate,
                    DescriptionField.Text, moreLink, mobileMoreLink);
            }
            else
            {
                // <lang>
                //   <zh-CN>更新路径已经在 `TryInitializeRequest` 中核验条目归属，避免跨模块编辑。</zh-CN>
                //   <en>The update path has already validated item ownership in `TryInitializeRequest`, avoiding cross-module edits.</en>
                // </lang>
                AnnouncementsDB.UpdateAnnouncement(itemId, Context.User.Identity.Name, TitleField.Text, expireDate,
                    DescriptionField.Text, moreLink, mobileMoreLink);
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除已核验归属的公告。</zh-CN>
        ///   <en>Deletes an announcement whose ownership has been verified.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发删除命令的提交控件。</zh-CN>
        ///   <en>The submit control that raised the delete command.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>删除命令事件参数；当前实现不读取额外状态。</zh-CN>
        ///   <en>The delete-command event arguments; no additional state is read by the current implementation.</en>
        /// </l>
        /// </param>
        protected void DeleteBtn_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>删除同样重新走初始化校验，避免仅凭隐藏字段或 ViewState 删除其他模块条目。</zh-CN>
            //   <en>Deletion also reruns initialization checks, avoiding deletion of another module's item based only on hidden fields or ViewState.</en>
            // </lang>
            IAnnouncementItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (itemId != 0)
            {
                // <lang>
                //   <zh-CN>新增路径没有持久化条目可删；只有既有条目且归属已核验时才调用数据层删除。</zh-CN>
                //   <en>The creation path has no persisted row to delete; the data layer is called only for an existing item whose ownership has been verified.</en>
                // </lang>
                AnnouncementsDB.DeleteAnnouncement(itemId);
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>放弃编辑并返回安全地址。</zh-CN>
        ///   <en>Cancels editing and returns to a safe URL.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发取消命令的提交控件。</zh-CN>
        ///   <en>The submit control that raised the cancel command.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>取消命令事件参数；当前实现不读取额外状态。</zh-CN>
        ///   <en>The cancel-command event arguments; no additional state is read by the current implementation.</en>
        /// </l>
        /// </param>
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>取消不保存字段，但仍重新确认请求合法，防止编辑页被用作开放重定向跳板。</zh-CN>
            //   <en>Cancel does not persist fields, but still revalidates the request so the edit page cannot be used as an open-redirect trampoline.</en>
            // </lang>
            IAnnouncementItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取请求参数、核验模块编辑权限并确认既有公告归属。</zh-CN>
        ///   <en>Reads request parameters, verifies module edit permission and confirms ownership of an existing announcement.</en>
        /// </lang>
        /// </summary>
        /// <param name="item">
        /// <l>
        ///   <zh-CN>通过校验后输出的既有公告；新增路径为空。</zh-CN>
        ///   <en>Existing announcement emitted after validation; null on the creation path.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求可继续处理时返回 <c>true</c>；非法参数或权限失败时已跳转。</zh-CN>
        ///   <en><c>true</c> when the request may continue; invalid parameters or authorization failures have already redirected.</en>
        /// </l>
        /// </returns>
        private bool TryInitializeRequest(out IAnnouncementItem item)
        {
            // <lang>
            //   <zh-CN>输出参数先清空，保证所有失败分支都不会向调用方暴露未授权公告快照。</zh-CN>
            //   <en>The output parameter is cleared first so no failure path exposes an unauthorized announcement snapshot to callers.</en>
            // </lang>
            item = null;
            // <lang>
            //   <zh-CN>模块标识既决定权限范围也决定新增条目归属；缺失或非法时直接拒绝。</zh-CN>
            //   <en>The module identifier determines both permission scope and ownership for new rows; missing or invalid values are denied immediately.</en>
            // </lang>
            if (!PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["Mid"], out moduleId) ||
                !PortalSecurity.HasEditPermissions(moduleId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            // <lang>
            //   <zh-CN>ItemId 是可选参数；缺失代表新增公告，非空时必须是正整数。</zh-CN>
            //   <en>ItemId is optional; absence represents a new announcement, and non-empty values must be positive integers.</en>
            // </lang>
            string requestedItemId = Request.Params["ItemId"];
            if (!string.IsNullOrWhiteSpace(requestedItemId) &&
                !PortalNavigationPolicy.TryReadPositiveInt32(requestedItemId, out itemId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            if (itemId == 0)
            {
                // <lang>
                //   <zh-CN>新增路径无需读取数据库条目，前面的模块编辑权限就是完整授权条件。</zh-CN>
                //   <en>The creation path does not read a database item, so the earlier module edit permission is the complete authorization condition.</en>
                // </lang>
                return true;
            }

            // <lang>
            //   <zh-CN>既有公告必须重新按 ItemId 读取并比对模块归属，避免跨模块直接对象引用。</zh-CN>
            //   <en>An existing announcement must be reloaded by ItemId and compared against module ownership, avoiding cross-module direct object references.</en>
            // </lang>
            item = AnnouncementsDB.GetSingleAnnouncement(itemId);
            if (item == null || item.ModuleId != moduleId)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化可为空的浏览链接。</zh-CN>
        ///   <en>Normalizes an optional browse URL.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>管理员输入的链接文本。</zh-CN>
        ///   <en>Link text entered by the administrator.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedUrl">
        /// <l>
        ///   <zh-CN>归一化后的站内或 HTTP(S) 地址。</zh-CN>
        ///   <en>Normalized in-app or HTTP(S) URL.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>空值或符合当前导航策略时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the value is empty or accepted by the current navigation policy.</en>
        /// </l>
        /// </returns>
        private bool TryNormalizeOptionalBrowseUrl(string value, out string normalizedUrl)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // <lang>
                //   <zh-CN>可选链接的空输入表示清空字段，不触发导航策略错误。</zh-CN>
                //   <en>Blank input for an optional link means clearing the field and does not raise a navigation-policy error.</en>
                // </lang>
                normalizedUrl = string.Empty;
                return true;
            }

            // <lang>
            //   <zh-CN>非空链接必须通过统一浏览地址策略，输出值用于后续保存而不是原始输入。</zh-CN>
            //   <en>Non-empty links must pass the shared browse-url policy, and the output value is saved instead of the raw input.</en>
            // </lang>
            return PortalNavigationPolicy.TryNormalizeBrowseUrl(value, Request, out normalizedUrl);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>显示页面级低敏校验提示。</zh-CN>
        ///   <en>Displays a low-sensitivity page-level validation message.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>提示文本。</zh-CN>
        ///   <en>Message text.</en>
        /// </l>
        /// </param>
        private void ShowValidationMessage(string message)
        {
            // <lang>
            //   <zh-CN>提示文本固定来自服务器端分支，不包含用户输入、物理路径或异常堆栈。</zh-CN>
            //   <en>The message text comes from server-side branches and contains no user input, physical paths, or exception stacks.</en>
            // </lang>
            ValidationMessage.Text = message;
            ValidationMessage.Visible = true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>编码旧记录展示文本。</zh-CN>
        ///   <en>Encodes legacy record display text.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>旧数据中的原始文本。</zh-CN>
        ///   <en>Raw text from legacy data.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>先解码再编码后的展示文本，避免历史已编码值重复显示实体。</zh-CN>
        ///   <en>Display text decoded then encoded so historically encoded values do not render entities twice.</en>
        /// </l>
        /// </returns>
        private string EncodeDisplayText(string value)
        {
            // <lang>
            //   <zh-CN>旧记录可能已经存储实体文本；显示前先解码再编码，得到稳定的一次编码输出。</zh-CN>
            //   <en>Legacy rows may already store entity text; decoding before encoding yields stable single-encoded output.</en>
            // </lang>
            return Server.HtmlEncode(Server.HtmlDecode(value ?? string.Empty));
        }
    }
}
