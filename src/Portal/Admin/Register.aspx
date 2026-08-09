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
                            <%--
                              <lang>
                                <zh-CN>注册字段和验证器只构造一次性账号申请；唯一性、邀请员工号策略、规范化和账户写入仍由服务器完成。</zh-CN>
                                <en>Registration fields and validators construct a one-time account request; uniqueness, invited employee-code policy, normalization, and account persistence remain server-side.</en>
                              </lang>
                            --%>
                            <%-- 姓名输入 --%>
                            Name:
                            <br>
                            <asp:TextBox size="25" ID="Name" runat="server" />
                            &nbsp;
                            <%--
                              <lang>
                                <zh-CN>公开注册只在此处强制必填；更严格的用户名格式策略应通过系统设置统一治理。</zh-CN>
                                <en>Public registration only requires a non-empty value here; stricter user-name format rules should be governed through system settings.</en>
                              </lang>
                            --%>
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
                            <%--
                              <lang>
                                <zh-CN>密码与确认密码的隐藏密文字段仅用于提交前保护，仍属于客户端回传；服务器必须重新验证匹配、策略和安全存储。</zh-CN>
                                <en>The password and confirmation ciphertext fields only support pre-submit protection and remain client-posted data; the server must revalidate matching, policy, and secure storage.</en>
                              </lang>
                            --%>
                            <asp:TextBox size="25" ID="Password" TextMode="Password" runat="server" />
                            <asp:HiddenField ID="EncryptedPassword" runat="server" />
                            &nbsp;
                            <%-- 必填项验证 --%>
                            <asp:RequiredFieldValidator ControlToValidate="Password" ErrorMessage="'Password' must not be left blank."
                                runat="server" ID="RequiredFieldValidator3" />
                            <p>
                            <%-- 确认密码输入 --%>
                            Confirm Password:
                            <br>
                            <asp:TextBox size="25" ID="ConfirmPassword" TextMode="Password" runat="server" />
                            <asp:HiddenField ID="EncryptedConfirmPassword" runat="server" />
                            &nbsp;
                            <%-- 必填项验证 --%>
                            <asp:RequiredFieldValidator ControlToValidate="ConfirmPassword" Display="Dynamic"
                                ErrorMessage="'Confirm' must not be left blank." runat="server" ID="RequiredFieldValidator4" />
                            <%-- 密码匹配验证 --%>
                            <asp:CompareValidator ControlToValidate="ConfirmPassword" ControlToCompare="Password"
                                ErrorMessage="Password fields do not match." runat="server" ID="CompareValidator1" />
                            <p>
                            <%-- 注册按钮 --%>
                            <%--
                              <lang>
                                <zh-CN>RegisterBtn_Click 是账号创建与失败提示的服务器入口；Message 只能反馈服务器结果，不能由页面标记宣称注册成功。</zh-CN>
                                <en>RegisterBtn_Click is the server entry for account creation and failure messaging; Message reports the server result and cannot be used by markup to claim registration success.</en>
                              </lang>
                            --%>
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
