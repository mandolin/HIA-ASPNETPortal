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

<hr noshade size="1" width="98%">
<%-- 登录标题 --%>
<span class="SubSubHead" style="HEIGHT: 20px"><%=lang.Signin_accountLogin%></span>
<br>
<%-- 用户名或邮箱提示 --%>
<span class="Normal"><%=lang.Signin_EmailOrName%></span>
<br>
<%-- 用户名或邮箱输入框 --%>
<asp:TextBox id="EmailOrName" columns="9" width="130" cssclass="NormalTextBox" runat="server" />
<br>
<%-- 密码提示 --%>
<span class="Normal"><%=lang.Signin_password%></span>
<br>
<%-- 密码输入框 --%>
<asp:TextBox id="password" columns="9" width="130" textmode="password" cssclass="NormalTextBox" runat="server" />
<br>
<%-- 记住登录复选框 --%>
<asp:CheckBox id="RememberCheckbox" class="Normal" Text='<%$ Resources:lang,Signin_rememberLogin %>' runat="server" />
<table width="100%" cellspacing="0" cellpadding="4" border="0">
    <tr>
        <td>
            <%-- 登录按钮 --%>
            <asp:ImageButton id="SigninBtn" ImageUrl="<%$ Resources:lang,Signin_LoginImg %>" runat="server" onclick="LoginBtn_Click" />
            <br>
            <%-- 注册链接 --%>
            <a href="Admin/Register.aspx"><img src="<%=lang.Signin_RegImg%>" border="0"></a>
            <%-- 错误消息标签 --%>
            <asp:Label id="Message" class="NormalRed" runat="server" />
        </td>
    </tr>
</table>
<br>