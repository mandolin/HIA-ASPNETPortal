<%@ Page
    Language="c#"
    CodeBehind="CollaborationItems.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.CollaborationItems"
    MasterPageFile="~/Default.master" %>

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
                <h1 class="Head portal-admin-title">Collaboration Items</h1>
                <p class="Normal portal-admin-subtitle">Create and handle low-sensitivity enterprise collaboration items.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="WorkItems.aspx">Work Items</a>
                <a class="CommandButton" href="BusinessApplications.aspx">Business Applications</a>
                <a class="CommandButton" href="OperationAudits.aspx">Operation Audits</a>
            </div>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" EnableViewState="false" runat="server" />

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Create Collaboration Item</h2>
            </div>
            <div class="portal-form-grid">
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Type Key</span>
                    <asp:DropDownList ID="ItemTypeList" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Owner Role</span>
                    <asp:TextBox ID="OwnerRoleKeyTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="120" Text="Business.Collaboration.Handle" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Priority</span>
                    <asp:DropDownList ID="PriorityList" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Due UTC</span>
                    <asp:TextBox ID="DueUtcTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="19" runat="server" />
                </div>
                <div class="portal-form-field portal-form-field-wide">
                    <span class="SubHead portal-form-label">Title</span>
                    <asp:TextBox ID="TitleTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="200" runat="server" />
                </div>
                <div class="portal-form-field portal-form-field-wide">
                    <span class="SubHead portal-form-label">Summary</span>
                    <asp:TextBox ID="SummaryTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="500" runat="server" />
                </div>
                <div class="portal-form-field portal-form-field-wide">
                    <span class="SubHead portal-form-label">Description</span>
                    <asp:TextBox ID="DescriptionTextBox" CssClass="NormalTextBox portal-form-input" TextMode="MultiLine" Rows="4" runat="server" />
                </div>
                <div class="portal-form-actions">
                    <asp:Button
                        ID="CreateButton"
                        Text="Create and Submit"
                        CssClass="CommandButton"
                        CausesValidation="False"
                        OnClick="CreateButton_Click"
                        runat="server" />
                </div>
            </div>
        </div>

        <div class="portal-admin-section portal-filter-panel">
            <div class="portal-filter-grid">
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label">Status</span>
                    <asp:DropDownList ID="StatusFilterList" CssClass="NormalTextBox portal-filter-input" runat="server" />
                </div>
                <div class="portal-filter-actions">
                    <asp:Button
                        ID="SearchButton"
                        Text="Search"
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
                <h2 class="Head portal-section-title">Collaboration Item List</h2>
            </div>
            <div class="portal-table-wrap">
                <asp:Repeater ID="ItemsRepeater" OnItemCommand="ItemsRepeater_ItemCommand" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="70" class="SubHead">ID</th>
                                <th scope="col" width="145" class="SubHead">Action UTC</th>
                                <th scope="col" width="155" class="SubHead">Code</th>
                                <th scope="col" width="140" class="SubHead">Owner</th>
                                <th scope="col" class="SubHead">Item</th>
                                <th scope="col" width="110" class="SubHead">Status</th>
                                <th scope="col" width="390" class="SubHead">Handle / Comment</th>
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
                                        <div><span class="SubHead">Title:</span> <%#: Eval("Title") %></div>
                                        <div><span class="SubHead">Type:</span> <%#: Eval("ItemTypeKey") %></div>
                                        <div><span class="SubHead">Priority:</span> <%#: Eval("PriorityKey") %></div>
                                        <div><span class="SubHead">Summary:</span> <%#: Eval("Summary") %></div>
                                        <div><span class="SubHead">Description:</span> <%#: Eval("Description") %></div>
                                        <div><span class="SubHead">Latest Workflow Comment:</span> <%#: Eval("LastActionComment") %></div>
                                        <div><span class="SubHead">Latest Visible Timeline Comment:</span> <%#: Eval("LatestVisibleComment") %></div>
                                    </div>
                                </td>
                                <td><%#: Eval("ItemStatus") %></td>
                                <td>
                                    <asp:TextBox ID="ActionCommentTextBox" CssClass="NormalTextBox portal-review-note" Width="280" MaxLength="1000" TextMode="MultiLine" Rows="3" runat="server" />
                                    <div class="portal-row-actions">
                                        <asp:Button ID="StartButton" Text="Start" CssClass="CommandButton" CommandName="Start" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="CompleteButton" Text="Complete" CssClass="CommandButton" CommandName="Complete" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="ReturnButton" Text="Return" CssClass="CommandButton" CommandName="Return" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="ResubmitButton" Text="Resubmit" CssClass="CommandButton" CommandName="Resubmit" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="RejectButton" Text="Reject" CssClass="CommandButton CommandButtonDanger" CommandName="Reject" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="CancelButton" Text="Cancel" CssClass="CommandButton CommandButtonDanger" CommandName="Cancel" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="CloseButton" Text="Close" CssClass="CommandButton" CommandName="Close" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="AddParticipantCommentButton" Text="Participant Comment" CssClass="CommandButton" CommandName="AddParticipantComment" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="AddAdministratorCommentButton" Text="Administrator Comment" CssClass="CommandButton" CommandName="AddAdministratorComment" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
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
