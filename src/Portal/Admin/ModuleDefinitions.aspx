<%@ Page Language="c#" CodeBehind="ModuleDefinitions.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.ModuleDefinitions" MasterPageFile="~/Default.master" %>

<%--
    The SecurityRoles.aspx page is used to create and edit security roles within
    the Portal application.
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="maincontent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="150">
                &nbsp;
            </td>
            <td width="*">
                <table width="500" cellspacing="0" cellpadding="0">
                    <tr>
                        <td align="left" class="Head">
                            Module Type Definition
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <hr noshade size="1">
                        </td>
                    </tr>
                </table>
                <table width="750" cellspacing="0" cellpadding="0" border="0">
                    <tr>
                        <td width="100" class="SubHead">
                            Friendly Name:
                        </td>
                        <td rowspan="5">
                            &nbsp;
                        </td>
                        <td>
                            <asp:TextBox ID="FriendlyName" CssClass="NormalTextBox" Width="390" Columns="30"
                                MaxLength="150" runat="server" />
                        </td>
                        <td width="25" rowspan="5">
                            &nbsp;
                        </td>
                        <td class="Normal" width="250">
                            <asp:RequiredFieldValidator ID="Req1" Display="Static" ErrorMessage="Enter a Module NAme"
                                ControlToValidate="FriendlyName" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead" nowrap>
                            Desktop Source:
                        </td>
                        <td>
                            <asp:TextBox ID="DesktopSrc" CssClass="NormalTextBox" Width="390" Columns="30" MaxLength="150"
                                runat="server" />
                        </td>
                        <td class="Normal">
                            <asp:RequiredFieldValidator ID="Req2" Display="Static" ErrorMessage="You Must Enter Source Path for the Desktop Module"
                                ControlToValidate="DesktopSrc" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">
                            Mobile Source:
                        </td>
                        <td>
                            <asp:TextBox ID="MobileSrc" CssClass="NormalTextBox" Width="390" Columns="30" MaxLength="150"
                                runat="server" />
                        </td>
                        <td>
                            &nbsp;
                        </td>
                    </tr>
                </table>
                <p>
                    <asp:LinkButton ID="updateButton" Text="Update" runat="server" class="CommandButton"
                        BorderStyle="none" OnClick="UpdateBtn_Click" />
                    &nbsp;
                    <asp:LinkButton ID="cancelButton" Text="Cancel" CausesValidation="False" runat="server"
                        class="CommandButton" BorderStyle="none" OnClick="CancelBtn_Click" />
                    &nbsp;
                    <asp:LinkButton ID="deleteButton" Text="Delete this module type" CausesValidation="False"
                        runat="server" class="CommandButton" BorderStyle="none" OnClick="DeleteBtn_Click" />
                </p>
            </td>
        </tr>
    </table>
</asp:Content>
