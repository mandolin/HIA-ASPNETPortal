<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Signin" CodeBehind="Signin.ascx.cs" AutoEventWireup="True" %>
<%@ Import Namespace="Resources" %>
<%--

   The SignIn User Control enables clients to authenticate themselves using 
   the ASP.NET Forms based authentication system.

   When a client enters their username/password within the appropriate
   textboxes and clicks the "Login" button, the LoginBtn_Click event
   handler executes on the server and attempts to validate their
   credentials against a SQL database.

   If the password check succeeds, then the LoginBtn_Click event handler
   sets the customers username in an encrypted cookieID and redirects
   back to the portal home page.

   If the password check fails, then an appropriate error message
   is displayed.

--%>

<hr noshade size="1" width="98%">
<span class="SubSubHead" style="HEIGHT: 20px"><%=lang.Signin_accountLogin%></span>
<br>
<span class="Normal"><%=lang.Signin_email%></span>
<br>
<asp:TextBox id="email" columns="9" width="130" cssclass="NormalTextBox" runat="server" />
<br>
<span class="Normal"><%=lang.Signin_password%></span>
<br>
<asp:TextBox id="password" columns="9" width="130" textmode="password" cssclass="NormalTextBox" runat="server" />
<br>
<asp:checkbox id="RememberCheckbox" class="Normal" Text='<%$ Resources:lang,Signin_rememberLogin %>' runat="server" />
<table width="100%" cellspacing="0" cellpadding="4" border="0">
    <tr>
        <td>
            <asp:ImageButton id="SigninBtn" ImageUrl="<%$ Resources:lang,Signin_LoginImg %>" runat="server" onclick="LoginBtn_Click" />
            <br>
            <a href="Admin/Register.aspx"><img src="<%=lang.Signin_RegImg%>" border="0"></a>
            <asp:label id="Message" class="NormalRed" runat="server" />
        </td>
    </tr>
</table>
<br>