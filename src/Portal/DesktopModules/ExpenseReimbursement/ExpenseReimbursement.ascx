<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ExpenseReimbursement.ascx.cs" Inherits="ASPNET.StarterKit.Portal.ExpenseReimbursement" %>
<div class="expense-reimbursement">
    <div class="expense-reimbursement-title">费用报销</div>
    <asp:Panel ID="pnlMessage" runat="server" Visible="false">
        <span class="expense-reimbursement-message"><asp:Literal ID="litMessage" runat="server" /></span>
    </asp:Panel>
    <div class="expense-reimbursement-form-grid">
        <div class="expense-reimbursement-form-field expense-reimbursement-form-field-wide">
            <label class="SubHead expense-reimbursement-label">费用类别</label>
            <asp:DropDownList ID="ddlCategory" runat="server" CssClass="NormalTextBox expense-reimbursement-input">
                <asp:ListItem Text="差旅" Value="差旅" />
                <asp:ListItem Text="餐饮" Value="餐饮" />
                <asp:ListItem Text="办公" Value="办公" />
                <asp:ListItem Text="其他" Value="其他" />
            </asp:DropDownList>
        </div>
        <div class="expense-reimbursement-form-field">
            <label class="SubHead expense-reimbursement-label">报销金额</label>
            <asp:TextBox ID="txtAmount" runat="server" CssClass="NormalTextBox expense-reimbursement-input" />
        </div>
        <div class="expense-reimbursement-form-field">
            <label class="SubHead expense-reimbursement-label">发票/附件</label>
            <asp:FileUpload ID="fileAttach" runat="server" CssClass="expense-reimbursement-input" />
        </div>
        <div class="expense-reimbursement-form-field expense-reimbursement-form-field-wide">
            <label class="SubHead expense-reimbursement-label">费用事由</label>
            <asp:TextBox ID="txtReason" runat="server" TextMode="MultiLine" Rows="5" CssClass="NormalTextBox expense-reimbursement-input expense-reimbursement-body" />
        </div>
    </div>
    <div class="expense-reimbursement-actions">
        <asp:Button ID="btnSubmit" runat="server" CssClass="CommandButton" Text="提交报销单" OnClick="btnSubmit_Click" />
    </div>
    <div class="expense-reimbursement-subtitle">我的报销</div>
    <div class="expense-reimbursement-list-wrap">
        <asp:Panel ID="pnlEmpty" runat="server" Visible="false">
            <span class="expense-reimbursement-empty">暂无报销记录</span>
        </asp:Panel>
        <asp:Repeater ID="rptRecent" runat="server" Visible="false">
            <HeaderTemplate>
                <table class="expense-reimbursement-list" cellspacing="0" cellpadding="4" border="0">
                    <tr><th scope="col">编号</th><th scope="col">类别</th><th scope="col">金额</th><th scope="col">状态</th></tr>
            </HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td><%# Eval("RequestId") %></td>
                    <td><%# Eval("Category") %></td>
                    <td><%# Eval("Amount", "{0:N2}") %></td>
                    <td><%# Eval("Status") %></td>
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                </table>
            </FooterTemplate>
        </asp:Repeater>
    </div>
</div>
