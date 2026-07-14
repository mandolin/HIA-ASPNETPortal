<%@ Control Inherits="ASPNET.StarterKit.Portal.Tabs" CodeBehind="Tabs.ascx.cs" Language="c#" AutoEventWireup="True" %>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx" %>

<ASPNETPortal:Title runat="server" ID="Title1" />

<%-- 创建一个表格，用于布局 --%>
<table cellpadding=2 cellspacing=0 border=0>
    <tr>
        <td></td>
        <td class="NormalRed">
            <asp:Label ID="Message" runat="server" />
        </td>
    </tr>
    <%-- 第一行用于添加新页的按钮 --%>
    <tr>
        <td colspan=2>
            <%-- 添加新页的按钮 --%>
            <asp:LinkButton id="addBtn" cssclass="CommandButton" Text="Add New Tab" runat="server" onclick="AddTab_Click" />
        </td>
    </tr>
    <%-- 第二行用于显示页列表和其他操作按钮 --%>
    <tr valign="top">
        <td width=100>
            <%-- 空白列，用于间隔 --%>
            &nbsp;
        </td>
        <td width=50 class="Normal">
            <%-- 页标题 --%>
            Tabs:
        </td>
        <td>
            <%-- 内嵌表格，用于显示页列表 --%>
            <table cellpadding=0 cellspacing=0 border=0>
                <tr valign="top">
                    <td>
                        <%-- 显示页列表的ListBox控件 --%>
                        <asp:ListBox id="tabList" width="200" DataSource="<%# PortalTabs %>" DataTextField="TabName" DataValueField="TabId" rows=5 runat="server" />
                    </td>
                    <td>
                        <%-- 空白列，用于间隔 --%>
                        &nbsp;
                    </td>
                    <td>
                        <%-- 表格，用于放置操作按钮 --%>
                        <table>
                            <tr>
                                <td>
                                    <%-- 上移页的ImageButton --%>
                                    <asp:ImageButton id="upBtn" ImageUrl="~/images/up.gif" CommandName="up" AlternateText="Move selected tab up in list" runat="server" onclick="UpDown_Click" />
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <%-- 下移页的ImageButton --%>
                                    <asp:ImageButton id="downBtn" ImageUrl="~/images/dn.gif" CommandName="down" AlternateText="Move selected tab down in list" runat="server" onclick="UpDown_Click" />
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <%-- 编辑页属性的ImageButton --%>
                                    <asp:ImageButton id="editBtn" ImageUrl="~/images/edit.gif" AlternateText="Edit selected tab's properties" runat="server" onclick="EditBtn_Click" />
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <%-- 删除页的ImageButton --%>
                                    <asp:ImageButton id="deleteBtn" ImageUrl="~/images/delete.gif" AlternateText="Delete selected tab" runat="server" onclick="DeleteBtn_Click" />
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </td>
    </tr>
</table>
