<%@ Page
    Language="c#"
    CodeBehind="OperationAudits.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.OperationAudits"
    MasterPageFile="~/Default.master" %>

<%-- P2.4 只读运营审计页：查询高价值状态变更，不记录普通查看行为。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="20">&nbsp;</td>
            <td>
                <table width="100%" cellspacing="0" cellpadding="0">
                    <tr><td align="left" class="Head">Operation Audits</td></tr>
                    <tr><td><hr noshade size="1"></td></tr>
                </table>

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr>
                        <td width="110" class="SubHead">Start UTC:</td>
                        <td width="150"><asp:TextBox ID="StartDateTextBox" CssClass="NormalTextBox" Width="110" runat="server" /></td>
                        <td width="100" class="SubHead">End UTC:</td>
                        <td width="150"><asp:TextBox ID="EndDateTextBox" CssClass="NormalTextBox" Width="110" runat="server" /></td>
                        <td width="70" class="SubHead">Category:</td>
                        <td><asp:TextBox ID="CategoryFilter" CssClass="NormalTextBox" Width="120" runat="server" /></td>
                    </tr>
                    <tr>
                        <td class="SubHead">Action:</td>
                        <td><asp:TextBox ID="ActionFilter" CssClass="NormalTextBox" Width="110" runat="server" /></td>
                        <td class="SubHead">Target ID:</td>
                        <td><asp:TextBox ID="TargetIdFilter" CssClass="NormalTextBox" Width="150" runat="server" /></td>
                        <td colspan="2"><asp:LinkButton ID="SearchButton" Text="Search" CssClass="CommandButton" CausesValidation="False" OnClick="SearchButton_Click" runat="server" /></td>
                    </tr>
                    <tr><td colspan="6" class="NormalRed"><asp:Label ID="MessageLabel" runat="server" /></td></tr>
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
                                <td width="130">Category</td>
                                <td width="110">Action</td>
                                <td width="110">Actor</td>
                                <td width="95">Target</td>
                                <td width="100">Target ID</td>
                                <td>Summary</td>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("OccurredUtc", "{0:yyyy-MM-dd HH:mm:ss} UTC") %></td>
                                <td><%#: Eval("Category") %></td>
                                <td><%#: Eval("Action") %></td>
                                <td><%#: Eval("ActorUserName") %></td>
                                <td><%#: Eval("TargetType") %></td>
                                <td><%#: Eval("TargetId") %></td>
                                <td><%#: Eval("Summary") %></td>
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
