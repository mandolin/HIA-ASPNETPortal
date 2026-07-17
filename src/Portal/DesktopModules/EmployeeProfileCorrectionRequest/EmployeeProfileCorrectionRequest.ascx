<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="EmployeeProfileCorrectionRequest.ascx.cs" Inherits="ASPNET.StarterKit.Portal.EmployeeProfileCorrectionRequest" %>

<%-- P6.4.3 业务模块样板：员工提交低敏字段级更正请求，不提供附件、脚本或外部资源。 --%>
<div class="employee-profile-correction">
    <div class="employee-profile-correction-title">员工资料更正请求</div>
    <asp:Label ID="MessageLabel" CssClass="employee-profile-correction-message" runat="server" />

    <asp:Panel ID="RequestPanel" CssClass="employee-profile-correction-panel" Visible="false" runat="server">
        <table class="employee-profile-correction-table" cellspacing="0" cellpadding="4" border="0">
            <tr>
                <td class="employee-profile-correction-label">员工号</td>
                <td><asp:Label ID="EmployeeCodeLabel" runat="server" /></td>
            </tr>
            <tr>
                <td class="employee-profile-correction-label">姓名</td>
                <td><asp:Label ID="DisplayNameLabel" runat="server" /></td>
            </tr>
            <tr>
                <td class="employee-profile-correction-label">称呼</td>
                <td><asp:Label ID="PreferredNameLabel" runat="server" /></td>
            </tr>
            <tr>
                <td class="employee-profile-correction-label">工作邮箱</td>
                <td><asp:Label ID="WorkEmailLabel" runat="server" /></td>
            </tr>
            <tr>
                <td class="employee-profile-correction-label">组织</td>
                <td><asp:Label ID="OrganizationLabel" runat="server" /></td>
            </tr>
        </table>

        <table class="employee-profile-correction-form" cellspacing="0" cellpadding="4" border="0">
            <tr>
                <td class="employee-profile-correction-label">更正字段</td>
                <td><asp:DropDownList ID="FieldNameList" CssClass="NormalTextBox" runat="server" /></td>
            </tr>
            <tr>
                <td class="employee-profile-correction-label">建议值</td>
                <td><asp:TextBox ID="ProposedValueTextBox" CssClass="NormalTextBox" Width="360" MaxLength="512" runat="server" /></td>
            </tr>
            <tr>
                <td class="employee-profile-correction-label">说明</td>
                <td><asp:TextBox ID="RequestNoteTextBox" CssClass="NormalTextBox" Width="360" MaxLength="1000" TextMode="MultiLine" Rows="4" runat="server" /></td>
            </tr>
            <tr>
                <td></td>
                <td><asp:Button ID="SubmitButton" CssClass="CommandButton" Text="提交更正请求" OnClick="SubmitButton_Click" runat="server" /></td>
            </tr>
        </table>

        <div class="employee-profile-correction-subtitle">最近请求</div>
        <asp:Repeater ID="RecentRequestsRepeater" runat="server">
            <HeaderTemplate>
                <table class="employee-profile-correction-list" cellspacing="0" cellpadding="4" border="1">
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
    </asp:Panel>
</div>
