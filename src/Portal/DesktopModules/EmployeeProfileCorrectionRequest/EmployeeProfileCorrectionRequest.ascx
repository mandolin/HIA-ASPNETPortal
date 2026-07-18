<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="EmployeeProfileCorrectionRequest.ascx.cs" Inherits="ASPNET.StarterKit.Portal.EmployeeProfileCorrectionRequest" %>

<%-- P6.4.3 业务模块样板：员工提交低敏字段级更正请求，不提供附件、脚本或外部资源。 --%>
<div class="employee-profile-correction">
    <div class="employee-profile-correction-title">员工资料更正请求</div>
    <asp:Label ID="MessageLabel" CssClass="employee-profile-correction-message" runat="server" />

    <asp:Panel ID="RequestPanel" CssClass="employee-profile-correction-panel" Visible="false" runat="server">
        <%-- 中文 / English: 当前资料快照用字段网格展示；提交区仍保留原控件 ID 和事件。 --%>
        <div class="employee-profile-field-grid">
            <div class="employee-profile-field">
                <span class="employee-profile-correction-label employee-profile-field-label">员工号</span>
                <span class="employee-profile-field-value"><asp:Label ID="EmployeeCodeLabel" runat="server" /></span>
            </div>
            <div class="employee-profile-field">
                <span class="employee-profile-correction-label employee-profile-field-label">姓名</span>
                <span class="employee-profile-field-value"><asp:Label ID="DisplayNameLabel" runat="server" /></span>
            </div>
            <div class="employee-profile-field">
                <span class="employee-profile-correction-label employee-profile-field-label">称呼</span>
                <span class="employee-profile-field-value"><asp:Label ID="PreferredNameLabel" runat="server" /></span>
            </div>
            <div class="employee-profile-field">
                <span class="employee-profile-correction-label employee-profile-field-label">工作邮箱</span>
                <span class="employee-profile-field-value"><asp:Label ID="WorkEmailLabel" runat="server" /></span>
            </div>
            <div class="employee-profile-field employee-profile-field-wide">
                <span class="employee-profile-correction-label employee-profile-field-label">组织</span>
                <span class="employee-profile-field-value"><asp:Label ID="OrganizationLabel" runat="server" /></span>
            </div>
        </div>

        <div class="employee-profile-correction-subtitle">提交更正</div>
        <div class="employee-profile-form-grid">
            <div class="employee-profile-form-field">
                <span class="employee-profile-correction-label employee-profile-field-label">更正字段</span>
                <asp:DropDownList ID="FieldNameList" CssClass="NormalTextBox employee-profile-correction-input" runat="server" />
            </div>
            <div class="employee-profile-form-field">
                <span class="employee-profile-correction-label employee-profile-field-label">建议值</span>
                <asp:TextBox ID="ProposedValueTextBox" CssClass="NormalTextBox employee-profile-correction-input" MaxLength="512" runat="server" />
            </div>
            <div class="employee-profile-form-field employee-profile-form-field-wide">
                <span class="employee-profile-correction-label employee-profile-field-label">说明</span>
                <asp:TextBox ID="RequestNoteTextBox" CssClass="NormalTextBox employee-profile-correction-input employee-profile-correction-note"
                    MaxLength="1000" TextMode="MultiLine" Rows="4" runat="server" />
            </div>
            <div class="employee-profile-correction-actions">
                <asp:Button ID="SubmitButton" CssClass="CommandButton" Text="提交更正请求" OnClick="SubmitButton_Click" runat="server" />
            </div>
        </div>

        <div class="employee-profile-correction-subtitle">最近请求</div>
        <div class="employee-profile-list-wrap">
            <asp:Repeater ID="RecentRequestsRepeater" runat="server">
                <HeaderTemplate>
                    <table class="employee-profile-correction-list" cellspacing="0" cellpadding="4" border="0">
                        <tr>
                            <th>UTC</th>
                            <th>字段</th>
                            <th>当前值快照</th>
                            <th>建议值</th>
                            <th>状态</th>
                        </tr>
                </HeaderTemplate>
                <ItemTemplate>
                        <tr>
                            <td><%#: Eval("SubmittedUtcText") %></td>
                            <td><%#: Eval("FieldName") %></td>
                            <td><%#: Eval("CurrentValueSnapshot") %></td>
                            <td><%#: Eval("ProposedValue") %></td>
                            <td><%#: Eval("RequestStatus") %></td>
                        </tr>
                </ItemTemplate>
                <FooterTemplate>
                    </table>
                </FooterTemplate>
            </asp:Repeater>
        </div>
    </asp:Panel>
</div>
