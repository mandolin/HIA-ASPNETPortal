<%@ Control Language="c#" Inherits="ASPNET.StarterKit.Portal.Signin" CodeBehind="Signin.ascx.cs" AutoEventWireup="True" %>
<%@ Import Namespace="Resources" %>
<%--
    用户登录控件（SignIn User Control）允许客户端使用 ASP.NET 表单身份验证系统进行身份验证。

    当客户端在其相应的文本框中输入用户名/密码并点击“登录”按钮时，
    在服务器端会执行 LoginBtn_Click 事件处理器，并尝试验证其凭据是否正确。

    如果密码验证成功，那么 LoginBtn_Click 事件处理器会将用户的用户名
    设置在一个加密的 Cookie 中，并重定向回门户首页。

    如果密码验证失败，则显示适当的错误消息。
--%>

<%-- 中文：P7.4 登录区改为真实表单布局和 Button，不再使用旧图片按钮。English: P7.4 uses a real form layout and Button for sign-in instead of legacy image buttons. --%>
<div class="portal-signin-card">
    <div class="portal-signin-title SubSubHead"><%=lang.Signin_accountLogin%></div>

    <div class="portal-field">
        <label class="portal-field-label Normal" for="<%= EmailOrName.ClientID %>"><%=lang.Signin_EmailOrName%></label>
        <asp:TextBox id="EmailOrName" columns="18" cssclass="NormalTextBox portal-field-input" runat="server" />
    </div>

    <div class="portal-field">
        <label class="portal-field-label Normal" for="<%= password.ClientID %>"><%=lang.Signin_password%></label>
        <asp:TextBox id="password" columns="18" textmode="password" cssclass="NormalTextBox portal-field-input" runat="server" />
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
