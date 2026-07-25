using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>编辑链接模块条目的页面。</zh-CN>
    ///   <en>Page for editing link-module items.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该页面沿用旧链接模块编辑入口；安全边界集中在模块编辑权限、链接条目归属、链接地址策略、显示顺序解析和站内安全回跳。</zh-CN>
    ///   <en>This page keeps the legacy link-module editing entry; its safety boundary centers on module edit permission, link-item ownership, URL policy, display-order parsing and safe in-app return.</en>
    /// </lang>
    /// </remarks>
    public partial class EditLinks : PortalPage<EditLinks>
    {
        private int itemId;
        private int moduleId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>链接数据访问服务。</zh-CN>
        ///   <en>Link data-access service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public ILinksDb LinkDB { private get; set; }

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
        protected void Page_Load(object sender, EventArgs e)
        {
            ILinkItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                if (item != null)
                {
                    // <lang>
                    //   <zh-CN>首次加载只在归属校验通过后回填旧链接字段，避免通过 ItemId 读取其他模块的条目。</zh-CN>
                    //   <en>Initial binding fills legacy link fields only after ownership validation, avoiding reads of items from another module via ItemId.</en>
                    // </lang>
                    TitleField.Text = item.Title;
                    DescriptionField.Text = item.Description;
                    UrlField.Text = item.Url;
                    MobileUrlField.Text = item.MobileUrl;
                    ViewOrderField.Text = item.ViewOrder.HasValue ? item.ViewOrder.Value.ToString() : string.Empty;
                    CreatedBy.Text = EncodeDisplayText(item.CreatedByUser);
                    CreatedDate.Text = item.CreatedDate.HasValue ? item.CreatedDate.Value.ToShortDateString() : string.Empty;
                }

                // <lang>
                //   <zh-CN>回跳地址只保存经策略清洗后的站内地址，供保存、删除和取消共用。</zh-CN>
                //   <en>The return URL stores only a policy-cleaned in-app address shared by save, delete and cancel actions.</en>
                // </lang>
                ViewState["UrlReferrer"] = PortalNavigationPolicy.GetSafeReturnUrl(Request);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建或更新已授权链接，并回跳到当前应用内的安全地址。</zh-CN>
        ///   <en>Creates or updates an authorized link and returns to a safe URL inside the current application.</en>
        /// </lang>
        /// </summary>
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            ILinkItem item;
            if (!TryInitializeRequest(out item) || !Page.IsValid)
            {
                return;
            }

            int viewOrder;
            if (!int.TryParse(ViewOrderField.Text, out viewOrder))
            {
                // <lang>
                //   <zh-CN>显示顺序只接受旧数据库可保存的整数值，失败时输出低敏校验提示。</zh-CN>
                //   <en>Display order accepts only integer values that the legacy database can persist; failures emit a low-sensitivity validation message.</en>
                // </lang>
                ShowValidationMessage("请输入有效的显示顺序。");
                return;
            }

            string url;
            string mobileUrl;
            if (!PortalNavigationPolicy.TryNormalizeBrowseUrl(UrlField.Text, Request, out url) ||
                !TryNormalizeOptionalBrowseUrl(MobileUrlField.Text, out mobileUrl))
            {
                // <lang>
                //   <zh-CN>链接地址沿用统一浏览地址策略，避免在编辑页绕过前台链接过滤。</zh-CN>
                //   <en>Link URLs reuse the shared browse-URL policy so the edit page cannot bypass front-end link filtering.</en>
                // </lang>
                ShowValidationMessage("链接地址只能使用站内地址或 HTTP(S) 地址。");
                return;
            }

            if (itemId == 0)
            {
                // <lang>
                //   <zh-CN>新增链接使用当前模块标识，创建人来自服务器端认证身份，不接受浏览器传入。</zh-CN>
                //   <en>New links use the current module id, and creator identity comes from the server-side authenticated principal rather than browser input.</en>
                // </lang>
                LinkDB.AddLink(moduleId, Context.User.Identity.Name, TitleField.Text, url, mobileUrl, viewOrder,
                    DescriptionField.Text);
            }
            else
            {
                // <lang>
                //   <zh-CN>更新路径已经在 `TryInitializeRequest` 中核验条目归属，避免跨模块编辑。</zh-CN>
                //   <en>The update path has already validated item ownership in `TryInitializeRequest`, avoiding cross-module edits.</en>
                // </lang>
                LinkDB.UpdateLink(itemId, Context.User.Identity.Name, TitleField.Text, url, mobileUrl, viewOrder,
                    DescriptionField.Text);
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除已核验归属的链接。</zh-CN>
        ///   <en>Deletes a link whose ownership has been verified.</en>
        /// </lang>
        /// </summary>
        protected void DeleteBtn_Click(object sender, EventArgs e)
        {
            ILinkItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (itemId != 0)
            {
                LinkDB.DeleteLink(itemId);
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>放弃编辑并返回安全地址。</zh-CN>
        ///   <en>Cancels editing and returns to a safe URL.</en>
        /// </lang>
        /// </summary>
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            ILinkItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取请求参数、核验模块编辑权限并确认既有链接归属。</zh-CN>
        ///   <en>Reads request parameters, verifies module edit permission and confirms ownership of an existing link.</en>
        /// </lang>
        /// </summary>
        /// <param name="item">
        /// <l>
        ///   <zh-CN>通过校验后输出的既有链接；新增路径为空。</zh-CN>
        ///   <en>Existing link emitted after validation; null on the creation path.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求可继续处理时返回 <c>true</c>；非法参数或权限失败时已跳转。</zh-CN>
        ///   <en><c>true</c> when the request may continue; invalid parameters or authorization failures have already redirected.</en>
        /// </l>
        /// </returns>
        private bool TryInitializeRequest(out ILinkItem item)
        {
            item = null;
            if (!PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["Mid"], out moduleId) ||
                !PortalSecurity.HasEditPermissions(moduleId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            string requestedItemId = Request.Params["ItemId"];
            if (!string.IsNullOrWhiteSpace(requestedItemId) &&
                !PortalNavigationPolicy.TryReadPositiveInt32(requestedItemId, out itemId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            if (itemId == 0)
            {
                return true;
            }

            item = LinkDB.GetSingleLink(itemId);
            if (item == null || item.ModuleId != moduleId)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化可为空的移动端链接地址。</zh-CN>
        ///   <en>Normalizes an optional mobile link URL.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>管理员输入的地址文本。</zh-CN>
        ///   <en>URL text entered by the administrator.</en>
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
                normalizedUrl = string.Empty;
                return true;
            }

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
            return Server.HtmlEncode(Server.HtmlDecode(value ?? string.Empty));
        }
    }
}
