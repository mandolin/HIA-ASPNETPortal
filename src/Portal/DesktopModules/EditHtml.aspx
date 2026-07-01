<%@ Page Language="c#" CodeBehind="EditHtml.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditHtml"
    ValidateRequest="false"
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 页面内容区域开始 --%>
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="100">
                &nbsp;
            </td>
            <td width="*">
                <table width="750" cellspacing="0" cellpadding="0">
                    <tr>
                        <td align="left" class="Head">
                            Html Settings
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <hr noshade size="1">
                        </td>
                    </tr>
                </table>
                <table width="720" cellspacing="0" cellpadding="0">
                    <tr valign="top">
                        <td class="SubHead">
                            Desktop Html Content:
                        </td>
                        <td>
                            &nbsp;&nbsp;
                        </td>
                        <td>
                            <%-- 输入桌面版HTML内容的多行文本框 --%>
                            <asp:TextBox ID="DesktopText" Columns="75" Width="650" Rows="12" TextMode="multiline"
                                runat="server" />
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            Mobile Summary (optional):
                        </td>
                        <td>
                            &nbsp;&nbsp;
                        </td>
                        <td>
                            <%-- 输入移动端摘要（可选）的多行文本框 --%>
                            <asp:TextBox ID="MobileSummary" Columns="75" Width="650" Rows="3" TextMode="multiline"
                                runat="server" />
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            Mobile Details (optional):
                        </td>
                        <td>
                            &nbsp;&nbsp;
                        </td>
                        <td>
                            <%-- 输入移动端详情（可选）的多行文本框 --%>
                            <asp:TextBox ID="MobileDetails" Columns="75" Width="650" Rows="5" TextMode="multiline"
                                runat="server" />
                        </td>
                    </tr>
                </table>
                <p>
                    <%-- 更新按钮 --%>
                    <asp:LinkButton ID="updateButton" Text="Update" runat="server" class="CommandButton"
                        BorderStyle="none" OnClick="UpdateBtn_Click" />
                    &nbsp;
                    <%-- 取消按钮 --%>
                    <asp:LinkButton ID="cancelButton" Text="Cancel" CausesValidation="False" runat="server"
                        class="CommandButton" BorderStyle="none" OnClick="CancelBtn_Click" />
                    &nbsp;
                </p>
            </td>
        </tr>
    </table>
    <%-- 页面内容区域结束 --%>
</asp:Content>
