<%@ Page
    Language="c#"
    CodeBehind="SystemHealth.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.SystemHealth"
    MasterPageFile="~/Default.master" %>

<%-- P2.2 只读系统健康页：仅展示检查结果，不提供修复、编辑或命令入口。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="20">
                &nbsp;
            </td>
            <td>
                <table width="100%" cellspacing="0" cellpadding="0">
                    <tr>
                        <td align="left" class="Head">
                            System Health
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <hr noshade size="1">
                        </td>
                    </tr>
                    <tr>
                        <td class="Normal">
                            <a class="CommandButton" href="ThemeSettings.aspx">Theme Settings</a>
                        </td>
                    </tr>
                </table>

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr>
                        <td width="140" class="SubHead">
                            Overall Status:
                        </td>
                        <td class="Normal">
                            <asp:Label ID="OverallStatusLabel" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">
                            Last Checked:
                        </td>
                        <td class="Normal">
                            <asp:Label ID="GeneratedUtcLabel" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            &nbsp;
                        </td>
                        <td>
                            <asp:LinkButton
                                ID="RefreshButton"
                                Text="Recheck"
                                CssClass="CommandButton"
                                CausesValidation="False"
                                OnClick="RefreshButton_Click"
                                runat="server" />
                        </td>
                    </tr>
                </table>

                <br>

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr>
                        <td class="Head">
                            Health Checks
                        </td>
                    </tr>
                </table>

                <asp:Repeater ID="HealthChecksRepeater" runat="server">
                    <HeaderTemplate>
                        <table width="100%" cellspacing="0" cellpadding="3" border="1">
                            <tr class="SubHead">
                                <td width="110">Category</td>
                                <td width="150">Check</td>
                                <td width="90">Status</td>
                                <td width="220">Summary</td>
                                <td>Detail</td>
                                <td width="150">Event ID</td>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("Category") %></td>
                                <td><%#: Eval("Name") %></td>
                                <td><%#: Eval("Status") %></td>
                                <td><%#: Eval("Summary") %></td>
                                <td><%#: Eval("Detail") %></td>
                                <td><%#: Eval("EventId") %></td>
                            </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>

                <br>

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr>
                        <td class="Head">
                            Settings Registry
                        </td>
                    </tr>
                </table>

                <asp:Repeater ID="SettingsRepeater" runat="server">
                    <HeaderTemplate>
                        <table width="100%" cellspacing="0" cellpadding="3" border="1">
                            <tr class="SubHead">
                                <td width="230">Key</td>
                                <td width="150">Name</td>
                                <td width="80">Type</td>
                                <td width="150">Current Value</td>
                                <td width="120">Source</td>
                                <td width="80">Sensitive</td>
                                <td width="90">Editable</td>
                                <td width="90">Restart</td>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("Key") %></td>
                                <td><%#: Eval("DisplayName") %></td>
                                <td><%#: Eval("ValueType") %></td>
                                <td><%#: Eval("CurrentValue") %></td>
                                <td><%#: Eval("Source") %></td>
                                <td><%#: Eval("IsSensitive") %></td>
                                <td><%#: Eval("CanEditOnline") %></td>
                                <td><%#: Eval("RequiresRestart") %></td>
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
