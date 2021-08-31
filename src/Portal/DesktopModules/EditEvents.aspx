<%@ Page Language="c#" CodeBehind="EditEvents.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditEvents"
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="maincontent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="100">
                &nbsp;
            </td>
            <td width="*">
                <table width="500" cellspacing="0" cellpadding="0">
                    <tr>
                        <td align="left" class="Head">
                            Event Details
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <hr noshade size="1pt">
                        </td>
                    </tr>
                </table>
                <table width="750" cellspacing="0" cellpadding="0">
                    <tr valign="top">
                        <td width="100" class="SubHead">
                            Title:
                        </td>
                        <td rowspan="4">
                            &nbsp;
                        </td>
                        <td>
                            <asp:TextBox ID="TitleField" CssClass="NormalTextBox" Width="390" Columns="30" MaxLength="150"
                                runat="server" />
                        </td>
                        <td width="25" rowspan="4">
                            &nbsp;
                        </td>
                        <td class="Normal" width="250">
                            <asp:RequiredFieldValidator Display="Static" runat="server" ErrorMessage="You Must Enter a Valid Title"
                                ControlToValidate="TitleField" />
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            Description:
                        </td>
                        <td>
                            <asp:TextBox ID="DescriptionField" TextMode="Multiline" Width="390" Columns="44"
                                Rows="6" runat="server" />
                        </td>
                        <td class="Normal">
                            <asp:RequiredFieldValidator Display="Static" runat="server" ErrorMessage="You Must Enter a Valid Description"
                                ControlToValidate="DescriptionField" />
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            Where/When:
                        </td>
                        <td>
                            <asp:TextBox ID="WhereWhenField" CssClass="NormalTextBox" Width="390" Columns="30"
                                MaxLength="150" runat="server" />
                        </td>
                        <td class="Normal">
                            <asp:RequiredFieldValidator Display="Static" runat="server" ErrorMessage="You Must Enter a Valid Time/Location"
                                ControlToValidate="WhereWhenField" />
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
                    <asp:LinkButton ID="updateButton" Text="Update" runat="server" class="CommandButton"
                        BorderStyle="none" OnClick="UpdateBtn_Click" />
                    &nbsp;
                    <asp:LinkButton ID="cancelButton" Text="Cancel" CausesValidation="False" runat="server"
                        class="CommandButton" BorderStyle="none" OnClick="CancelBtn_Click" />
                    &nbsp;
                    <asp:LinkButton ID="deleteButton" Text="Delete this item" CausesValidation="False"
                        runat="server" class="CommandButton" BorderStyle="none" OnClick="DeleteBtn_Click" />
                    <hr noshade size="1pt" width="500">
                    <span class="Normal">Created by
                        <asp:Label ID="CreatedBy" runat="server" />
                        on
                        <asp:Label ID="CreatedDate" runat="server" />
                        <br>
                    </span>
                </p>
            </td>
        </tr>
    </table>
</asp:Content>
