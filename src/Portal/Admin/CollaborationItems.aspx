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
                            <tr>
                                <th scope="col" width="70" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnId %></th>
                                <th scope="col" width="145" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnActionUtc %></th>
                                <th scope="col" width="155" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnCode %></th>
                                <th scope="col" width="140" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnOwner %></th>
                                <th scope="col" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnItem %></th>
                                <th scope="col" width="110" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnStatus %></th>
                                <th scope="col" width="390" class="SubHead"><%= lang.Admin_CollaborationItems_ColumnHandleComment %></th>
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
                                        <div><span class="SubHead"><%= lang.Admin_CollaborationItems_LabelParticipants %></span> <%#: Eval("ParticipantsText") %></div>
                                    </div>
                                </td>
                                <td><%#: Eval("ItemStatus") %></td>
                                <td>
                                    <asp:TextBox ID="ActionCommentTextBox" CssClass="NormalTextBox portal-review-note" Width="280" MaxLength="1000" TextMode="MultiLine" Rows="3" runat="server" />
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
                                    <%--
                                        <lang>
                                          <zh-CN>P47.4 参与人增删：用户标识与角色来自当前行控件，添加与移除共用同一输入；授权与重复校验由数据层承担，标记层不做授权判断。</zh-CN>
                                          <en>P47.4 participant add/remove: user id and role come from the current row controls and both commands share one input; authorization and duplication checks belong to the data layer, and markup performs no authorization decision.</en>
                                        </lang>
                                    --%>
                                    <div class="portal-participant-actions">
                                        <span class="SubHead"><%= lang.Admin_CollaborationItems_LabelParticipantUser %></span>
                                        <asp:TextBox ID="ParticipantUserTextBox" CssClass="NormalTextBox" Width="60" MaxLength="10" runat="server" />
                                        <%--
                                            <zh-CN>参与人角色下拉：显示名本地化为 RoleType 键，Value 保持角色类型键（不可翻译）。</zh-CN>
                                            <en>Participant role dropdown: display names localized via RoleType keys; Value keeps the untranslatable role-type key.</en>
                                        --%>
                                        <asp:DropDownList ID="ParticipantRoleList" CssClass="NormalTextBox" runat="server">
                                            <asp:ListItem Text="<%$ Resources:lang, Admin_CollaborationItems_RoleType_Collaborator %>" Value="Collaborator" Selected="True" />
                                            <asp:ListItem Text="<%$ Resources:lang, Admin_CollaborationItems_RoleType_Watcher %>" Value="Watcher" />
                                        </asp:DropDownList>
                                        <asp:Button ID="AddParticipantButton" Text="<%$ Resources:lang, Admin_CollaborationItems_ButtonAddParticipant %>" CssClass="CommandButton" CommandName="AddParticipant" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="RemoveParticipantButton" Text="<%$ Resources:lang, Admin_CollaborationItems_ButtonRemoveParticipant %>" CssClass="CommandButton" CommandName="RemoveParticipant" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                    </div>
                                </td>
                            </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </div>
        </div>
    </div>
</asp:Content>
