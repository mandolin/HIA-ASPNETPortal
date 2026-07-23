using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI.WebControls;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>显示讨论主题和已展开主题的回复列表。</zh-CN>
    ///   <en>Renders discussion topics and replies for an expanded topic.</en>
    /// </lang>
    /// </summary>
    public partial class Discussion : PortalModuleControl<Discussion>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>讨论数据访问服务。</zh-CN>
        ///   <en>Discussion data-access service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IDiscussionsDb DiscussionDB { private get; set; }

        // <lang>
        //   <zh-CN>保存当前展开的顶级消息 DisplayOrder，供嵌套回复列表按同一父主题读取数据。</zh-CN>
        //   <en>Stores the expanded top-level message DisplayOrder so the nested reply list can read data for the same parent topic.</en>
        // </lang>
        private string _currentParentDisplayOrder;

        /// <summary>
        /// <lang>
        ///   <zh-CN>在首次请求时绑定讨论主题。</zh-CN>
        ///   <en>Binds discussion topics on the first request.</en>
        /// </lang>
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>只在首次请求绑定主题列表，避免展开/折叠等回发状态被初始化覆盖。</zh-CN>
            //   <en>Binds the topic list only on the initial request so expand/collapse postback state is not reset.</en>
            // </lang>
            if (!Page.IsPostBack)
            {
                BindList();
            }
        }

        // <lang>
        //   <zh-CN>列表会在展开/折叠命令后重绑，避免让模板直接承担状态转换逻辑。</zh-CN>
        //   <en>The list is rebound after expand/collapse commands so the template does not own state transitions.</en>
        // </lang>
        private void BindList()
        {
            // <lang>
            //   <zh-CN>只读取当前模块的顶级讨论消息，回复列表由展开项的数据绑定阶段单独读取。</zh-CN>
            //   <en>Reads only top-level discussion messages for the current module; replies are loaded separately while binding the expanded item.</en>
            // </lang>
            TopLevelList.DataSource = DiscussionDB.GetTopLevelMessages(ModuleId);
            TopLevelList.DataBind();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为当前展开主题读取回复列表。</zh-CN>
        ///   <en>Reads replies for the currently expanded topic.</en>
        /// </lang>
        /// </summary>
        protected List<IDiscussionItem> GetThreadMessages(string displayOrder)
        {
            return DiscussionDB.GetThreadMessages(displayOrder);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在数据绑定时给展开项绑定回复列表。</zh-CN>
        ///   <en>Binds replies for an expanded item during data binding.</en>
        /// </lang>
        /// </summary>
        protected void TopLevelList_ItemDataBound(object sender, DataListItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                var item = (IDiscussionItem)e.Item.DataItem;

                // <lang>
                //   <zh-CN>模板中的子回复列表只在当前主题项内查找，避免跨主题复用控件状态。</zh-CN>
                //   <en>The nested reply list is resolved only inside the current topic item to avoid sharing control state across topics.</en>
                // </lang>
                DataList detailList = (DataList)e.Item.FindControl("DetailList");
                if (detailList != null)
                {
                    // <lang>
                    //   <zh-CN>关键点：先记录顶级帖子的 DisplayOrder，再按该值读取它的所有回复。</zh-CN>
                    //   <en>Key point: record the top-level post DisplayOrder before loading all replies under that value.</en>
                    // </lang>
                    _currentParentDisplayOrder = item.DisplayOrder; // 例如 "0001."

                    // <lang>
                    //   <zh-CN>回复绑定限制在当前展开主题内，不改变顶级主题列表的选择状态。</zh-CN>
                    //   <en>Reply binding remains scoped to the expanded topic and does not change the selected state of the top-level list.</en>
                    // </lang>
                    detailList.DataSource = DiscussionDB.GetThreadMessages(_currentParentDisplayOrder);
                    detailList.DataBind();
                }

            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>格式化可空日期值。</zh-CN>
        ///   <en>Formats a nullable date value.</en>
        /// </lang>
        /// </summary>
        protected string FormatDate(object dateObj)
        {
            if (dateObj == null || dateObj == DBNull.Value)
                return "未知时间";

            return ((DateTime)dateObj).ToString("g");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将历史上可能已编码的讨论文本规范为一次 HTML 编码的显示文本。</zh-CN>
        ///   <en>Normalizes discussion text that may already be encoded into display text encoded exactly once for HTML output.</en>
        /// </lang>
        /// </summary>
        protected string EncodeDisplayText(object value)
        {
            return Server.HtmlEncode(Server.HtmlDecode(Convert.ToString(value) ?? string.Empty));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>生成只由计算所得缩进级别组成的安全布局标记。</zh-CN>
        ///   <en>Generates safe layout markup composed only from a computed indentation level.</en>
        /// </lang>
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
        /// <lang>
        ///   <zh-CN>展开或折叠选定主题。</zh-CN>
        ///   <en>Expands or collapses the selected topic.</en>
        /// </lang>
        /// </summary>
        protected void TopLevelList_OnItemCommand(object Sender, DataListCommandEventArgs e)
        {
            // <lang>
            //   <zh-CN>命令只接受模板按钮发出的 select/collapse，其它命令直接忽略。</zh-CN>
            //   <en>Commands are limited to select/collapse values emitted by template buttons; any other command is ignored.</en>
            // </lang>
            LinkButton commandButton = e.CommandSource as LinkButton;
            string command = commandButton == null ? string.Empty : commandButton.CommandName;

            // <lang>
            //   <zh-CN>选择索引是 Web Forms DataList 展开状态的来源，更新后必须重新绑定列表才能刷新嵌套回复。</zh-CN>
            //   <en>The selected index is the Web Forms DataList source of expanded state, so the list must be rebound after it changes.</en>
            // </lang>
            if (command == "collapse")
            {
                TopLevelList.SelectedIndex = -1;
            }
            else if (command == "select")
            {
                TopLevelList.SelectedIndex = e.Item.ItemIndex;

            }
            else
            {
                return;
            }

            BindList();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>构建当前模块内讨论详情页地址。</zh-CN>
        ///   <en>Builds a discussion-detail URL inside the current module.</en>
        /// </lang>
        /// </summary>
        protected string FormatUrl(int item)
        {
            // <lang>
            //   <zh-CN>详情页仍沿用旧查询参数契约，模块标识用于返回当前模块上下文。</zh-CN>
            //   <en>The detail page keeps the legacy query-string contract, and the module identifier preserves the current module context.</en>
            // </lang>
            return "~/DesktopModules/DiscussDetails.aspx?ItemID=" + item + "&mid=" + ModuleId;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>根据子消息数选择展开命令。</zh-CN>
        ///   <en>Selects an expand command by child-message count.</en>
        /// </lang>
        /// </summary>
        protected string NodeCommandName(int count)
        {
            // <lang>
            //   <zh-CN>有回复的主题允许展开；无回复主题只展示静态状态文本，不触发回发命令。</zh-CN>
            //   <en>Topics with replies can expand; empty topics show static status text and do not emit a postback command.</en>
            // </lang>
            return count > 0 ? "select" : "";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断主题是否有可展开的回复。</zh-CN>
        ///   <en>Determines whether a topic has replies that can be expanded.</en>
        /// </lang>
        /// </summary>
        protected bool HasChildMessages(int count)
        {
            return count > 0;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回主题行左侧状态按钮文本。</zh-CN>
        ///   <en>Returns the text shown in the left-side topic status button.</en>
        /// </lang>
        /// </summary>
        protected string NodeToggleText(int count)
        {
            return count > 0 ? "Expand" : "Thread";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回主题行左侧状态按钮样式，保持可展开和无回复主题的视觉区分。</zh-CN>
        ///   <en>Returns the left-side topic status-button classes, visually separating expandable and empty topics.</en>
        /// </lang>
        /// </summary>
        protected string NodeToggleCssClass(int count)
        {
            return count > 0
                ? "CommandButton portal-discussion-toggle portal-secondary-action"
                : "CommandButton portal-discussion-toggle portal-secondary-action portal-discussion-toggle-empty";
        }

    }
}
