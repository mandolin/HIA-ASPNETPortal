<%@ Page Language="c#" CodeBehind="Register.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.Register"
    MasterPageFile="~/Default.master" %>

<%--
   注释：
   Register.aspx 页面用于使客户端能够在门户系统中注册一个新的唯一用户名和密码。
   页面包含一个服务器事件处理器 -- RegisterBtn_Click -- 在页面的注册按钮被点击时执行。

   Register.aspx 页面使用 UsersDB 类来管理实际的账户创建。
   注意：用户名和密码存储在一个SQL数据库的表中。
--%>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 页面主体表格 --%>
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr>
            <td width="150">
                &nbsp;
            </td>
            <td width="*">
                <table cellpadding="2" cellspacing="1" border="0">
                    <tr>
                        <td width="450">
                            <table width="100%" cellspacing="0" cellpadding="0">
                                <tr>
                                    <td>
                                        <%-- 标题 --%>
                                        <span class="Head">Create a New Account </span>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <%-- 水平线 --%>
                                        <hr noshade size="1">
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <%-- 用户信息输入表格 --%>
                    <tr valign="top">
                        <td class="Normal">
                            <%-- 姓名输入 --%>
                            Name:
                            <br>
                            <asp:TextBox size="25" ID="Name" runat="server" />
                            &nbsp;
                            <%-- todo 此处可以增加正则表达式验证 --%>
                            <asp:RequiredFieldValidator ControlToValidate="Name" ErrorMessage="'Name' must not be left blank."
                                runat="server" ID="RequiredFieldValidator1" />
                            <p>
                            <%-- 邮箱输入 --%>
                            Email:
                            <br>
                            <asp:TextBox size="25" ID="Email" runat="server" />
                            &nbsp;
                            <%-- 邮箱格式验证 --%>
                            <asp:RegularExpressionValidator ControlToValidate="Email" ValidationExpression="[\w\.-]+(\+[\w-]*)?@([\w-]+\.)+[\w-]+"
                                Display="Dynamic" ErrorMessage="Must use a valid email address." runat="server"
                                ID="RegularExpressionValidator1" />
                            <%-- 必填项验证 --%>
                            <asp:RequiredFieldValidator ControlToValidate="Email" ErrorMessage="'Email' must not be left blank."
                                runat="server" ID="RequiredFieldValidator2" />
                            <p>
                            <%-- 员工号：企业临时注册链接流程中必填，普通自注册流程暂不强制。 --%>
                            Employee Code:
                            <asp:Label ID="EmployeeCodeRequiredHint" CssClass="NormalRed" Text="*" Visible="false" runat="server" />
                            <br>
                            <asp:TextBox size="25" ID="EmployeeCode" runat="server" />
                            &nbsp;
                            <asp:RequiredFieldValidator ControlToValidate="EmployeeCode" Display="Dynamic"
                                ErrorMessage="'Employee Code' must not be left blank for invitation registration."
                                Enabled="false" runat="server" ID="EmployeeCodeRequiredValidator" />
                            <p>
                            <%-- 密码输入 --%>
                            Password:
                            <br>
                            <asp:TextBox size="25" ID="Password" TextMode="Password" runat="server" />
                            &nbsp;
                            <%-- 必填项验证 --%>
                            <asp:RequiredFieldValidator ControlToValidate="Password" ErrorMessage="'Password' must not be left blank."
                                runat="server" ID="RequiredFieldValidator3" />
                            <p>
                            <%-- 确认密码输入 --%>
                            Confirm Password:
                            <br>
                            <asp:TextBox size="25" ID="ConfirmPassword" TextMode="Password" runat="server" />
                            &nbsp;
                            <%-- 必填项验证 --%>
                            <asp:RequiredFieldValidator ControlToValidate="ConfirmPassword" Display="Dynamic"
                                ErrorMessage="'Confirm' must not be left blank." runat="server" ID="RequiredFieldValidator4" />
                            <%-- 密码匹配验证 --%>
                            <asp:CompareValidator ControlToValidate="ConfirmPassword" ControlToCompare="Password"
                                ErrorMessage="Password fields do not match." runat="server" ID="CompareValidator1" />
                            <p>
                            <%-- 注册按钮 --%>
                            <asp:LinkButton class="CommandButton" Text="Submit Registration" runat="server"
                                ID="RegisterBtn" OnClick="RegisterBtn_Click" />
                            <br>
                            <br>
                            <p>
                                <%-- 显示消息标签 --%>
                                <asp:Label ID="Message" CssClass="NormalRed" runat="server" />
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</asp:Content>
