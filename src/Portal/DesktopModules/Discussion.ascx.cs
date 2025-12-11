using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI.WebControls;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class Discussion : PortalModuleControl<Discussion>
    {
        [Dependency]
        public IDiscussionsDb DiscussionDB { private get; set; } // 依赖注入：讨论数据库访问接口

        // 用来存放当前选中顶级消息的 DisplayOrder（用于取回复）
        private string _currentParentDisplayOrder;

        // Page_Load 事件处理程序用于在页面首次访问时获取并绑定讨论消息列表
        protected void Page_Load(object sender, EventArgs e)
        {
            // 如果这不是一个回发请求，则绑定列表
            if (!Page.IsPostBack)
            {
                BindList();
            }
        }

        // BindList 方法用于从 Discussion 表中获取顶级消息列表，并将其绑定到名为 "TopLevelList" 的 DataList 控件
        private void BindList()
        {
            // 获取与模块相关的讨论消息列表，并绑定到 DataList 控件
            TopLevelList.DataSource = DiscussionDB.GetTopLevelMessages(ModuleId);
            TopLevelList.DataBind();
        }

        // GetThreadMessages 方法用于获取顶级讨论消息线程中的子主题消息列表
        // 此方法用于填充 "TopLevelList" 中 "SelectedItemTemplate" 内的 "DetailList" DataList 控件
        protected List<IDiscussionItem> GetThreadMessages(string displayOrder)
        {
            // 获取与选定的顶级讨论消息相关的子讨论消息列表
            //IDataReader dr = DiscussionDB.GetThreadMessages(TopLevelList.DataKeys[TopLevelList.SelectedIndex].ToString());

            // 返回过滤后的 DataReader
            //return dr;

            return DiscussionDB.GetThreadMessages(displayOrder);
            throw new NotImplementedException();
        }

        // 在 TopLevelList 的 ItemDataBound 或 ItemCommand 中设置这个值
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

                // 如果你原来是用 SelectedIndex 来判断“展开哪个”，现在可以用一个 HiddenField 或 ViewState
                // 这里我们默认全部展开（最常见需求），如果要“点击展开”，再告诉我我给你加折叠逻辑
            }
        }

        protected string FormatDate(object dateObj)
        {
            if (dateObj == null || dateObj == DBNull.Value)
                return "未知时间";

            return ((DateTime)dateObj).ToString("g");
        }

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

        // TopLevelList_OnItemCommand 事件处理程序用于在层次结构的 DataList 控件中展开/折叠选定的讨论主题
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

        // FormatUrl 方法是一个帮助方法，由 DataList 控件模板中的绑定语句调用
        // 该方法在此定义为帮助方法（而不是直接在模板内定义），以改善代码组织并避免在内容模板中嵌入逻辑
        protected string FormatUrl(int item)
        {
            // 构造讨论详情页面的 URL
            return "~/DesktopModules/DiscussDetails.aspx?ItemID=" + item + "&mid=" + ModuleId;
        }

        // NodeImage 方法是一个帮助方法，由 DataList 控件模板中的绑定语句调用
        // 它控制列表中的项目是否应作为可展开的主题呈现，或者只是作为一个单一节点
        protected string NodeImage(int count)
        {
            // 如果子项计数大于 0，则返回展开图标，否则返回单节点图标
            return count > 0 ? "~/images/plus.gif" : "~/images/node.gif";
        }

        protected string NodeCommandName(int count)
        {
            // 如果子项计数大于 0，则返回展开图标，否则返回单节点图标
            return count > 0 ? "select" : "";
        }
        
    }
}