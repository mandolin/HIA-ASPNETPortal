<%@ Page Language="c#" CodeBehind="TabLayout.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.TabLayout"
    MasterPageFile="~/Default.master" %>

<%--
     The TabLayout.aspx page is used to control the layout settings of an
     individual tab within the portal.
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="maincontent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4">
        <tr valign="top">
            <td width="150">
                &nbsp;
            </td>
            <td width="*">
                <table border="0" cellpadding="2" cellspacing="1">
                    <tr>
                        <td colspan="4">
                            <table width="100%" cellspacing="0" cellpadding="0">
                                <tr>
                                    <td align="left" class="Head">
                                        Tab Name and Layout
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <hr noshade size="1">
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td width="100" class="Normal">
                            Tab Name:
                        </td>
                        <td colspan="3">
                            <asp:TextBox ID="tabName" Width="300" CssClass="NormalTextBox" runat="server" OnTextChanged="TabSettings_Change" />
                        </td>
                    </tr>
                    <tr>
                        <td class="Normal" nowrap>
                            Authorized Roles:
                        </td>
                        <td colspan="3">
                            <asp:CheckBoxList ID="authRoles" RepeatColumns="2" Font-Names="Verdana,Arial" Font-Size="8pt"
                                Width="300" runat="server" OnSelectedIndexChanged="TabSettings_Change" />
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
                    <tr>
                        <td class="Normal" nowrap>
                            Show to mobile users?:
                        </td>
                        <td colspan="3">
                            <asp:CheckBox ID="showMobile" Font-Names="Verdana,Arial" Font-Size="8pt" runat="server"
                                OnCheckedChanged="TabSettings_Change" />
                        </td>
                    </tr>
                    <tr>
                        <td class="Normal" nowrap>
                            Mobile Tab Name:
                        </td>
                        <td colspan="3">
                            <asp:TextBox ID="mobileTabName" Width="300" CssClass="NormalTextBox" runat="server"
                                OnTextChanged="TabSettings_Change" />
                        </td>
                    </tr>
                    <tr>
                        <td colspan="4">
                            <hr noshade size="1">
                        </td>
                    </tr>
                    <tr>
                        <td class="Normal">
                            Add Module:
                        </td>
                        <td class="Normal">
                            Module Type
                        </td>
                        <td colspan="2">
                            <asp:DropDownList ID="moduleType" DataValueField="ModuleDefID" DataTextField="FriendlyName"
                                runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            &nbsp;
                        </td>
                        <td class="Normal">
                            Module Name:
                        </td>
                        <td colspan="2">
                            <asp:TextBox ID="moduleTitle" EnableViewState="false" Text="New Module Name" CssClass="NormalTextBox"
                                Width="250" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            &nbsp;
                        </td>
                        <td colspan="3">
                            <asp:LinkButton class="CommandButton" Text='<img src="../images/dn.gif" border=0> Add to "Organize Modules" Below'
                                runat="server" ID="AddModuleBtn" OnClick="AddModuleToPane_Click" />
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
                    <tr valign="top">
                        <td class="Normal">
                            Organize Modules:
                        </td>
                        <td width="120">
                            <table border="0" cellspacing="0" cellpadding="2" width="100%">
                                <tr>
                                    <td class="NormalBold">
                                        &nbsp;Left Mini Pane
                                    </td>
                                </tr>
                                <tr valign="top">
                                    <td>
                                        <table border="0" cellspacing="2" cellpadding="0">
                                            <tr valign="top">
                                                <td rowspan="2">
                                                    <asp:ListBox ID="leftPane" DataSource="<%# leftList %>" DataTextField="ModuleTitle"
                                                        DataValueField="ModuleId" Width="110" Rows="7" runat="server" />
                                                </td>
                                                <td valign="top" nowrap>
                                                    <asp:ImageButton ImageUrl="~/images/up.gif" CommandName="up" CommandArgument="leftPane"
                                                        AlternateText="Move selected module up in list" runat="server" ID="LeftUpBtn"
                                                        OnClick="UpDown_Click" />
                                                    <br>
                                                    <asp:ImageButton ImageUrl="~/images/rt.gif" CommandName="right" sourcepane="leftPane"
                                                        targetpane="contentPane" AlternateText="Move selected module to the content pane"
                                                        runat="server" ID="LeftRightBtn" OnClick="RightLeft_Click" />
                                                    <br>
                                                    <asp:ImageButton ImageUrl="~/images/dn.gif" CommandName="down" CommandArgument="leftPane"
                                                        AlternateText="Move selected module down in list" runat="server" ID="LeftDownBtn"
                                                        OnClick="UpDown_Click" />
                                                    &nbsp;&nbsp;
                                                </td>
                                            </tr>
                                            <tr>
                                                <td valign="bottom" nowrap>
                                                    <asp:ImageButton ImageUrl="~/images/edit.gif" CommandName="edit" CommandArgument="leftPane"
                                                        AlternateText="Edit this item" runat="server" ID="LeftEditBtn" OnClick="EditBtn_Click" />
                                                    <br>
                                                    <asp:ImageButton ImageUrl="~/images/delete.gif" CommandName="delete" CommandArgument="leftPane"
                                                        AlternateText="Delete this item" runat="server" ID="LeftDeleteBtn" OnClick="DeleteBtn_Click" />
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                        <td width="*">
                            <table border="0" cellspacing="0" cellpadding="2" width="100%">
                                <tr>
                                    <td class="NormalBold">
                                        &nbsp;Content Pane
                                    </td>
                                </tr>
                                <tr>
                                    <td align="center">
                                        <table border="0" cellspacing="2" cellpadding="0">
                                            <tr valign="top">
                                                <td rowspan="2">
                                                    <asp:ListBox ID="contentPane" DataSource="<%# contentList %>" DataTextField="ModuleTitle"
                                                        DataValueField="ModuleId" Width="170" Rows="7" runat="server" />
                                                </td>
                                                <td valign="top" nowrap>
                                                    <asp:ImageButton ImageUrl="~/images/up.gif" CommandName="up" CommandArgument="contentPane"
                                                        AlternateText="Move selected module up in list" runat="server" ID="ContentUpBtn"
                                                        OnClick="UpDown_Click" />
                                                    <br>
                                                    <asp:ImageButton ImageUrl="~/images/lt.gif" sourcepane="contentPane" targetpane="leftPane"
                                                        AlternateText="Move selected module to the left pane" runat="server" ID="ContentLeftBtn"
                                                        OnClick="RightLeft_Click" />
                                                    <br>
                                                    <asp:ImageButton ImageUrl="~/images/rt.gif" sourcepane="contentPane" targetpane="rightPane"
                                                        AlternateText="Move selected module to the right pane" runat="server" ID="ContentRightBtn"
                                                        OnClick="RightLeft_Click" />
                                                    <br>
                                                    <asp:ImageButton ImageUrl="~/images/dn.gif" CommandName="down" CommandArgument="contentPane"
                                                        AlternateText="Move selected module down in list" runat="server" ID="ContentDownBtn"
                                                        OnClick="UpDown_Click" />
                                                    &nbsp;&nbsp;
                                                </td>
                                            </tr>
                                            <tr>
                                                <td valign="bottom" nowrap>
                                                    <asp:ImageButton ImageUrl="~/images/edit.gif" CommandName="edit" CommandArgument="contentPane"
                                                        AlternateText="Edit this item" runat="server" ID="ContentEditBtn" OnClick="EditBtn_Click" />
                                                    <br>
                                                    <asp:ImageButton ImageUrl="~/images/delete.gif" CommandName="delete" CommandArgument="contentPane"
                                                        AlternateText="Delete this item" runat="server" ID="ContentDeleteBtn" OnClick="DeleteBtn_Click" />
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                        <td width="120">
                            <table border="0" cellspacing="0" cellpadding="2" width="100%">
                                <tr>
                                    <td class="NormalBold">
                                        &nbsp;Right Mini Pane
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <table border="0" cellspacing="2" cellpadding="0">
                                            <tr valign="top">
                                                <td rowspan="2">
                                                    <asp:ListBox ID="rightPane" DataSource="<%# rightList %>" DataTextField="ModuleTitle"
                                                        DataValueField="ModuleId" Width="110" Rows="7" runat="server" />
                                                </td>
                                                <td valign="top" nowrap>
                                                    <asp:ImageButton ImageUrl="~/images/up.gif" CommandName="up" CommandArgument="rightPane"
                                                        AlternateText="Move selected module up in list" runat="server" ID="RightUpBtn"
                                                        OnClick="UpDown_Click" />
                                                    <br>
                                                    <asp:ImageButton ImageUrl="~/images/lt.gif" sourcepane="rightPane" targetpane="contentPane"
                                                        AlternateText="Move selected module to the left pane" runat="server" ID="RightLeftBtn"
                                                        OnClick="RightLeft_Click" />
                                                    <br>
                                                    <asp:ImageButton ImageUrl="~/images/dn.gif" CommandName="down" CommandArgument="rightPane"
                                                        AlternateText="Move selected module down in list" runat="server" ID="RightDownBtn"
                                                        OnClick="UpDown_Click" />
                                                </td>
                                            </tr>
                                            <tr>
                                                <td valign="bottom" nowrap>
                                                    <asp:ImageButton ImageUrl="~/images/edit.gif" CommandName="edit" CommandArgument="rightPane"
                                                        AlternateText="Edit this item" runat="server" ID="RightEditBtn" OnClick="EditBtn_Click" />
                                                    <br>
                                                    <asp:ImageButton ImageUrl="~/images/delete.gif" CommandName="delete" CommandArgument="rightPane"
                                                        AlternateText="Delete this item" runat="server" ID="RightDeleteBtn" OnClick="DeleteBtn_Click" />
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td colspan="4">
                            <hr noshade size="1">
                        </td>
                    </tr>
                    <tr>
                        <td colspan="4">
                            <asp:LinkButton ID="applyBtn" class="CommandButton" Text="Apply Changes" runat="server"
                                OnClick="Apply_Click" />
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</asp:Content>
