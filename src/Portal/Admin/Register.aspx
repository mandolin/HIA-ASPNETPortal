<%@ Page Language="c#" CodeBehind="Register.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.Register"
    MasterPageFile="~/Default.master" %>

<%--
   <lang>
     <zh-CN>Register.aspx 为公开或邀请式注册提供旧 Web Forms 表单；页面只收集账号、邮箱、员工号与密码输入，唯一性校验、邀请策略、密码处理和数据库写入均由 RegisterBtn_Click 及 UsersDB 服务器链路完成。</zh-CN>
     <en>Register.aspx provides the legacy Web Forms surface for public or invitation registration; the page only collects account, email, employee code, and password input, while uniqueness checks, invitation policy, password handling, and database persistence are handled by RegisterBtn_Click and the UsersDB server path.</en>
   </lang>
--%>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
      <lang>
        <zh-CN>外层表格保留 StarterKit 时代布局契约，避免注册页在旧主题和窄屏回退中脱离主内容列。</zh-CN>
        <en>The outer table preserves the StarterKit-era layout contract so the registration page stays aligned with the main content column in legacy themes and narrow fallbacks.</en>
      </lang>
    --%>
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
                                        <%--
                                          <lang>
                                            <zh-CN>标题文本是此页面的可见入口提示；账号创建结果仍以后续服务器消息为准。</zh-CN>
                                            <en>The heading is the visible entry cue for this page; the outcome of account creation is still determined by the later server message.</en>
                                          </lang>
                                        --%>
                                        <span class="Head">Create a New Account </span>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <%--
                                          <lang>
                                            <zh-CN>旧式水平线只承担视觉分隔作用，不表达验证、授权或流程状态。</zh-CN>
                                            <en>The legacy horizontal rule is only a visual separator and does not express validation, authorization, or workflow state.</en>
                                          </lang>
                                        --%>
                                        <hr noshade size="1">
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <%--
                      <lang>
                        <zh-CN>用户信息区域按回发字段组织输入；每个服务器控件的 ID 是后端读取和验证注册请求的稳定契约。</zh-CN>
                        <en>The user information area groups postback fields; each server control ID is the stable contract used by the backend to read and validate the registration request.</en>
                      </lang>
                    --%>
                    <tr valign="top">
                        <td class="Normal">
                            <%--
                              <lang>
                                <zh-CN>注册字段和验证器只构造一次性账号申请；唯一性、邀请员工号策略、规范化和账户写入仍由服务器完成。</zh-CN>
                                <en>Registration fields and validators construct a one-time account request; uniqueness, invited employee-code policy, normalization, and account persistence remain server-side.</en>
                              </lang>
                            --%>
                            <%--
                              <lang>
                                <zh-CN>Name 字段承载注册用户名；标记层只要求非空，规范化、重复检查和持久化在服务器端完成。</zh-CN>
                                <en>The Name field carries the requested user name; markup only requires a non-empty value, with normalization, duplicate checks, and persistence handled server-side.</en>
                              </lang>
                            --%>
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
                            <%--
                              <lang>
                                <zh-CN>Email 字段用于注册联系地址；页面校验只覆盖基本格式和必填，邮箱可信度与后续使用策略不能由客户端验证替代。</zh-CN>
                                <en>The Email field stores the registration contact address; page validators only cover basic format and presence, and cannot replace server-side trust or follow-up usage policy.</en>
                              </lang>
                            --%>
                            Email:
                            <br>
                            <asp:TextBox size="25" ID="Email" runat="server" />
                            &nbsp;
                            <asp:RegularExpressionValidator ControlToValidate="Email" ValidationExpression="[\w\.-]+(\+[\w-]*)?@([\w-]+\.)+[\w-]+"
                                Display="Dynamic" ErrorMessage="Must use a valid email address." runat="server"
                                ID="RegularExpressionValidator1" />
                            <asp:RequiredFieldValidator ControlToValidate="Email" ErrorMessage="'Email' must not be left blank."
                                runat="server" ID="RequiredFieldValidator2" />
                            <p>
                            <%--
                              <lang>
                                <zh-CN>EmployeeCode 在企业临时邀请注册中作为必填绑定线索；普通自注册路径默认不强制，启用状态由服务器策略切换。</zh-CN>
                                <en>EmployeeCode is the required binding hint for enterprise invitation registration; ordinary self-registration does not require it by default, and the enabled state is switched by server policy.</en>
                              </lang>
                            --%>
                            Employee Code:
                            <asp:Label ID="EmployeeCodeRequiredHint" CssClass="NormalRed" Text="*" Visible="false" runat="server" />
                            <br>
                            <asp:TextBox size="25" ID="EmployeeCode" runat="server" />
                            &nbsp;
                            <asp:RequiredFieldValidator ControlToValidate="EmployeeCode" Display="Dynamic"
                                ErrorMessage="'Employee Code' must not be left blank for invitation registration."
                                Enabled="false" runat="server" ID="EmployeeCodeRequiredValidator" />
                            <p>
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
                            <asp:RequiredFieldValidator ControlToValidate="Password" ErrorMessage="'Password' must not be left blank."
                                runat="server" ID="RequiredFieldValidator3" />
                            <p>
                            <%--
                              <lang>
                                <zh-CN>确认密码字段只为本次回发提供一致性检查；匹配验证发生在页面层，服务器仍需重新确认明文/密文输入对应同一用户意图。</zh-CN>
                                <en>The confirmation password field only supports consistency checks for this postback; matching validation occurs at the page layer, and the server must still re-confirm that plaintext or ciphertext input reflects the same user intent.</en>
                              </lang>
                            --%>
                            Confirm Password:
                            <br>
                            <asp:TextBox size="25" ID="ConfirmPassword" TextMode="Password" runat="server" />
                            <asp:HiddenField ID="EncryptedConfirmPassword" runat="server" />
                            &nbsp;
                            <asp:RequiredFieldValidator ControlToValidate="ConfirmPassword" Display="Dynamic"
                                ErrorMessage="'Confirm' must not be left blank." runat="server" ID="RequiredFieldValidator4" />
                            <asp:CompareValidator ControlToValidate="ConfirmPassword" ControlToCompare="Password"
                                ErrorMessage="Password fields do not match." runat="server" ID="CompareValidator1" />
                            <p>
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
                                <%--
                                  <lang>
                                    <zh-CN>Message 标签只显示服务器返回的注册结果或错误提示，不保存敏感输入，也不承担客户端状态同步职责。</zh-CN>
                                    <en>The Message label only displays the registration result or error returned by the server; it does not store sensitive input or synchronize client-side state.</en>
                                  </lang>
                                --%>
                                <asp:Label ID="Message" CssClass="NormalRed" runat="server" />
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</asp:Content>
