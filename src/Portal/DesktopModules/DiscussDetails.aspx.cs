using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：显示讨论主题并允许已授权编辑者发布主题或回复的页面。
    ///
    /// English: Page that displays a discussion topic and allows authorized editors to post a topic or reply.
    /// </summary>
    public partial class DiscussDetails : PortalPage<DiscussDetails>
    {
        private int itemId;
        private int moduleId;

        /// <summary>
        /// 中文：讨论数据访问服务。English: Discussion data-access service.
        /// </summary>
        [Dependency]
        public IDiscussionsDb DiscussionDB { private get; set; }

        /// <summary>
        /// 中文：模块编辑权限服务。English: Module edit-authorization service.
        /// </summary>
        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        /// <summary>
        /// 中文：初始化讨论请求；任何访客可读取现有主题，只有模块编辑者可创建或回复。
        ///
        /// English: Initializes a discussion request. Any visitor may read an existing topic, while only module editors may create or reply.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TryReadRequestIdentifiers())
            {
                return;
            }

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
        /// 中文：打开回复编辑区；仅模块编辑者可执行。English: Opens the reply editor; only module editors may perform this action.
        /// </summary>
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
        /// 中文：创建主题或回复，并使用 HTML 编码存储普通用户文本。English: Creates a topic or reply and stores ordinary user text HTML-encoded.
        /// </summary>
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            if (!TryAuthorizeEditorForCurrentItem())
            {
                return;
            }

            itemId = DiscussionDB.AddMessage(moduleId, itemId, User.Identity.Name,
                Server.HtmlEncode(TitleField.Text), Server.HtmlEncode(BodyField.Text));
            EditPanel.Visible = false;
            ButtonPanel.Visible = true;
            BindData();
        }

        /// <summary>
        /// 中文：取消回复编辑；仅模块编辑者可执行。English: Cancels reply editing; only module editors may perform this action.
        /// </summary>
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            if (!TryAuthorizeEditorForCurrentItem())
            {
                return;
            }

            EditPanel.Visible = false;
            ButtonPanel.Visible = true;
        }

        private bool TryReadRequestIdentifiers()
        {
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

            if (!PortalNavigationPolicy.TryReadPositiveInt32(requestedItemId, out itemId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            return true;
        }

        private bool TryAuthorizeEditorForCurrentItem()
        {
            if (!TryReadRequestIdentifiers() || !PortalSecurity.HasEditPermissions(moduleId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            if (itemId == 0)
            {
                return true;
            }

            IDiscussionItem item = DiscussionDB.GetSingleMessage(itemId);
            if (item == null || item.ModuleID != moduleId)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            return true;
        }

        private bool BindData()
        {
            IDiscussionItem item = DiscussionDB.GetSingleMessage(itemId);
            if (item == null || item.ModuleID != moduleId)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            Subject.Text = EncodeDisplayText(item.Title);
            Body.Text = EncodeDisplayText(item.Body);
            CreatedByUser.Text = EncodeDisplayText(item.CreatedByUser ?? "匿名");
            CreatedDate.Text = item.CreatedDate.HasValue ? item.CreatedDate.Value.ToString("d") : "未知时间";
            TitleField.Text = ReTitle(Server.HtmlDecode(item.Title ?? string.Empty));
            prevItem.Visible = false;
            nextItem.Visible = false;
            return true;
        }

        private string ReTitle(string title)
        {
            return string.IsNullOrEmpty(title) || title.StartsWith("Re: ", StringComparison.Ordinal)
                ? title
                : "Re: " + title;
        }

        private string EncodeDisplayText(string value)
        {
            return Server.HtmlEncode(Server.HtmlDecode(value ?? string.Empty));
        }
    }
}
