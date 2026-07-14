using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI.WebControls;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：显示讨论主题和已展开主题的回复列表。
    ///
    /// English: Renders discussion topics and replies for an expanded topic.
    /// </summary>
    public partial class Discussion : PortalModuleControl<Discussion>
    {
        /// <summary>
        /// 中文：讨论数据访问服务。English: Discussion data-access service.
        /// </summary>
        [Dependency]
        public IDiscussionsDb DiscussionDB { private get; set; }

        // 用来存放当前选中顶级消息的 DisplayOrder（用于取回复）
        private string _currentParentDisplayOrder;

        /// <summary>
        /// 中文：在首次请求时绑定讨论主题。English: Binds discussion topics on the first request.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            // 如果这不是一个回发请求，则绑定列表
            if (!Page.IsPostBack)
            {
                BindList();
            }
        }

        // 中文：列表会在展开/折叠命令后重绑，避免让模板直接承担状态转换逻辑。
        // English: The list is rebound after expand/collapse commands so the template does not own state transitions.
        private void BindList()
        {
            // 获取与模块相关的讨论消息列表，并绑定到 DataList 控件
            TopLevelList.DataSource = DiscussionDB.GetTopLevelMessages(ModuleId);
            TopLevelList.DataBind();
        }

        /// <summary>
        /// 中文：为当前展开主题读取回复列表。English: Reads replies for the currently expanded topic.
        /// </summary>
        protected List<IDiscussionItem> GetThreadMessages(string displayOrder)
        {
            return DiscussionDB.GetThreadMessages(displayOrder);
        }

        /// <summary>
        /// 中文：在数据绑定时给展开项绑定回复列表。English: Binds replies for an expanded item during data binding.
        /// </summary>
        protected void TopLevelList_ItemDataBound(object sender, DataListItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                var item = (IDiscussionItem)e.Item.DataItem;

                // 找到子回复的 DataList
                DataList detailList = (DataList)e.Item.FindControl("DetailList");
                if (detailList != null)
                {
                    // 关键：把当前顶级帖子的 DisplayOrder 传给子列表
                    _currentParentDisplayOrder = item.DisplayOrder; // 例如 "0001."

                    // 绑定子回复
                    detailList.DataSource = DiscussionDB.GetThreadMessages(_currentParentDisplayOrder);
                    detailList.DataBind();
                }

            }
        }

        /// <summary>
        /// 中文：格式化可空日期值。English: Formats a nullable date value.
        /// </summary>
        protected string FormatDate(object dateObj)
        {
            if (dateObj == null || dateObj == DBNull.Value)
                return "未知时间";

            return ((DateTime)dateObj).ToString("g");
        }

        /// <summary>
        /// 中文：将历史上可能已编码的讨论文本规范为一次 HTML 编码的显示文本。
        ///
        /// English: Normalizes discussion text that may already be encoded into display text encoded exactly once for HTML output.
        /// </summary>
        protected string EncodeDisplayText(object value)
        {
            return Server.HtmlEncode(Server.HtmlDecode(Convert.ToString(value) ?? string.Empty));
        }

        /// <summary>
        /// 中文：生成只由计算所得缩进级别组成的安全布局标记。English: Generates safe layout markup composed only from a computed indentation level.
        /// </summary>
        protected string GetIndentHtml(object displayOrderObj)
        {
            if (displayOrderObj == null || displayOrderObj == DBNull.Value)
                return string.Empty;

            string displayOrder = displayOrderObj.ToString();
            if (string.IsNullOrEmpty(displayOrder))
                return string.Empty;

            int level = displayOrder.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Length - 1;
            if (level <= 0) return string.Empty;

            return "<span style=\"margin-left:" + (level * 20) + "px;display:inline-block;\"></span>";
        }

        /// <summary>
        /// 中文：展开或折叠选定主题。English: Expands or collapses the selected topic.
        /// </summary>
        protected void TopLevelList_OnItemCommand(object Sender, DataListCommandEventArgs e)
        {
            // 确定按钮的命令（要么是 "select"，要么是 "collapse"）
            string command = ((ImageButton)e.CommandSource).CommandName;

            // 根据命令类型更新 DataList 的选择索引，然后重新绑定 DataList 的内容
            if (command == "collapse")
            {
                // 如果命令是 "collapse"，则清除选择
                TopLevelList.SelectedIndex = -1;
            }
            else if (command == "select")
            {
                // 否则，设置新的选择索引
                TopLevelList.SelectedIndex = e.Item.ItemIndex;

            }
            else
            {
                return;
            }

            // 重新绑定列表以反映更改
            BindList();
        }

        /// <summary>
        /// 中文：构建当前模块内讨论详情页地址。English: Builds a discussion-detail URL inside the current module.
        /// </summary>
        protected string FormatUrl(int item)
        {
            // 构造讨论详情页面的 URL
            return "~/DesktopModules/DiscussDetails.aspx?ItemID=" + item + "&mid=" + ModuleId;
        }

        /// <summary>
        /// 中文：根据子消息数选择树节点图标。English: Selects a tree-node icon by child-message count.
        /// </summary>
        protected string NodeImage(int count)
        {
            // 如果子项计数大于 0，则返回展开图标，否则返回单节点图标
            return count > 0 ? "~/images/plus.gif" : "~/images/node.gif";
        }

        /// <summary>
        /// 中文：根据子消息数选择展开命令。English: Selects an expand command by child-message count.
        /// </summary>
        protected string NodeCommandName(int count)
        {
            // 如果子项计数大于 0，则返回展开图标，否则返回单节点图标
            return count > 0 ? "select" : "";
        }
        
    }
}
