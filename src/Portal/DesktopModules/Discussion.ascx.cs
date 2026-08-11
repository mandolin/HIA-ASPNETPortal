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
    /// <remarks>
    /// <lang>
    ///   <zh-CN>控件保留旧 Web Forms DataList 展开/折叠模型：顶级主题由模块标识读取，选中项再按 <c>DisplayOrder</c> 读取回复。用户输入文本必须继续通过 code-behind helper 编码后进入标记层。</zh-CN>
    ///   <en>The control preserves the legacy Web Forms DataList expand/collapse model: top-level topics are read by module identifier, and the selected item then loads replies by <c>DisplayOrder</c>. User-supplied text must continue to pass through code-behind encoding helpers before reaching markup.</en>
    /// </lang>
    /// </remarks>
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
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发页面生命周期事件的控件实例。</zh-CN>
        ///   <en>The control instance that raised the page lifecycle event.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>页面加载事件数据；当前实现不读取其内容。</zh-CN>
        ///   <en>Page-load event data; the current implementation does not read its contents.</en>
        /// </l>
        /// </param>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>把当前模块的顶级讨论主题绑定到外层列表。</zh-CN>
        ///   <en>Binds the current module's top-level discussion topics to the outer list.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>列表会在展开/折叠命令后重绑，避免让模板直接承担状态转换逻辑；回复数据仍由选中项的数据绑定阶段按需读取。</zh-CN>
        ///   <en>The list is rebound after expand/collapse commands so the template does not own state transitions; reply data is still loaded on demand during selected-item binding.</en>
        /// </lang>
        /// </remarks>
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
        /// <param name="displayOrder">
        /// <l>
        ///   <zh-CN>顶级主题的旧 <c>DisplayOrder</c> 路径；来自已绑定的数据项，而不是任意客户端输入。</zh-CN>
        ///   <en>The top-level topic's legacy <c>DisplayOrder</c> path, coming from a bound data item rather than arbitrary client input.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>该父路径下的回复集合；展示层仍负责逐项编码。</zh-CN>
        ///   <en>The replies under that parent path; the presentation layer remains responsible for per-item encoding.</en>
        /// </l>
        /// </returns>
        protected List<IDiscussionItem> GetThreadMessages(string displayOrder)
        {
            // <lang>
            //   <zh-CN>此 helper 只桥接模板绑定表达式和数据访问接口，不缓存回复集合，避免跨回发复用旧主题内容。</zh-CN>
            //   <en>This helper only bridges the template binding expression to the data-access contract and does not cache replies, avoiding reuse of stale thread content across postbacks.</en>
            // </lang>
            return DiscussionDB.GetThreadMessages(displayOrder);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在数据绑定时给展开项绑定回复列表。</zh-CN>
        ///   <en>Binds replies for an expanded item during data binding.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发项绑定事件的外层 DataList。</zh-CN>
        ///   <en>The outer DataList that raised the item-binding event.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>当前模板项和其数据对象。</zh-CN>
        ///   <en>The current template item and its data object.</en>
        /// </l>
        /// </param>
        protected void TopLevelList_ItemDataBound(object sender, DataListItemEventArgs e)
        {
            // <lang>
            //   <zh-CN>只处理普通数据项；页眉、页脚和分隔项不携带讨论业务对象，不能参与回复绑定。</zh-CN>
            //   <en>Only ordinary data items are processed; headers, footers, and separators do not carry discussion business objects and cannot bind replies.</en>
            // </lang>
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                // <lang>
                //   <zh-CN>当前顶级主题对象来自 DataList 数据源，生命周期只覆盖本次 ItemDataBound 回调。</zh-CN>
                //   <en>The current top-level topic object comes from the DataList data source and lives only for this ItemDataBound callback.</en>
                // </lang>
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
                    _currentParentDisplayOrder = item.DisplayOrder;

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
        /// <param name="dateObj">
        /// <l>
        ///   <zh-CN>来自数据绑定表达式的候选创建时间，可能为 <c>null</c> 或 <see cref="DBNull"/>。</zh-CN>
        ///   <en>Candidate creation time from a data-binding expression; it may be <c>null</c> or <see cref="DBNull"/>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前区域性的短日期时间文本；缺失时返回旧界面使用的中文占位文本。</zh-CN>
        ///   <en>A short date-time string using the current culture, or the legacy Chinese placeholder when the value is missing.</en>
        /// </l>
        /// </returns>
        protected string FormatDate(object dateObj)
        {
            // <lang>
            //   <zh-CN>旧数据和 DataBinder 都可能以空引用或 DBNull 表示缺失时间；两者需要同一占位输出。</zh-CN>
            //   <en>Legacy data and DataBinder may represent a missing date as either null or DBNull, so both need the same placeholder output.</en>
            // </lang>
            if (dateObj == null || dateObj == DBNull.Value)
                return "未知时间";

            // <lang>
            //   <zh-CN>格式化只发生在服务器端，返回值随后进入普通文本节点，不携带 HTML 标记。</zh-CN>
            //   <en>Formatting happens only on the server, and the returned value later enters an ordinary text node without carrying HTML markup.</en>
            // </lang>
            return ((DateTime)dateObj).ToString("g");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将历史上可能已编码的讨论文本规范为一次 HTML 编码的显示文本。</zh-CN>
        ///   <en>Normalizes discussion text that may already be encoded into display text encoded exactly once for HTML output.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>来自数据绑定表达式的候选标题、作者或正文片段。</zh-CN>
        ///   <en>Candidate title, author, or body fragment from a data-binding expression.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>适合写入 HTML 文本节点的编码文本。</zh-CN>
        ///   <en>Encoded text suitable for writing into an HTML text node.</en>
        /// </l>
        /// </returns>
        protected string EncodeDisplayText(object value)
        {
            // <lang>
            //   <zh-CN>先解码再编码可以兼容已编码历史行，同时避免把新输入重复编码后显示为实体文本。</zh-CN>
            //   <en>Decoding before encoding supports historically encoded rows while avoiding newly entered text being double-encoded into entity text.</en>
            // </lang>
            return Server.HtmlEncode(Server.HtmlDecode(Convert.ToString(value) ?? string.Empty));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>生成只由计算所得缩进级别组成的安全布局标记。</zh-CN>
        ///   <en>Generates safe layout markup composed only from a computed indentation level.</en>
        /// </lang>
        /// </summary>
        /// <param name="displayOrderObj">
        /// <l>
        ///   <zh-CN>来自绑定行的旧 <c>DisplayOrder</c> 值。</zh-CN>
        ///   <en>The legacy <c>DisplayOrder</c> value from the bound row.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>用于树形缩进的受限 <c>span</c> 标记；没有缩进时返回空字符串。</zh-CN>
        ///   <en>A constrained <c>span</c> fragment used for tree indentation, or an empty string when no indentation is needed.</en>
        /// </l>
        /// </returns>
        protected string GetIndentHtml(object displayOrderObj)
        {
            // <lang>
            //   <zh-CN>缺失排序路径时不生成占位标记，避免错误数据改变讨论行布局。</zh-CN>
            //   <en>When the sort path is missing, no placeholder markup is emitted so malformed data does not alter the discussion-row layout.</en>
            // </lang>
            if (displayOrderObj == null || displayOrderObj == DBNull.Value)
                return string.Empty;

            // <lang>
            //   <zh-CN>DisplayOrder 是旧线程路径文本，只用于计算层级；不会原样拼接进返回的 HTML。</zh-CN>
            //   <en>DisplayOrder is a legacy thread-path string used only to compute the level; it is never concatenated directly into returned HTML.</en>
            // </lang>
            string displayOrder = displayOrderObj.ToString();
            if (string.IsNullOrEmpty(displayOrder))
                return string.Empty;

            // <lang>
            //   <zh-CN>层级由点分段数推导，顶级或异常短路径不需要缩进。</zh-CN>
            //   <en>The level is inferred from dot-separated segments, and top-level or unusually short paths need no indentation.</en>
            // </lang>
            int level = displayOrder.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Length - 1;
            if (level <= 0) return string.Empty;

            // <lang>
            //   <zh-CN>返回片段只包含由整数层级计算出的像素值，因此不引入来自数据库的 HTML 或样式文本。</zh-CN>
            //   <en>The returned fragment contains only a pixel value calculated from an integer level, so it introduces no HTML or style text from the database.</en>
            // </lang>
            return "<span style=\"margin-left:" + (level * 20) + "px;display:inline-block;\"></span>";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>展开或折叠选定主题。</zh-CN>
        ///   <en>Expands or collapses the selected topic.</en>
        /// </lang>
        /// </summary>
        /// <param name="Sender">
        /// <l>
        ///   <zh-CN>触发列表命令的控件；保留旧签名大小写以匹配既有事件处理器。</zh-CN>
        ///   <en>The control that raised the list command; the legacy parameter casing is preserved to match the existing event handler.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>包含命令源和当前列表项索引的 DataList 命令事件数据。</zh-CN>
        ///   <en>DataList command event data containing the command source and current item index.</en>
        /// </l>
        /// </param>
        protected void TopLevelList_OnItemCommand(object Sender, DataListCommandEventArgs e)
        {
            // <lang>
            //   <zh-CN>命令只接受模板按钮发出的 select/collapse，其它命令直接忽略。</zh-CN>
            //   <en>Commands are limited to select/collapse values emitted by template buttons; any other command is ignored.</en>
            // </lang>
            LinkButton commandButton = e.CommandSource as LinkButton;
            // <lang>
            //   <zh-CN>将缺失或非 LinkButton 命令源折叠为空命令，使后续白名单分支统一拒绝。</zh-CN>
            //   <en>Missing or non-LinkButton command sources are folded into an empty command so the later allow-list branch rejects them uniformly.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>选择状态改变后立即重绑，确保选中模板和子回复列表与 DataList 状态同步。</zh-CN>
            //   <en>After selected state changes, rebind immediately so the selected template and child reply list match the DataList state.</en>
            // </lang>
            BindList();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>构建当前模块内讨论详情页地址。</zh-CN>
        ///   <en>Builds a discussion-detail URL inside the current module.</en>
        /// </lang>
        /// </summary>
        /// <param name="item">
        /// <l>
        ///   <zh-CN>当前讨论条目的数据库标识。</zh-CN>
        ///   <en>The database identifier of the current discussion item.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>带消息标识和当前模块标识的站内详情页虚拟路径。</zh-CN>
        ///   <en>An in-site detail-page virtual path carrying the message identifier and current module identifier.</en>
        /// </l>
        /// </returns>
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
        /// <param name="count">
        /// <l>
        ///   <zh-CN>当前主题的直接子回复数量。</zh-CN>
        ///   <en>The current topic's direct child-reply count.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可展开时返回 <c>select</c>；否则返回空命令以禁止无意义回发。</zh-CN>
        ///   <en>Returns <c>select</c> when expansion is available, otherwise an empty command to suppress meaningless postbacks.</en>
        /// </l>
        /// </returns>
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
        /// <param name="count">
        /// <l>
        ///   <zh-CN>当前主题的直接子回复数量。</zh-CN>
        ///   <en>The current topic's direct child-reply count.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>存在至少一个子回复时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when at least one child reply exists.</en>
        /// </l>
        /// </returns>
        protected bool HasChildMessages(int count)
        {
            // <lang>
            //   <zh-CN>该返回值同时驱动按钮可用性和展开命令，保持视觉状态与服务器端回发语义一致。</zh-CN>
            //   <en>This return value drives both button enablement and expand command behavior, keeping visual state aligned with server-side postback semantics.</en>
            // </lang>
            return count > 0;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回主题行左侧状态按钮文本。</zh-CN>
        ///   <en>Returns the text shown in the left-side topic status button.</en>
        /// </lang>
        /// </summary>
        /// <param name="count">
        /// <l>
        ///   <zh-CN>当前主题的直接子回复数量。</zh-CN>
        ///   <en>The current topic's direct child-reply count.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>有回复时显示展开动作文本；无回复时显示静态主题状态文本。</zh-CN>
        ///   <en>Shows expansion action text when replies exist; otherwise shows static thread-state text.</en>
        /// </l>
        /// </returns>
        protected string NodeToggleText(int count)
        {
            // <lang>
            //   <zh-CN>文案与命令启用条件共用同一个数量判断，避免“可点/不可点”状态在界面上分裂。</zh-CN>
            //   <en>The label shares the same count check as command enablement so clickable and non-clickable states do not diverge in the UI.</en>
            // </lang>
            return count > 0 ? "Expand" : "Thread";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回主题行左侧状态按钮样式，保持可展开和无回复主题的视觉区分。</zh-CN>
        ///   <en>Returns the left-side topic status-button classes, visually separating expandable and empty topics.</en>
        /// </lang>
        /// </summary>
        /// <param name="count">
        /// <l>
        ///   <zh-CN>当前主题的直接子回复数量。</zh-CN>
        ///   <en>The current topic's direct child-reply count.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>主题化按钮 CSS 类；无回复主题额外附加空状态类。</zh-CN>
        ///   <en>The themed button CSS classes, with an additional empty-state class for topics without replies.</en>
        /// </l>
        /// </returns>
        protected string NodeToggleCssClass(int count)
        {
            // <lang>
            //   <zh-CN>类名仍包含旧 CommandButton 以兼容既有样式，再叠加门户主题类表达现代视觉状态。</zh-CN>
            //   <en>The class list keeps the legacy CommandButton class for existing styles and layers portal theme classes for the modern visual state.</en>
            // </lang>
            return count > 0
                ? "CommandButton portal-discussion-toggle portal-secondary-action"
                : "CommandButton portal-discussion-toggle portal-secondary-action portal-discussion-toggle-empty";
        }

    }
}
