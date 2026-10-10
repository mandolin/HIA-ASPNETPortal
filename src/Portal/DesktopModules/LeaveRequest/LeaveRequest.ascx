<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="LeaveRequest.ascx.cs" Inherits="ASPNET.StarterKit.Portal.LeaveRequest" %>
<div class="leave-request">
    <div class="leave-request-title">请假申请</div>
    <asp:Panel ID="pnlMessage" runat="server" Visible="false">
        <span class="leave-request-message"><asp:Literal ID="litMessage" runat="server" /></span>
    </asp:Panel>
    <div class="leave-request-form-grid">
        <div class="leave-request-form-field leave-request-form-field-wide">
            <label class="SubHead leave-request-label">请假类型</label>
            <asp:DropDownList ID="ddlLeaveType" runat="server" CssClass="NormalTextBox leave-request-input">
                <asp:ListItem Text="年假" Value="年假" />
                <asp:ListItem Text="事假" Value="事假" />
                <asp:ListItem Text="病假" Value="病假" />
                <asp:ListItem Text="调休" Value="调休" />
            </asp:DropDownList>
        </div>
        <div class="leave-request-form-field">
            <label class="SubHead leave-request-label">开始日期</label>
            <asp:TextBox ID="txtStartDate" runat="server" TextMode="Date" CssClass="NormalTextBox leave-request-input" />
        </div>
        <div class="leave-request-form-field">
            <label class="SubHead leave-request-label">结束日期</label>
            <asp:TextBox ID="txtEndDate" runat="server" TextMode="Date" CssClass="NormalTextBox leave-request-input" />
        </div>
        <div class="leave-request-form-field">
            <label class="SubHead leave-request-label">请假天数</label>
            <asp:TextBox ID="txtDays" runat="server" CssClass="NormalTextBox leave-request-input" />
        </div>
        <div class="leave-request-form-field">
            <label class="SubHead leave-request-label">请假附件</label>
            <asp:FileUpload ID="fileAttach" runat="server" CssClass="leave-request-input" />
        </div>
        <div class="leave-request-form-field leave-request-form-field-wide">
            <label class="SubHead leave-request-label">请假事由</label>
            <asp:TextBox ID="txtReason" runat="server" TextMode="MultiLine" Rows="5" CssClass="NormalTextBox leave-request-input leave-request-body" />
        </div>
    </div>
    <div class="leave-request-actions">
        <asp:Button ID="btnSubmit" runat="server" CssClass="CommandButton" Text="提交申请" OnClick="btnSubmit_Click" />
    </div>
    <div class="leave-request-subtitle">我的请假</div>
    <div class="leave-request-list-wrap">
        <asp:Panel ID="pnlEmpty" runat="server" Visible="false">
            <span class="leave-request-empty">暂无请假记录</span>
        </asp:Panel>
        <asp:Repeater ID="rptRecent" runat="server" Visible="false">
            <HeaderTemplate>
                <table class="leave-request-list" cellspacing="0" cellpadding="4" border="0">
                    <tr><th scope="col">编号</th><th scope="col">类型</th><th scope="col">起止</th><th scope="col">天数</th><th scope="col">状态</th></tr>
            </HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td><%# Eval("RequestId") %></td>
                    <td><%# Eval("LeaveType") %></td>
                    <td><%# Eval("StartDate", "{0:yyyy-MM-dd}") %> ~ <%# Eval("EndDate", "{0:yyyy-MM-dd}") %></td>
                    <td><%# Eval("Days") %></td>
                    <td><%# Eval("Status") %></td>
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                </table>
            </FooterTemplate>
        </asp:Repeater>
    </div>
</div>
