<%@ Page
    Language="c#"
    CodeBehind="CollaborationItems.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.CollaborationItems"
    MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>

<%--
  <lang>
    <zh-CN>P21.3 企业协同事项后台页用于验证泛化企业能力对象，不承载具体领域字段。</zh-CN>
    <en>The P21.3 enterprise collaboration-item Admin page validates the generalized enterprise capability object and does not carry domain-specific fields.</en>
  </lang>
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="portal-admin-page portal-admin-collaboration-items">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title"><%= lang.Admin_CollaborationItems_Title %></h1>
                <p class="Normal portal-admin-subtitle"><%= lang.Admin_CollaborationItems_Subtitle %></p>
            </div>
            <%= PortalNavigationEntryRenderer.RenderActions("Admin.CollaborationItems", Context) %>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" EnableViewState="false" runat="server" />

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title"><%= lang.Admin_CollaborationItems_SectionCreate %></h2>
            </div>
            <div class="portal-form-grid">
                <%--
                    <lang>
                        <zh-CN>创建表单承载类型、责任角色、优先级、截止时间和正文输入；规范化、角色授权、时间单位和低敏边界不由标记层决定。</zh-CN>
                        <en>The create form carries type, owner role, priority, due time, and content inputs; normalization, role authorization, time units, and low-sensitivity boundaries are not decided by markup.</en>
                    </lang>
                --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_CollaborationItems_LabelTypeKey %></span>
                    <asp:DropDownList ID="ItemTypeList" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_CollaborationItems_LabelOwnerRole %></span>
                    <asp:TextBox ID="OwnerRoleKeyTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="120" Text="Business.Collaboration.Handle" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_CollaborationItems_LabelPriority %></span>
                    <asp:DropDownList ID="PriorityList" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_CollaborationItems_LabelDueUtc %></span>
                    <asp:TextBox ID="DueUtcTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="19" runat="server" />
                </div>
                <%--
                    <lang>
                      <zh-CN>P47.4 父事项输入：空白表示顶层事项；存在性、终态与层级深度校验由数据层承担，标记层只收集低敏编号文本。</zh-CN>
                      <en>P47.4 parent item input: blank marks a top-level item; existence, terminal state, and depth checks belong to the data layer, while markup only collects the low-sensitivity code text.</en>
                    </lang>
                --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_CollaborationItems_LabelParentItem %></span>
                    <asp:TextBox ID="ParentItemTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="19" runat="server" />
                </div>
                <div class="portal-form-field portal-form-field-wide">
                    <span class="SubHead portal-form-label"><%= lang.Admin_CollaborationItems_LabelTitle %></span>
                    <asp:TextBox ID="TitleTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="200" runat="server" />
                </div>
                <div class="portal-form-field portal-form-field-wide">
                    <span class="SubHead portal-form-label"><%= lang.Admin_CollaborationItems_LabelSummary %></span>
                    <asp:TextBox ID="SummaryTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="500" runat="server" />
                </div>
                <div class="portal-form-field portal-form-field-wide">
                    <span class="SubHead portal-form-label"><%= lang.Admin_CollaborationItems_LabelDescription %></span>
                    <asp:TextBox ID="DescriptionTextBox" CssClass="NormalTextBox portal-form-input" TextMode="MultiLine" Rows="4" runat="server" />
                </div>
                <div class="portal-form-actions">
                    <asp:Button
                        ID="CreateButton"
                        Text="<%$ Resources:lang, Admin_CollaborationItems_ButtonCreateAndSubmit %>"
                        CssClass="CommandButton"
                        CausesValidation="False"
                        OnClick="CreateButton_Click"
                        runat="server" />
                </div>
            </div>
        </div>

        <div class="portal-admin-section portal-filter-panel">
            <%--
                <lang>
                    <zh-CN>列表状态筛选与 SearchButton_Click 只刷新服务器提供的事项集合，不把筛选值当作工作流权限或状态迁移命令。</zh-CN>
                    <en>The list status filter and SearchButton_Click only refresh the server-provided item set; filter values are not workflow permissions or transition commands.</en>
                </lang>
            --%>
            <div class="portal-filter-grid">
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_CollaborationItems_LabelStatus %></span>
                    <asp:DropDownList ID="StatusFilterList" CssClass="NormalTextBox portal-filter-input" runat="server" />
                </div>
                <div class="portal-filter-actions">
                    <asp:Button
                        ID="SearchButton"
                        Text="<%$ Resources:lang, Admin_CollaborationItems_ButtonSearch %>"
                        CssClass="CommandButton"
                        CausesValidation="False"
                        OnClick="SearchButton_Click"
                        runat="server" />
                </div>
            </div>
        </div>

        <div class="portal-status-strip">
            <div class="Normal portal-status-line">
                <asp:Label ID="ResultLabel" runat="server" />
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title"><%= lang.Admin_CollaborationItems_SectionList %></h2>
            </div>
            <div class="portal-table-wrap">
                <%--
                    <lang>
                        <zh-CN>事项 Repeater 以 ItemId 绑定命令目标并编码展示时间线/正文；动作按钮和评论文本仍受服务器状态、角色和审计约束。</zh-CN>
                        <en>The item Repeater binds ItemId as the command target and encodes timeline/content output; action buttons and comments remain constrained by server state, roles, and audit rules.</en>
                    </lang>
                --%>
                <asp:Repeater ID="ItemsRepeater" OnItemCommand="ItemsRepeater_ItemCommand" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <%--
                                <lang>
                                  <zh-CN>P79.3 列宽预算：仅“编号”保留固定 70px（窄标识列，无文本增长需求），其余八列改相对单位且合计 90%，为固定列预留约 5% 余量；百分比合计超过 100% 时浏览器会挤压固定列，标识列将被折行。配合 .portal-data-table 的 overflow-wrap 规则，单元格内容在 200% 文字缩放下可重排而不裁切。</zh-CN>
                                  <en>P79.3 column-width budget: only "Id" keeps a fixed 70px (narrow identifier column with no text-growth need); the other eight columns use relative units totalling 90% so roughly 5% stays as headroom for the fixed column, because a percentage total above 100% makes the browser squeeze the fixed column and wrap the identifier. Together with the .portal-data-table overflow-wrap rules, cell content reflows instead of clipping at 200% text zoom.</en>
                                </lang>
                            --%>
                            <tr>
                                <th scope="col" width="70" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnId %></th>
                                <th scope="col" width="12%" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnActionUtc %></th>
                                <th scope="col" width="10%" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnCode %></th>
                                <th scope="col" width="10%" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnOwner %></th>
                                <th scope="col" width="14%" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnItem %></th>
                                <th scope="col" width="8%" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnStatus %></th>
                                <th scope="col" width="14%" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnHandleComment %></th>
                                <th scope="col" width="12%" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnActions %></th>
                                <th scope="col" width="10%" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnParticipants %></th>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("ItemId") %></td>
                                <td><%#: Eval("LastActionUtcText") %></td>
                                <td><%#: Eval("ItemCode") %></td>
                                <td><%#: Eval("OwnerText") %></td>
                                <td>
                                    <div class="portal-value-stack">
                                        <div><span class="SubHead"><%= lang.Admin_CollaborationItems_InlineTitle %></span><%# Convert.ToBoolean(Eval("HasParentItem")) ? " &#9656; " : " " %><%#: Eval("Title") %></div>
                                        <div><span class="SubHead"><%= lang.Admin_CollaborationItems_InlineType %></span> <%#: Eval("ItemTypeKey") %></div>
                                        <div><span class="SubHead"><%= lang.Admin_CollaborationItems_InlinePriority %></span> <%#: Eval("PriorityKey") %></div>
                                        <div><span class="SubHead"><%= lang.Admin_CollaborationItems_InlineSummary %></span> <%#: Eval("Summary") %></div>
                                        <div><span class="SubHead"><%= lang.Admin_CollaborationItems_InlineDescription %></span> <%#: Eval("Description") %></div>
                                        <div><span class="SubHead"><%= lang.Admin_CollaborationItems_InlineWorkflowComment %></span> <%#: Eval("LastActionComment") %></div>
                                        <div><span class="SubHead"><%= lang.Admin_CollaborationItems_InlineTimelineComment %></span> <%#: Eval("LatestVisibleComment") %></div>
                                    </div>
                                </td>
                                <td><%#: Eval("ItemStatus") %></td>
                                <td>
                                    <%--
                                        <lang>
                                          <zh-CN>P79.3 原先的单个 390px 单元格混装“处理意见输入 + 9 个状态动作 + 参与人增删”三类职责，内容远超列宽导致行高被撑到数百像素、参与人控件被挤成竖排。现按职责拆成三个独立列（处理意见 / 操作 / 参与人），列序即流程序；表格仍可整体横向滚动（WCAG SC 1.4.10 允许 data tables 用 2D 布局），但单个单元格内容必须可重排。意见框改用相对宽度，随所在列伸缩而不写死像素。</zh-CN>
                                          <en>P79.3 the former single 390px cell mixed three responsibilities — handling-comment input, nine status actions, and participant add/remove — so its content far exceeded the column width, inflating row height to hundreds of pixels and squeezing the participant controls into a vertical stack. They are now three single-responsibility columns (handling comment / actions / participants) whose order follows the workflow; the table may still scroll horizontally as a whole (WCAG SC 1.4.10 permits 2D layout for data tables), but content inside a single cell must reflow. The comment box now uses a relative width so it follows its column instead of a hard-coded pixel size.</en>
                                        </lang>
                                    --%>
                                    <asp:TextBox ID="ActionCommentTextBox" CssClass="NormalTextBox portal-review-note" Width="100%" MaxLength="1000" TextMode="MultiLine" Rows="3" runat="server" />
                                </td>
                                <td>
                                    <div class="portal-row-actions">
                                        <asp:Button ID="StartButton" Text="<%$ Resources:lang, Admin_CollaborationItems_ButtonStart %>" CssClass="CommandButton" CommandName="Start" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="CompleteButton" Text="<%$ Resources:lang, Admin_CollaborationItems_ButtonComplete %>" CssClass="CommandButton" CommandName="Complete" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="ReturnButton" Text="<%$ Resources:lang, Admin_CollaborationItems_ButtonReturn %>" CssClass="CommandButton" CommandName="Return" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="ResubmitButton" Text="<%$ Resources:lang, Admin_CollaborationItems_ButtonResubmit %>" CssClass="CommandButton" CommandName="Resubmit" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="RejectButton" Text="<%$ Resources:lang, Admin_CollaborationItems_ButtonReject %>" CssClass="CommandButton CommandButtonDanger" CommandName="Reject" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="CancelButton" Text="<%$ Resources:lang, Admin_CollaborationItems_ButtonCancel %>" CssClass="CommandButton CommandButtonDanger" CommandName="Cancel" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="CloseButton" Text="<%$ Resources:lang, Admin_CollaborationItems_ButtonClose %>" CssClass="CommandButton" CommandName="Close" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="AddParticipantCommentButton" Text="<%$ Resources:lang, Admin_CollaborationItems_ButtonParticipantComment %>" CssClass="CommandButton" CommandName="AddParticipantComment" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="AddAdministratorCommentButton" Text="<%$ Resources:lang, Admin_CollaborationItems_ButtonAdministratorComment %>" CssClass="CommandButton" CommandName="AddAdministratorComment" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                    </div>
                                </td>
                                <td>
                                    <%--
                                        <lang>
                                          <zh-CN>P47.4 参与人增删：用户标识与角色来自当前行控件，添加与移除共用同一输入；授权与重复校验由数据层承担，标记层不做授权判断。</zh-CN>
                                          <en>P47.4 participant add/remove: user id and role come from the current row controls and both commands share one input; authorization and duplication checks belong to the data layer, and markup performs no authorization decision.</en>
                                        </lang>
                                    --%>
                                    <%--
                                        <lang>
                                          <zh-CN>P79.3 已参与人只读列表：列头已标明“参与人”，格内不再重复行内标签（否则同一信息两处呈现）。文本由共享渲染器 PortalCollaborationParticipantText 产出“用户名（本地化角色名）”的逗号串，空集合回落为本地化占位文本；列宽较窄，靠 .portal-data-table 的 overflow-wrap 规则换行而不裁切。本轮只做呈现重排，不改数据层取数与拼接。</zh-CN>
                                          <en>P79.3 read-only participant list: the column header already says "Participants", so the cell no longer repeats an inline label, which would present the same information twice. The text comes from the shared renderer PortalCollaborationParticipantText as a comma-joined "user name (localized role name)" string, falling back to the localized placeholder for an empty set; the column is narrow, so the .portal-data-table overflow-wrap rules wrap it instead of clipping. This round only rearranges presentation and does not change data-layer fetching or composition.</en>
                                        </lang>
                                    --%>
                                    <div class="portal-participant-actions">
                                        <div><%#: Eval("ParticipantsText") %></div>
                                        <span class="SubHead"><%= lang.Admin_CollaborationItems_LabelParticipantUser %></span>
                                        <asp:TextBox ID="ParticipantUserTextBox" CssClass="NormalTextBox" Width="100%" MaxLength="10" runat="server" />
                                        <%--
                                            <zh-CN>参与人角色下拉：显示名本地化为 RoleType 键，Value 保持角色类型键（不可翻译）。宽度取相对单位，与用户标识输入框同宽；下拉的固有宽度由最长选项文本决定，写死不设宽度时在 10% 的独立列里会溢出所在单元格（P79.4 实测溢出 6px）。</zh-CN>
                                            <en>Participant role dropdown: display names localized via RoleType keys; Value keeps the untranslatable role-type key. The width is relative so it matches the user-id input; a dropdown's intrinsic width comes from its longest option text, and without a width it overflowed its cell in the 10% standalone column (6px measured in P79.4).</en>
                                        --%>
                                        <asp:DropDownList ID="ParticipantRoleList" CssClass="NormalTextBox" Width="100%" runat="server">
                                            <asp:ListItem Text="<%$ Resources:lang, Collaboration_ParticipantRole_Collaborator %>" Value="Collaborator" Selected="True" />
                                            <asp:ListItem Text="<%$ Resources:lang, Collaboration_ParticipantRole_Watcher %>" Value="Watcher" />
                                        </asp:DropDownList>
                                        <asp:Button ID="AddParticipantButton" Text="<%$ Resources:lang, Admin_CollaborationItems_ButtonAddParticipant %>" CssClass="CommandButton" CommandName="AddParticipant" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="RemoveParticipantButton" Text="<%$ Resources:lang, Admin_CollaborationItems_ButtonRemoveParticipant %>" CssClass="CommandButton" CommandName="RemoveParticipant" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                    </div>
                                </td>
                            </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        <%--
                            <lang>
                                <zh-CN>P74.3 空态：零条时在表尾渲染"表头 + 提示行"，文案按是否处于筛选态区分；失败路径由 EmptyStateRowCount 为 null 抑制，避免与错误提示矛盾。列数（P79.3 起为 9）与本表表头一致。</zh-CN>
                                <en>P74.3 empty state: with zero rows the footer renders the header-plus-hint row whose wording depends on whether a filter is active; failure paths suppress it by passing a null count so it cannot contradict the error message. The column count (9 since P79.3) matches this table's header.</en>
                                </lang>
                                --%>
                                <%= PortalEmptyStateRenderer.Render(EmptyStateRowCount, EmptyStateText, 9) %>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </div>
        </div>
    </div>
</asp:Content>
