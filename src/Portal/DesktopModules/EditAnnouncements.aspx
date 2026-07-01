<%@ Page Language="c#" CodeBehind="EditAnnouncements.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.EditAnnouncements" MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 外层表格，用于布局 --%>
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="150">
                &nbsp; <%-- 空白列，用于留出左侧空间 --%>
            </td>
            <td width="*">
                <%-- 标题表格 --%>
                <table width="520" cellspacing="0" cellpadding="0">
                    <tr>
                        <td align="left" class="Head">
                            Announcement Details <%-- 标题 --%>
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <hr noshade size="1"> <%-- 分割线 --%>
                        </td>
                    </tr>
                </table>
                
                <%-- 输入表格 --%>
                <table width="750" cellspacing="0" cellpadding="0">
                    <tr valign="top">
                        <td width="100" class="SubHead">
                            Title: <%-- 标题 --%>
                        </td>
                        <td rowspan="5">
                            &nbsp; <%-- 空白列，用于留出中间空间 --%>
                        </td>
                        <td>
                            <asp:TextBox ID="TitleField" CssClass="NormalTextBox" Width="390" Columns="30" MaxLength="100"
                                runat="server" /> <%-- 输入框：标题 --%>
                        </td>
                        <td width="25" rowspan="5">
                            &nbsp; <%-- 空白列，用于留出右侧空间 --%>
                        </td>
                        <td class="Normal" width="250">
                            <asp:RequiredFieldValidator ID="Req1" Display="Static" ErrorMessage="You Must Enter a Valid Title"
                                ControlToValidate="TitleField" runat="server" /> <%-- 必填字段验证器 --%>
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            Read More Link: <%-- 查看更多链接 --%>
                        </td>
                        <td>
                            <asp:TextBox ID="MoreLinkField" CssClass="NormalTextBox" Width="390" Columns="30"
                                MaxLength="100" runat="server" /> <%-- 输入框：更多信息链接 --%>
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead" nowrap>
                            Read More (Mobile): <%-- 查看更多（移动版） --%>
                        </td>
                        <td>
                            <asp:TextBox ID="MobileMoreField" CssClass="NormalTextBox" Width="390" Columns="30"
                                MaxLength="100" runat="server" /> <%-- 输入框：更多信息链接（移动版） --%>
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            Description: <%-- 描述 --%>
                        </td>
                        <td>
                            <asp:TextBox ID="DescriptionField" Width="390" TextMode="Multiline" Columns="44"
                                Rows="6" runat="server" /> <%-- 输入框：描述 --%>
                        </td>
                        <td class="Normal">
                            <asp:RequiredFieldValidator ID="Req2" Display="Static" ErrorMessage="You Must Enter a Valid Description"
                                ControlToValidate="DescriptionField" runat="server" /> <%-- 必填字段验证器 --%>
                        </td>
                    </tr>
                    <tr valign="top">
                        <td class="SubHead">
                            Expires: <%-- 过期日期 --%>
                        </td>
                        <td>
                            <asp:TextBox ID="ExpireField" Text="12/31/2025" CssClass="NormalTextBox" Width="100"
                                Columns="8" runat="server" /> <%-- 输入框：过期日期 --%>
                        </td>
                        <td class="Normal">
                            <asp:RequiredFieldValidator Display="Static" ID="RequiredExpireDate" runat="server"
                                ErrorMessage="You Must Enter a Valid Expiration Date" ControlToValidate="ExpireField" /> <%-- 必填字段验证器 --%>
                            <asp:CompareValidator Display="Static" ID="VerifyExpireDate" runat="server" Operator="DataTypeCheck"
                                ControlToValidate="ExpireField" Type="Date" ErrorMessage="You Must Enter a Valid Expiration Date" /> <%-- 类型验证器 --%>
                        </td>
                    </tr>
                </table>
                
                <%-- 按钮区域 --%>
                <p>
                    <asp:LinkButton ID="updateButton" Text="Update" runat="server" CssClass="CommandButton"
                        BorderStyle="none" OnClick="UpdateBtn_Click" /> <%-- 更新按钮 --%>
                    &nbsp;
                    <asp:LinkButton ID="cancelButton" Text="Cancel" CausesValidation="False" runat="server"
                        CssClass="CommandButton" BorderStyle="none" OnClick="CancelBtn_Click" /> <%-- 取消按钮 --%>
                    &nbsp;
                    <asp:LinkButton ID="deleteButton" Text="Delete this item" CausesValidation="False"
                        runat="server" CssClass="CommandButton" BorderStyle="none" OnClick="DeleteBtn_Click" /> <%-- 删除按钮 --%>
                </p>
                
                <%-- 分割线 --%>
                <hr noshade size="1" width="520">
                
                <%-- 创建者信息 --%>
                <span class="Normal">Created by
                    <asp:Label ID="CreatedBy" runat="server" /> <%-- 创建者标签 --%>
                    on
                    <asp:Label ID="CreatedDate" runat="server" /> <%-- 创建日期标签 --%>
                    <br>
                </span>
            </td>
        </tr>
    </table>
</asp:Content>