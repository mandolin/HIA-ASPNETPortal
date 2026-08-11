using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>编辑联系人模块条目的页面。</zh-CN>
    ///   <en>Page for editing contact-module items.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该页面沿用旧 WebForms 内容模块编辑入口；安全边界集中在模块编辑权限、联系人条目归属校验、服务器端创建人写入和站内安全回跳。</zh-CN>
    ///   <en>This page keeps the legacy WebForms content-module editing entry; its safety boundary centers on module edit permission, contact-item ownership validation, server-side creator assignment and safe in-app return.</en>
    /// </lang>
    /// </remarks>
    public partial class EditContacts : PortalPage<EditContacts>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前请求中的联系人条目标识；0 表示创建新联系人。</zh-CN>
        ///   <en>Contact item identifier for the current request; 0 means a new contact is being created.</en>
        /// </lang>
        /// </summary>
        private int itemId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前请求中的模块实例标识，是编辑权限和联系人归属校验的共同边界。</zh-CN>
        ///   <en>Module instance identifier for the current request, forming the shared boundary for edit permission and contact ownership checks.</en>
        /// </lang>
        /// </summary>
        private int moduleId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>联系人数据访问服务。</zh-CN>
        ///   <en>Contact data-access service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IContactsDb ContactsDB { private get; set; }

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
            //   <zh-CN>item 是通过权限与归属校验后的联系人快照；新增联系人时保持为空。</zh-CN>
            //   <en>item is the contact snapshot after permission and ownership validation; it remains null when creating a new contact.</en>
            // </lang>
            IContactItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                if (item != null)
                {
                    // <lang>
                    //   <zh-CN>首次加载只在归属校验通过后回填旧联系人字段，避免通过 ItemId 读取其他模块的条目。</zh-CN>
                    //   <en>Initial binding fills legacy contact fields only after ownership validation, avoiding reads of items from another module via ItemId.</en>
                    // </lang>
                    NameField.Text = item.Name;
                    RoleField.Text = item.Role;
                    EmailField.Text = item.Email;
                    Contact1Field.Text = item.Contact1;
                    Contact2Field.Text = item.Contact2;
                    CreatedBy.Text = EncodeDisplayText(item.CreatedByUser);
                    // <lang>
                    //   <zh-CN>创建时间只作为历史展示字段回填，不参与联系人保存或授权判断。</zh-CN>
                    //   <en>The creation date is filled only as a historical display field and does not participate in contact saving or authorization.</en>
                    // </lang>
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
        ///   <zh-CN>创建或更新已授权联系人，并回跳到当前应用内的安全地址。</zh-CN>
        ///   <en>Creates or updates an authorized contact and returns to a safe URL inside the current application.</en>
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
            //   <zh-CN>保存前重新初始化请求，确保联系人归属与模块编辑权限来自服务器端实时校验。</zh-CN>
            //   <en>The request is initialized again before saving so contact ownership and module edit permission come from live server-side validation.</en>
            // </lang>
            IContactItem item;
            if (!TryInitializeRequest(out item) || !Page.IsValid)
            {
                return;
            }

            if (itemId == 0)
            {
                // <lang>
                //   <zh-CN>新增联系人使用当前模块标识，创建人来自服务器端认证身份，不接受浏览器传入。</zh-CN>
                //   <en>New contacts use the current module id, and creator identity comes from the server-side authenticated principal rather than browser input.</en>
                // </lang>
                ContactsDB.AddContact(moduleId, Context.User.Identity.Name, NameField.Text, RoleField.Text,
                    EmailField.Text, Contact1Field.Text, Contact2Field.Text);
            }
            else
            {
                // <lang>
                //   <zh-CN>更新路径已经在 `TryInitializeRequest` 中核验条目归属，避免跨模块编辑。</zh-CN>
                //   <en>The update path has already validated item ownership in `TryInitializeRequest`, avoiding cross-module edits.</en>
                // </lang>
                ContactsDB.UpdateContact(itemId, Context.User.Identity.Name, NameField.Text, RoleField.Text,
                    EmailField.Text, Contact1Field.Text, Contact2Field.Text);
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除已核验归属的联系人。</zh-CN>
        ///   <en>Deletes a contact whose ownership has been verified.</en>
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
            //   <zh-CN>删除路径重新校验请求，避免仅凭 ItemId 触发跨模块删除。</zh-CN>
            //   <en>The delete path revalidates the request, preventing cross-module deletion based only on ItemId.</en>
            // </lang>
            IContactItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (itemId != 0)
            {
                // <lang>
                //   <zh-CN>只有既有联系人且已确认属于当前模块时才调用数据层删除。</zh-CN>
                //   <en>The data layer is called only for an existing contact already confirmed to belong to the current module.</en>
                // </lang>
                ContactsDB.DeleteContact(itemId);
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
            //   <zh-CN>取消不写入联系人字段，但仍确认请求合法后才使用保存的安全回跳地址。</zh-CN>
            //   <en>Cancel does not persist contact fields, but still confirms the request is valid before using the stored safe return URL.</en>
            // </lang>
            IContactItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取请求参数、核验模块编辑权限并确认既有联系人归属。</zh-CN>
        ///   <en>Reads request parameters, verifies module edit permission and confirms ownership of an existing contact.</en>
        /// </lang>
        /// </summary>
        /// <param name="item">
        /// <l>
        ///   <zh-CN>通过校验后输出的既有联系人；新增路径为空。</zh-CN>
        ///   <en>Existing contact emitted after validation; null on the creation path.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求可继续处理时返回 <c>true</c>；非法参数或权限失败时已跳转。</zh-CN>
        ///   <en><c>true</c> when the request may continue; invalid parameters or authorization failures have already redirected.</en>
        /// </l>
        /// </returns>
        private bool TryInitializeRequest(out IContactItem item)
        {
            // <lang>
            //   <zh-CN>输出参数先清空，保证权限失败时不会把联系人快照交给调用方。</zh-CN>
            //   <en>The output parameter is cleared first so authorization failures cannot pass a contact snapshot to callers.</en>
            // </lang>
            item = null;
            // <lang>
            //   <zh-CN>模块标识是权限检查和新增联系人归属的根；非法模块直接进入受控拒绝页。</zh-CN>
            //   <en>The module identifier is the root for permission checks and new-contact ownership; invalid modules go directly to the controlled denial page.</en>
            // </lang>
            if (!PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["Mid"], out moduleId) ||
                !PortalSecurity.HasEditPermissions(moduleId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            // <lang>
            //   <zh-CN>ItemId 缺失表示新增联系人；非空值必须是正整数，避免 0 或负数伪装既有条目。</zh-CN>
            //   <en>A missing ItemId means a new contact; non-empty values must be positive integers, preventing 0 or negative values from impersonating existing rows.</en>
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
                //   <zh-CN>新增路径没有既有联系人可做归属比对，模块编辑权限就是完整授权条件。</zh-CN>
                //   <en>The creation path has no existing contact for ownership comparison, so module edit permission is the complete authorization condition.</en>
                // </lang>
                return true;
            }

            // <lang>
            //   <zh-CN>既有联系人重新从数据层读取并比对模块，避免通过 ItemId 直接引用其他模块记录。</zh-CN>
            //   <en>Existing contacts are reloaded from the data layer and compared against the module to avoid direct references to another module's row via ItemId.</en>
            // </lang>
            item = ContactsDB.GetSingleContact(itemId);
            if (item == null || item.ModuleId != moduleId)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            return true;
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
            //   <zh-CN>展示历史创建人等文本时先解码再编码，兼容旧实体存储并保持 HTML 文本节点安全。</zh-CN>
            //   <en>When displaying historical creator text and similar values, decode before encoding to support legacy entity storage while keeping HTML text nodes safe.</en>
            // </lang>
            return Server.HtmlEncode(Server.HtmlDecode(value ?? string.Empty));
        }
    }
}
