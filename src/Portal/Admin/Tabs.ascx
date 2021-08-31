<%@ Control Inherits="ASPNET.StarterKit.Portal.Tabs" CodeBehind="Tabs.ascx.cs" Language="c#" AutoEventWireup="True" %>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<ASPNETPortal:title runat="server" id=Title1 />

<table cellpadding=2 cellspacing=0 border=0>
    <tr>
        <td colspan=2>
            <asp:LinkButton id="addBtn" cssclass="CommandButton" Text="Add New Tab" runat="server" onclick="AddTab_Click" />
        </td>
    </tr>
    <tr valign="top">
        <td width=100>
            &nbsp;
        </td>
        <td width=50 class="Normal">
            Tabs:
        </td>
        <td>
            <table cellpadding=0 cellspacing=0 border=0>
                <tr valign="top">
                    <td>
                        <asp:ListBox id="tabList" width="200" DataSource="<%# PortalTabs %>" DataTextField="TabName" DataValueField="TabId" rows=5 runat="server" />
                    </td>
                    <td>
                        &nbsp;
                    </td>
                    <td>
                        <table>
                            <tr>
                                <td>
                                    <asp:ImageButton id="upBtn" ImageUrl="~/images/up.gif" CommandName="up" AlternateText="Move selected tab up in list" runat="server" onclick="UpDown_Click" />
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:ImageButton id="downBtn" ImageUrl="~/images/dn.gif" CommandName="down" AlternateText="Move selected tab down in list" runat="server" onclick="UpDown_Click" />
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:ImageButton id="editBtn" ImageUrl="~/images/edit.gif" AlternateText="Edit selected tab's properties" runat="server" onclick="EditBtn_Click" />
                                </td>
                            </tr>
                            <tr>
                                <td>
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