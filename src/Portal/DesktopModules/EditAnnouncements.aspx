<%@ Page Language="c#" CodeBehind="EditAnnouncements.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.EditAnnouncements" MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="maincontent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="150">
                &nbsp;
            </td>
            <td width="*">
                <table width="520" cellspacing="0" cellpadding="0">
                    <tr>
                        <td align="left" class="Head">
                            Announcement Details
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <hr noshade size="1">
                        </td>
                    </tr>
                </table>
                <table width="750" cellspacing="0" cellpadding="0">
                    <tr valign="top">
                        <td width="100" class="SubHead">
                            Title:
                        </td>
                        <td rowspan="5">
                            &nbsp;
                        </td>
                        <td>
                            <asp:TextBox ID="TitleField" CssClass="NormalTextBox" Width="390" Columns="30" MaxLength="100"
                                runat="server" />
                        </td>
                        <td width="25" rowspan="5">
                            &nbsp;
                        </td>
                        <td class="Normal" width="250">
                            <asp:RequiredFieldValidator ID="Req1" Display="Static" ErrorMessage="You Must Enter a Valid Title"
                                ControlToValidate="TitleField" runat="server" />
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            Read More Link:
                        </td>
                        <td>
                            <asp:TextBox ID="MoreLinkField" CssClass="NormalTextBox" Width="390" Columns="30"
                                MaxLength="100" runat="server" />
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead" nowrap>
                            Read More (Mobile):
                        </td>
                        <td>
                            <asp:TextBox ID="MobileMoreField" CssClass="NormalTextBox" Width="390" Columns="30"
                                MaxLength="100" runat="server" />
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            Description:
                        </td>
                        <td>
                            <asp:TextBox ID="DescriptionField" Width="390" TextMode="Multiline" Columns="44"
                                Rows="6" runat="server" />
                        </td>
                        <td class="Normal">
                            <asp:RequiredFieldValidator ID="Req2" Display="Static" ErrorMessage="You Must Enter a Valid Description"
                                ControlToValidate="DescriptionField" runat="server" />
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            Expires:
                        </td>
                        <td>
                            <asp:TextBox ID="ExpireField" Text="12/31/2001" CssClass="NormalTextBox" Width="100"
                                Columns="8" runat="server" />
                        </td>
                        <td class="Normal">
                            <asp:RequiredFieldValidator Display="Static" ID="RequiredExpireDate" runat="server"
                                ErrorMessage="You Must Enter a Valid Expiration Date" ControlToValidate="ExpireField" />
                            <asp:CompareValidator Display="Static" ID="VerifyExpireDate" runat="server" Operator="DataTypeCheck"
                                ControlToValidate="ExpireField" Type="Date" ErrorMessage="You Must Enter a Valid Expiration Date" />
                        </td>
                    </tr>
                </table>
                <p>
                    <asp:LinkButton ID="updateButton" Text="Update" runat="server" CssClass="CommandButton"
                        BorderStyle="none" OnClick="UpdateBtn_Click" />
                    &nbsp;
                    <asp:LinkButton ID="cancelButton" Text="Cancel" CausesValidation="False" runat="server"
                        CssClass="CommandButton" BorderStyle="none" OnClick="CancelBtn_Click" />
                    &nbsp;
                    <asp:LinkButton ID="deleteButton" Text="Delete this item" CausesValidation="False"
                        runat="server" CssClass="CommandButton" BorderStyle="none" OnClick="DeleteBtn_Click" />
                    <hr noshade size="1" width="520">
                    <span class="Normal">Created by
                        <asp:Label ID="CreatedBy" runat="server" />
                        on
                        <asp:Label ID="CreatedDate" runat="server" />
                        <br>
                    </span>
                    <p>
                    </p>
            </td>
        </tr>
    </table>
</asp:Content>
