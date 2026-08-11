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
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前请求中的链接条目标识；0 表示创建新链接。</zh-CN>
        ///   <en>Link item identifier for the current request; 0 means a new link is being created.</en>
        /// </lang>
        /// </summary>
        private int itemId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前请求中的模块实例标识，是编辑权限和链接归属校验的共同边界。</zh-CN>
        ///   <en>Module instance identifier for the current request, forming the shared boundary for edit permission and link ownership checks.</en>
        /// </lang>
        /// </summary>
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
            //   <zh-CN>item 是通过权限和归属校验后的链接快照；新增链接时为空。</zh-CN>
            //   <en>item is the link snapshot after permission and ownership validation; it is null when creating a new link.</en>
            // </lang>
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
                    // <lang>
                    //   <zh-CN>显示顺序按旧整数文本回填；空值保留为空字符串交由保存阶段校验。</zh-CN>
                    //   <en>The display order is filled as legacy integer text; null stays blank and is validated during save.</en>
                    // </lang>
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
            //   <zh-CN>保存前重新初始化请求，确保链接编辑不能依赖旧 ViewState 绕过权限或归属检查。</zh-CN>
            //   <en>The request is initialized again before saving so link editing cannot rely on stale ViewState to bypass permission or ownership checks.</en>
            // </lang>
            ILinkItem item;
            if (!TryInitializeRequest(out item) || !Page.IsValid)
            {
                return;
            }

            // <lang>
            //   <zh-CN>viewOrder 是旧链接模块的整数排序值；解析失败时不进入数据层。</zh-CN>
            //   <en>viewOrder is the legacy Links module integer ordering value; parse failures never reach the data layer.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>url 为必填浏览地址，mobileUrl 为可选兼容地址；两者都保存归一化结果。</zh-CN>
            //   <en>url is the required browse address and mobileUrl is the optional compatibility address; both persist normalized results.</en>
            // </lang>
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
            //   <zh-CN>删除路径重新校验模块权限和链接归属，避免通过 ItemId 删除其他模块链接。</zh-CN>
            //   <en>The delete path revalidates module permission and link ownership, avoiding deletion of another module's link by ItemId.</en>
            // </lang>
            ILinkItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (itemId != 0)
            {
                // <lang>
                //   <zh-CN>只有既有链接且归属已确认时，才调用数据访问层删除。</zh-CN>
                //   <en>The data-access delete is called only for an existing link whose ownership has been confirmed.</en>
                // </lang>
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
            //   <zh-CN>取消不保存字段，但仍确认请求合法后才执行安全回跳。</zh-CN>
            //   <en>Cancel does not persist fields, but still confirms the request is valid before performing the safe return.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>输出参数先清空，保证失败路径不会把链接快照暴露给调用方。</zh-CN>
            //   <en>The output parameter is cleared first so failure paths cannot expose a link snapshot to callers.</en>
            // </lang>
            item = null;
            // <lang>
            //   <zh-CN>模块标识同时限定链接编辑权限和新增链接归属；非法或无权限请求统一拒绝。</zh-CN>
            //   <en>The module identifier constrains both link edit permission and new-link ownership; invalid or unauthorized requests are denied uniformly.</en>
            // </lang>
            if (!PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["Mid"], out moduleId) ||
                !PortalSecurity.HasEditPermissions(moduleId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            // <lang>
            //   <zh-CN>ItemId 缺失表示新增链接；非空值必须是正整数，避免跨模块枚举异常输入。</zh-CN>
            //   <en>A missing ItemId means a new link; non-empty values must be positive integers to avoid cross-module enumeration with malformed input.</en>
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
                //   <zh-CN>新增路径没有旧链接可校验归属，模块编辑权限即完整授权条件。</zh-CN>
                //   <en>The creation path has no legacy link for ownership validation, so module edit permission is the complete authorization condition.</en>
                // </lang>
                return true;
            }

            // <lang>
            //   <zh-CN>既有链接必须按 ItemId 读取并比对模块归属，避免直接对象引用。</zh-CN>
            //   <en>Existing links must be read by ItemId and compared against module ownership, avoiding direct object references.</en>
            // </lang>
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
                // <lang>
                //   <zh-CN>移动端兼容地址允许为空；空输入保存为空字符串而不是触发校验错误。</zh-CN>
                //   <en>The mobile compatibility URL may be blank; blank input is saved as an empty string instead of causing a validation error.</en>
                // </lang>
                normalizedUrl = string.Empty;
                return true;
            }

            // <lang>
            //   <zh-CN>非空兼容地址仍必须通过统一浏览地址策略，保持桌面和移动链接安全边界一致。</zh-CN>
            //   <en>Non-empty compatibility URLs must still pass the shared browse-url policy, keeping desktop and mobile link safety boundaries aligned.</en>
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
            //   <zh-CN>校验提示固定来自服务器端分支，避免把不合规 URL 或排序输入回显为错误详情。</zh-CN>
            //   <en>The validation notice comes from fixed server-side branches, avoiding echoing rejected URLs or ordering input as error details.</en>
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
            //   <zh-CN>旧创建人文本显示前执行解码再编码，兼容历史实体存储并保持输出安全。</zh-CN>
            //   <en>Legacy creator text is decoded before encoding for display, supporting historical entity storage while keeping output safe.</en>
            // </lang>
            return Server.HtmlEncode(Server.HtmlDecode(value ?? string.Empty));
        }
    }
}
