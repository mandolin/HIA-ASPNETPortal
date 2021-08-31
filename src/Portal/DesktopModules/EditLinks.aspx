<%@ Page Language="c#" CodeBehind="EditLinks.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditLinks"
    MasterPageFile="~/Default.master" %>

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
                            Link Details
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
                            Title:
                        </td>
                        <td rowspan="5">
                            &nbsp;
                        </td>
                        <td>
                            <asp:TextBox ID="TitleField" CssClass="NormalTextBox" Width="390" Columns="30" MaxLength="150"
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
                    <tr>
                        <td class="SubHead">
                            Url:
                        </td>
                        <td>
                            <asp:TextBox ID="UrlField" CssClass="NormalTextBox" Width="390" Columns="30" MaxLength="150"
                                runat="server" />
                        </td>
                        <td class="Normal">
                            <asp:RequiredFieldValidator ID="Req2" Display="Static" runat="server" ErrorMessage="You Must Enter a Valid URL"
                                ControlToValidate="UrlField" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">
                            Mobile Url:
                        </td>
                        <td>
                            <asp:TextBox ID="MobileUrlField" CssClass="NormalTextBox" Width="390" Columns="30"
                                MaxLength="150" runat="server" />
                        </td>
                        <td>
                            &nbsp;
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">
                            Description:
                        </td>
                        <td>
                            <asp:TextBox ID="DescriptionField" CssClass="NormalTextBox" Width="390" Columns="30"
                                MaxLength="150" runat="server" />
                        </td>
                        <td>
                            &nbsp;
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">
                            View Order:
                        </td>
                        <td>
                            <asp:TextBox ID="ViewOrderField" CssClass="NormalTextBox" Width="390" Columns="30"
                                MaxLength="3" runat="server" />
                        </td>
                        <td class="Normal">
                            <asp:RequiredFieldValidator Display="Static" ID="RequiredViewOrder" runat="server"
                                ControlToValidate="ViewOrderField" ErrorMessage="You Must Enter a Valid View Order" />
                            <asp:CompareValidator Display="Static" ID="VerifyViewOrder" runat="server" Operator="DataTypeCheck"
                                ControlToValidate="ViewOrderField" Type="Integer" ErrorMessage="You Must Enter a Valid View Order" />
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
