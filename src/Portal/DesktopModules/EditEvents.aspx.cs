using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>编辑事件模块条目的页面。</zh-CN>
    ///   <en>Page for editing event-module items.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该页面沿用旧事件模块编辑入口；安全边界集中在模块编辑权限、事件条目归属校验、旧日期输入兼容和站内安全回跳。</zh-CN>
    ///   <en>This page keeps the legacy event-module editing entry; its safety boundary centers on module edit permission, event-item ownership validation, legacy date-input compatibility and safe in-app return.</en>
    /// </lang>
    /// </remarks>
    public partial class EditEvents : PortalPage<EditEvents>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前请求中的事件条目标识；0 表示创建新事件。</zh-CN>
        ///   <en>Event item identifier for the current request; 0 means a new event is being created.</en>
        /// </lang>
        /// </summary>
        private int itemId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前请求中的模块实例标识，是编辑权限和事件归属校验的共同边界。</zh-CN>
        ///   <en>Module instance identifier for the current request, forming the shared boundary for edit permission and event ownership checks.</en>
        /// </lang>
        /// </summary>
        private int moduleId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>事件数据访问服务。</zh-CN>
        ///   <en>Event data-access service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IEventsDb EventsDB { private get; set; }

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
            //   <zh-CN>item 是通过模块权限和归属校验后的事件快照；新增事件路径保持为空。</zh-CN>
            //   <en>item is the event snapshot after module permission and ownership validation; it remains null on the new-event path.</en>
            // </lang>
            IEventItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                if (item != null)
                {
                    // <lang>
                    //   <zh-CN>首次加载只在归属校验通过后回填旧事件字段，避免通过 ItemId 读取其他模块的条目。</zh-CN>
                    //   <en>Initial binding fills legacy event fields only after ownership validation, avoiding reads of items from another module via ItemId.</en>
                    // </lang>
                    TitleField.Text = item.Title;
                    DescriptionField.Text = item.Description;
                    // <lang>
                    //   <zh-CN>事件过期日期继续按旧页面短日期文本回填，保持当前文化兼容。</zh-CN>
                    //   <en>The event expiry date continues to fill with the legacy page's short-date text, preserving current-culture compatibility.</en>
                    // </lang>
                    ExpireField.Text = item.ExpireDate.HasValue ? item.ExpireDate.Value.ToShortDateString() : string.Empty;
                    CreatedBy.Text = EncodeDisplayText(item.CreatedByUser);
                    WhereWhenField.Text = item.WhereWhen;
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
        ///   <zh-CN>创建或更新已授权事件，并回跳到当前应用内的安全地址。</zh-CN>
        ///   <en>Creates or updates an authorized event and returns to a safe URL inside the current application.</en>
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
            //   <zh-CN>保存前重新初始化请求，确保事件归属与模块编辑权限来自服务器端实时校验。</zh-CN>
            //   <en>The request is initialized again before saving so event ownership and module edit permission come from live server-side validation.</en>
            // </lang>
            IEventItem item;
            if (!TryInitializeRequest(out item) || !Page.IsValid)
            {
                return;
            }

            // <lang>
            //   <zh-CN>expireDate 是当前文化解析得到的事件过期时间；解析失败只显示低敏提示，不写入数据库。</zh-CN>
            //   <en>expireDate is the event expiry time parsed with the current culture; parse failures only show a low-sensitivity notice and do not write to the database.</en>
            // </lang>
            DateTime expireDate;
            if (!DateTime.TryParse(ExpireField.Text, out expireDate))
            {
                // <lang>
                //   <zh-CN>旧页面仍用当前文化解析日期；失败时只显示低敏校验提示，不暴露服务器区域性或异常细节。</zh-CN>
                //   <en>The legacy page still parses dates using current culture; failures show only a low-sensitivity validation message without exposing server culture or exception details.</en>
                // </lang>
                ShowValidationMessage("请输入有效的到期日期。");
                return;
            }

            if (itemId == 0)
            {
                // <lang>
                //   <zh-CN>新增事件使用当前模块标识，创建人来自服务器端认证身份，不接受浏览器传入。</zh-CN>
                //   <en>New events use the current module id, and creator identity comes from the server-side authenticated principal rather than browser input.</en>
                // </lang>
                EventsDB.AddEvent(moduleId, Context.User.Identity.Name, TitleField.Text, expireDate,
                    DescriptionField.Text, WhereWhenField.Text);
            }
            else
            {
                // <lang>
                //   <zh-CN>更新路径已经在 `TryInitializeRequest` 中核验条目归属，避免跨模块编辑。</zh-CN>
                //   <en>The update path has already validated item ownership in `TryInitializeRequest`, avoiding cross-module edits.</en>
                // </lang>
                EventsDB.UpdateEvent(itemId, Context.User.Identity.Name, TitleField.Text, expireDate,
                    DescriptionField.Text, WhereWhenField.Text);
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除已核验归属的事件。</zh-CN>
        ///   <en>Deletes an event whose ownership has been verified.</en>
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
            //   <zh-CN>删除路径重新读取请求并校验归属，避免直接用 ItemId 删除其他模块事件。</zh-CN>
            //   <en>The delete path rereads the request and validates ownership, avoiding deletion of another module's event by ItemId.</en>
            // </lang>
            IEventItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (itemId != 0)
            {
                // <lang>
                //   <zh-CN>新增路径没有持久化事件可删；既有事件通过归属检查后才进入数据层删除。</zh-CN>
                //   <en>The creation path has no persisted event to delete; existing events reach the data-layer delete only after ownership checks.</en>
                // </lang>
                EventsDB.DeleteEvent(itemId);
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
            //   <zh-CN>取消不保存字段，但仍确认请求合法后才回到安全来源页。</zh-CN>
            //   <en>Cancel does not persist fields, but still confirms the request is valid before returning to the safe referrer.</en>
            // </lang>
            IEventItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取请求参数、核验模块编辑权限并确认既有事件归属。</zh-CN>
        ///   <en>Reads request parameters, verifies module edit permission and confirms ownership of an existing event.</en>
        /// </lang>
        /// </summary>
        /// <param name="item">
        /// <l>
        ///   <zh-CN>通过校验后输出的既有事件；新增路径为空。</zh-CN>
        ///   <en>Existing event emitted after validation; null on the creation path.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求可继续处理时返回 <c>true</c>；非法参数或权限失败时已跳转。</zh-CN>
        ///   <en><c>true</c> when the request may continue; invalid parameters or authorization failures have already redirected.</en>
        /// </l>
        /// </returns>
        private bool TryInitializeRequest(out IEventItem item)
        {
            // <lang>
            //   <zh-CN>输出参数先清空，确保非法请求不会把事件快照泄露给后续流程。</zh-CN>
            //   <en>The output parameter is cleared first so invalid requests cannot leak an event snapshot into later flow.</en>
            // </lang>
            item = null;
            // <lang>
            //   <zh-CN>模块标识同时限定权限和新增事件归属；非法或无权限请求统一进入编辑拒绝页。</zh-CN>
            //   <en>The module identifier constrains both permission and new-event ownership; invalid or unauthorized requests go to the edit-denied page uniformly.</en>
            // </lang>
            if (!PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["Mid"], out moduleId) ||
                !PortalSecurity.HasEditPermissions(moduleId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            // <lang>
            //   <zh-CN>ItemId 可省略以表示新增事件；非空值必须是正整数。</zh-CN>
            //   <en>ItemId may be omitted to represent a new event; non-empty values must be positive integers.</en>
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
                //   <zh-CN>新增路径不读取数据库事件，前置模块编辑权限已经足够授权创建。</zh-CN>
                //   <en>The creation path does not read a database event; the earlier module edit permission is sufficient to authorize creation.</en>
                // </lang>
                return true;
            }

            // <lang>
            //   <zh-CN>既有事件必须按 ItemId 读取并核对模块归属，避免跨模块直接对象引用。</zh-CN>
            //   <en>Existing events must be read by ItemId and checked against module ownership, avoiding cross-module direct object references.</en>
            // </lang>
            item = EventsDB.GetSingleEvent(itemId);
            if (item == null || item.ModuleId != moduleId)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            return true;
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
            //   <zh-CN>校验提示由服务器固定分支提供，不回显管理员输入的原始日期或路径。</zh-CN>
            //   <en>The validation notice comes from fixed server-side branches and does not echo the administrator's raw date or path input.</en>
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
            //   <zh-CN>历史创建人等字段可能已经实体化；显示前先解码再编码，避免重复实体显示。</zh-CN>
            //   <en>Historical creator and similar fields may already be entity-encoded; decode before encoding to avoid repeated entity display.</en>
            // </lang>
            return Server.HtmlEncode(Server.HtmlDecode(value ?? string.Empty));
        }
    }
}
