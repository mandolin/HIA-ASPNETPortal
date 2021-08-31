<%@ Page Language="c#" CodeBehind="DiscussDetails.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.DiscussDetails" MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<asp:Content ID="Content1" ContentPlaceHolderID="maincontent" runat="server">
    <table cellspacing="0" cellpadding="0" width="600">
        <tr>
            <td align="left">
                <span class="Head">Message Detail</span>
            </td>
            <td align="right">
                <asp:Panel ID="ButtonPanel" runat="server">
                    <a class="CommandButton" id="prevItem" title="Previous Message" runat="server">
                        <img src='<%=Global.GetApplicationPath(Request) + "/images/rew.gif"%>' border="0"></a>&nbsp;
                    <a class="CommandButton" id="nextItem" title="Next Message" runat="server">
                        <img src='<%=Global.GetApplicationPath(Request) + "/images/fwd.gif"%>' border="0"></a>&nbsp;
                    <asp:LinkButton ID="ReplyBtn" runat="server" EnableViewState="false" CssClass="CommandButton"
                        Text="Reply to this Message" OnClick="ReplyBtn_Click"></asp:LinkButton>
                </asp:Panel>
            </td>
        </tr>
        <tr>
            <td colspan="2">
                <hr noshade size="1">
            </td>
        </tr>
    </table>
    <asp:Panel ID="EditPanel" runat="server" Visible="false">
        <table cellspacing="0" cellpadding="4" width="600" border="0">
            <tr valign="top">
                <td class="SubHead" width="150">
                    Title:
                </td>
                <td rowspan="4">
                    &nbsp;
                </td>
                <td width="*">
                    <asp:TextBox ID="TitleField" runat="server" MaxLength="100" Columns="40" Width="500"
                        CssClass="NormalTextBox"></asp:TextBox>
                </td>
            </tr>
            <tr valign="top">
                <td class="SubHead">
                    Body:
                </td>
                <td width="*">
                    <asp:TextBox ID="BodyField" runat="server" Columns="59" Width="500" Rows="15" TextMode="Multiline"></asp:TextBox>
                </td>
            </tr>
            <tr valign="top">
                <td>
                    &nbsp;
                </td>
                <td>
                    <asp:LinkButton class="CommandButton" ID="updateButton" runat="server" Text="Submit"
                        OnClick="UpdateBtn_Click"></asp:LinkButton>
                    &nbsp;
                    <asp:LinkButton class="CommandButton" ID="cancelButton" runat="server" Text="Cancel"
                        CausesValidation="False" OnClick="CancelBtn_Click"></asp:LinkButton>
                    &nbsp;
                </td>
            </tr>
            <tr valign="top">
                <td class="SubHead">
                    Original Message:
                </td>
                <td>
                    &nbsp;
                </td>
            </tr>
        </table>
    </asp:Panel>
    <table cellspacing="0" cellpadding="4" width="600" border="0">
        <tr valign="top">
            <td class="Message" align="left">
                <b>Subject: </b>
                <asp:Label ID="Subject" runat="server"></asp:Label>
                <br>
                <b>Author: </b>
                <asp:Label ID="CreatedByUser" runat="server"></asp:Label>
                <br>
                <b>Date: </b>
                <asp:Label ID="CreatedDate" runat="server"></asp:Label>
                <br>
                <br>
                <asp:Label ID="Body" runat="server"></asp:Label>
            </td>
        </tr>
    </table>
</asp:Content>
