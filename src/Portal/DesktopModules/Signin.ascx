<%@ Control Language="c#" Inherits="ASPNET.StarterKit.Portal.Signin" CodeBehind="Signin.ascx.cs" AutoEventWireup="True" %>
<%@ Import Namespace="Resources" %>
<%--
    <lang>
        <zh-CN>登录控件负责采集登录标识、口令和记住登录选项，并通过 code-behind 的表单认证流程完成校验、票据写入和失败提示。</zh-CN>
        <en>The sign-in control collects the login identifier, password, and remember-me option, then lets the code-behind forms-authentication flow validate credentials, issue tickets, or display failure messages.</en>
    </lang>
--%>

<%--
    <lang>
        <zh-CN>P7.4 登录区改为真实表单布局和 Button，不再使用旧图片按钮。</zh-CN>
        <en>P7.4 uses a real form layout and Button for sign-in instead of legacy image buttons.</en>
    </lang>
--%>
<div class="portal-signin-card">
    <div class="portal-signin-title SubSubHead"><%=lang.Signin_accountLogin%></div>

    <div class="portal-field">
        <label class="portal-field-label Normal" for="<%= EmailOrName.ClientID %>"><%=lang.Signin_EmailOrName%></label>
        <asp:TextBox id="EmailOrName" columns="18" cssclass="NormalTextBox portal-field-input" runat="server" />
    </div>

    <div class="portal-field">
        <label class="portal-field-label Normal" for="<%= password.ClientID %>"><%=lang.Signin_password%></label>
        <asp:TextBox id="password" columns="18" textmode="password" cssclass="NormalTextBox portal-field-input" runat="server" />
        <asp:HiddenField id="EncryptedPassword" runat="server" />
    </div>

    <div class="portal-checkline">
        <asp:CheckBox id="RememberCheckbox" CssClass="Normal portal-check" Text='<%$ Resources:lang,Signin_rememberLogin %>' runat="server" />
    </div>

    <div class="portal-action-row">
        <asp:Button id="SigninBtn" CssClass="CommandButton portal-primary-action" Text='<%$ Resources:lang,Signin_LoginText %>' runat="server" onclick="LoginBtn_Click" />
        <asp:HyperLink
            id="RegisterLink"
            CssClass="CommandButton portal-secondary-action"
            NavigateUrl="~/Admin/Register.aspx"
            Text='<%$ Resources:lang,Signin_RegisterText %>'
            Visible="false"
            runat="server" />
    </div>

    <asp:Label id="Message" CssClass="NormalRed portal-form-message" runat="server" />
</div>
