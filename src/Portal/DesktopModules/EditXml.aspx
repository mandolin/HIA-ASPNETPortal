<%@ Page Language="c#" CodeBehind="EditXml.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditXml"
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
                            XML Settings
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <hr noshade size="1">
                        </td>
                    </tr>
                </table>
                <table width="500" cellspacing="0" cellpadding="0">
                    <tr valign="top">
                        <td width="100" class="SubHead">
                            XML Data File:
                        </td>
                        <td>
                            &nbsp;
                        </td>
                        <td align="right">
                            <asp:TextBox ID="XmlDataSrc" CssClass="NormalTextBox" Columns="26" Width="340" runat="server" />
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            XSL/T Transform File:
                        </td>
                        <td>
                            &nbsp;
                        </td>
                        <td align="right">
                            <asp:TextBox ID="XslTransformSrc" CssClass="NormalTextBox" Columns="26" Width="340"
                                runat="server" />
                        </td>
                    </tr>
                </table>
                <p>
                    <asp:LinkButton ID="updateButton" Text="Update" runat="server" class="CommandButton"
                        BorderStyle="none" OnClick="UpdateBtn_Click" />
                    &nbsp;
                    <asp:LinkButton ID="cancelButton" Text="Cancel" CausesValidation="False" runat="server"
                        class="CommandButton" BorderStyle="none" OnClick="CancelBtn_Click" />
                </p>
            </td>
        </tr>
    </table>
</asp:Content>
