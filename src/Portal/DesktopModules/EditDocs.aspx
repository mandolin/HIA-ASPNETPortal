<%@ Page Language="c#" CodeBehind="EditDocs.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditDocs"
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="maincontent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="150">
                &nbsp;
            </td>
            <td>
                <table width="500" cellspacing="0" cellpadding="0">
                    <tr>
                        <td align="left" class="Head">
                            Document Details
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <hr noshade size="1">
                        </td>
                    </tr>
                </table>
                <table width="726" cellspacing="0" cellpadding="0" border="0">
                    <tr valign="top">
                        <td width="100" class="SubHead">
                            Name:
                        </td>
                        <td>
                            &nbsp;
                        </td>
                        <td>
                            <asp:TextBox ID="NameField" CssClass="NormalTextBox" Width="353" Columns="28" MaxLength="150"
                                runat="server" />
                        </td>
                        <td width="25" rowspan="6">
                            &nbsp;
                        </td>
                        <td class="Normal" width="250">
                            <asp:RequiredFieldValidator Display="Static" runat="server" ErrorMessage="You Must Enter a Valid Name"
                                ControlToValidate="NameField" ID="RequiredFieldValidator1" />
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            Category:
                        </td>
                        <td>
                            &nbsp;
                        </td>
                        <td>
                            <asp:TextBox ID="CategoryField" CssClass="NormalTextBox" Width="353" Columns="28"
                                MaxLength="50" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            &nbsp;
                        </td>
                        <td colspan="2">
                            <hr noshade size="1" width="100%">
                        </td>
                    </tr>
                    <tr valign="top">
                        <td width="100" class="SubHead">
                            URL to Browse:
                        </td>
                        <td>
                            &nbsp;
                        </td>
                        <td>
                            <asp:TextBox ID="PathField" CssClass="NormalTextBox" Width="353" Columns="28" MaxLength="250"
                                runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">
                            — or —
                        </td>
                        <td colspan="2">
                            &nbsp;
                            <br>
                            <br>
                        </td>
                    </tr>
                    <tr valign="top">
                        <td nowrap class="SubHead">
                            Upload to Web Server:&nbsp;
                        </td>
                        <td>
                            &nbsp;
                        </td>
                        <td>
                            <asp:CheckBox ID="Upload" CssClass="Normal" Text="Upload document to server" runat="server" />
                            <br />
                            <asp:CheckBox ID="storeInDatabase" CssClass="Normal" Text="Store in database (web farm support)"
                                runat="server" />
                            <br />
                            <input type="file" id="FileUpload" width="300" style="font-family: verdana; width: 353px;"
                                runat="server" name="FileUpload" />
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
