<%@ Page CodeBehind="ModuleSettings.aspx.cs" Language="c#" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.ModuleSettingsPage" MasterPageFile="~/Default.master" %>

<%--
    注释：
    ModuleSettings.aspx 页面用于使管理员能够查看/编辑/更新门户模块的设置（标题、输出缓存属性、编辑访问权限）。
--%>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 页面主体表格 --%>
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="150">
                &nbsp;
            </td>
            <td width="*">
                <table cellpadding="2" cellspacing="1" border="0">
                    <tr>
                        <td colspan="4">
                            <table width="100%" cellspacing="0" cellpadding="0">
                                <tr>
                                    <td align="left" class="Head">
                                        <%-- 标题 --%>
                                        Module Settings
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <%-- 水平线 --%>
                                        <hr noshade size="1">
                                    </td>
                                </tr>
                                <tr>
                                    <td class="NormalRed">
                                        <asp:Label ID="Message" runat="server" />
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <%-- 模块名称输入 --%>
                    <tr>
                        <td width="100" class="SubHead">
                            Module Name:
                        </td>
                        <td colspan="3">
                            &nbsp;<asp:TextBox ID="moduleTitle" Width="300" CssClass="NormalTextBox" runat="server" />
                        </td>
                    </tr>
                    <%-- 缓存超时时间输入 --%>
                    <tr>
                        <td class="SubHead">
                            Cache Timeout (seconds):
                        </td>
                        <td colspan="3">
                            &nbsp;<asp:TextBox ID="cacheTime" Width="100" CssClass="NormalTextBox" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            &nbsp;
                        </td>
                        <td colspan="3">
                            <hr noshade size="1">
                        </td>
                    </tr>
                    <%-- 可以编辑内容的角色选择 --%>
                    <tr>
                        <td class="SubHead">
                            Roles that can edit content:
                        </td>
                        <td colspan="3">
                            <asp:CheckBoxList ID="authEditRoles" RepeatColumns="2"
                                Font-Size="8pt" Width="300" CellPadding="0" CellSpacing="0" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            &nbsp;
                        </td>
                        <td colspan="3">
                            <hr noshade size="1">
                        </td>
                    </tr>
                    <%-- 是否对移动用户显示的选择 --%>
                    <tr>
                        <td class="SubHead" nowrap>
                            Show to mobile users?:
                        </td>
                        <td colspan="3">
                            <asp:CheckBox ID="showMobile" Font-Size="8pt" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td colspan="4">
                            <hr noshade size="1">
                        </td>
                    </tr>
                    <%-- 应用模块更改按钮 --%>
                    <tr>
                        <td colspan="4">
                            <asp:LinkButton class="CommandButton" Text="Apply Module Changes" runat="server"
                                ID="ApplyButton" OnClick="ApplyChanges_Click" />
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</asp:Content>
