<%@ Page Language="c#" CodeBehind="EditContacts.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditContacts"
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="maincontent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="150">
                &nbsp;
            </td>
            <td>
                <table width="500" cellspacing="0" cellpadding="0" border="0">
                    <tr>
                        <td align="left" class="Head">
                            Contact Details
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <hr noshade size="1">
                        </td>
                    </tr>
                </table>
                <table width="750" cellspacing="0" cellpadding="0" border="0">
                    <tr valign="top">
                        <td width="100" class="SubHead">
                            Name:
                        </td>
                        <td rowspan="5">
                            &nbsp;
                        </td>
                        <td align="left">
                            <asp:TextBox ID="NameField" CssClass="NormalTextBox" Width="390" Columns="30" MaxLength="50"
                                runat="server" />
                        </td>
                        <td width="25" rowspan="5">
                            &nbsp;
                        </td>
                        <td class="Normal" width="250">
                            <asp:RequiredFieldValidator Display="Static" runat="server" ErrorMessage="You Must Enter a Valid Name"
                                ControlToValidate="NameField" ID="RequiredFieldValidator1" />
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            Role:
                        </td>
                        <td>
                            <asp:TextBox ID="RoleField" CssClass="NormalTextBox" Width="390" Columns="30" MaxLength="100"
                                runat="server" />
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            Email:
                        </td>
                        <td>
                            <asp:TextBox ID="EmailField" CssClass="NormalTextBox" Width="390" Columns="30" MaxLength="100"
                                runat="server" />
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            Contact1:
                        </td>
                        <td>
                            <asp:TextBox ID="Contact1Field" CssClass="NormalTextBox" Width="390" Columns="30"
                                MaxLength="250" runat="server" />
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            Contact2:
                        </td>
                        <td>
                            <asp:TextBox ID="Contact2Field" CssClass="NormalTextBox" Width="390" Columns="30"
                                MaxLength="250" runat="server" />
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
                    <asp:LinkButton ID="deleteButton" Text="Delete this item" CausesValidation="False"
                        runat="server" class="CommandButton" BorderStyle="none" OnClick="DeleteBtn_Click" />
                    <hr noshade size="1" width="500">
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
