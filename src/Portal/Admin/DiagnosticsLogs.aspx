<%@ Page
    Language="c#"
    CodeBehind="DiagnosticsLogs.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.DiagnosticsLogs"
    MasterPageFile="~/Default.master" %>

<%-- P2.4 只读诊断日志页：仅查询受限 NDJSON 记录，不提供下载、删除或路径输入。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="20">&nbsp;</td>
            <td>
                <table width="100%" cellspacing="0" cellpadding="0">
                    <tr><td align="left" class="Head">Diagnostics Logs</td></tr>
                    <tr><td><hr noshade size="1"></td></tr>
                </table>

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr>
                        <td width="110" class="SubHead">Start UTC:</td>
                        <td width="150"><asp:TextBox ID="StartDateTextBox" CssClass="NormalTextBox" Width="110" runat="server" /></td>
                        <td width="100" class="SubHead">End UTC:</td>
                        <td width="150"><asp:TextBox ID="EndDateTextBox" CssClass="NormalTextBox" Width="110" runat="server" /></td>
                        <td width="70" class="SubHead">Level:</td>
                        <td>
                            <asp:DropDownList ID="LevelFilter" CssClass="NormalTextBox" runat="server">
                                <asp:ListItem Text="All" Value="" />
                                <asp:ListItem Text="Info" Value="Info" />
                                <asp:ListItem Text="Warning" Value="Warning" />
                                <asp:ListItem Text="Error" Value="Error" />
                            </asp:DropDownList>
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Category:</td>
                        <td><asp:TextBox ID="CategoryFilter" CssClass="NormalTextBox" Width="110" runat="server" /></td>
                        <td class="SubHead">Event ID:</td>
                        <td><asp:TextBox ID="EventIdFilter" CssClass="NormalTextBox" Width="150" runat="server" /></td>
                        <td colspan="2">
                            <asp:LinkButton ID="SearchButton" Text="Search" CssClass="CommandButton" CausesValidation="False" OnClick="SearchButton_Click" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td colspan="6" class="NormalRed"><asp:Label ID="MessageLabel" runat="server" /></td>
                    </tr>
                </table>

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr>
                        <td class="Normal"><asp:Label ID="ResultLabel" runat="server" /></td>
                        <td align="right">
                            <asp:LinkButton ID="PreviousButton" Text="Previous" CssClass="CommandButton" CausesValidation="False" OnClick="PreviousButton_Click" runat="server" />
                            &nbsp;
                            <asp:LinkButton ID="NextButton" Text="Next" CssClass="CommandButton" CausesValidation="False" OnClick="NextButton_Click" runat="server" />
                        </td>
                    </tr>
                </table>

                <asp:Repeater ID="EntriesRepeater" runat="server">
                    <HeaderTemplate>
                        <table width="100%" cellspacing="0" cellpadding="3" border="1">
                            <tr class="SubHead">
                                <td width="155">UTC</td>
                                <td width="75">Level</td>
                                <td width="150">Category</td>
                                <td>Message</td>
                                <td width="195">Event ID</td>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("UtcTime", "{0:yyyy-MM-dd HH:mm:ss} UTC") %></td>
                                <td><%#: Eval("Level") %></td>
                                <td><%#: Eval("Category") %></td>
                                <td><%#: Eval("Message") %></td>
                                <td><asp:HyperLink ID="DetailLink" NavigateUrl='<%# GetDetailUrl(Eval("EventId")) %>' Text='<%#: Eval("EventId") %>' runat="server" /></td>
                            </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </td>
        </tr>
    </table>
</asp:Content>
