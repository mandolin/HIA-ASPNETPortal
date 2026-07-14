<%-- 指定页面的语言、代码隐藏文件、自动事件连线、继承关系和主页文件 --%>
<%@ Page Language="c#" CodeBehind="EditContacts.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditContacts"
    MasterPageFile="~/Default.master" %>

<%-- 定义放置在主内容占位符中的内容 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 主表格 --%>
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <%-- 左侧空白列 --%>
        <tr valign="top">
            <td width="150">
                &nbsp; <%-- 占位符 --%>
            </td>
            <%-- 右侧内容 --%>
            <td>
                <%-- 标题表格 --%>
                <table width="500" cellspacing="0" cellpadding="0" border="0">
                    <tr>
                        <td align="left" class="Head">
                            Contact Details <%-- 联系人详情标题 --%>
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <hr noshade size="1"> <%-- 分割线 --%>
                        </td>
                    </tr>
                </table>
                
                <%-- 输入表格 --%>
                <table width="750" cellspacing="0" cellpadding="0" border="0">
                    <%-- 姓名输入行 --%>
                    <tr valign="top">
                        <td width="100" class="SubHead">
                            Name: <%-- 姓名标签 --%>
                        </td>
                        <td rowspan="5">
                            &nbsp; <%-- 占位符 --%>
                        </td>
                        <td align="left">
                            <asp:TextBox ID="NameField" CssClass="NormalTextBox" Width="390" Columns="30" MaxLength="50"
                                runat="server" /> <%-- 姓名输入框 --%>
                        </td>
                        <td width="25" rowspan="5">
                            &nbsp; <%-- 占位符 --%>
                        </td>
                        <td class="Normal" width="250">
                            <asp:RequiredFieldValidator Display="Static" runat="server" ErrorMessage="You Must Enter a Valid Name"
                                ControlToValidate="NameField" ID="RequiredFieldValidator1" /> <%-- 姓名必填验证器 --%>
                        </td>
                    </tr>
                    
                    <%-- 角色输入行 --%>
                    <tr valign="top">
                        <td class="SubHead">
                            Role: <%-- 角色标签 --%>
                        </td>
                        <td>
                            <asp:TextBox ID="RoleField" CssClass="NormalTextBox" Width="390" Columns="30" MaxLength="100"
                                runat="server" /> <%-- 角色输入框 --%>
                        </td>
                    </tr>
                    
                    <%-- 邮箱输入行 --%>
                    <tr valign="top">
                        <td class="SubHead">
                            Email: <%-- 邮箱标签 --%>
                        </td>
                        <td>
                            <asp:TextBox ID="EmailField" CssClass="NormalTextBox" Width="390" Columns="30" MaxLength="100"
                                runat="server" /> <%-- 邮箱输入框 --%>
                        </td>
                    </tr>
                    
                    <%-- 联系方式1输入行 --%>
                    <tr valign="top">
                        <td class="SubHead">
                            Contact1: <%-- 联系方式1标签 --%>
                        </td>
                        <td>
                            <asp:TextBox ID="Contact1Field" CssClass="NormalTextBox" Width="390" Columns="30"
                                MaxLength="250" runat="server" /> <%-- 联系方式1输入框 --%>
                        </td>
                    </tr>
                    
                    <%-- 联系方式2输入行 --%>
                    <tr valign="top">
                        <td class="SubHead">
                            Contact2: <%-- 联系方式2标签 --%>
                        </td>
                        <td>
                            <asp:TextBox ID="Contact2Field" CssClass="NormalTextBox" Width="390" Columns="30"
                                MaxLength="250" runat="server" /> <%-- 联系方式2输入框 --%>
                        </td>
                    </tr>
                </table>
                
                <%-- 操作按钮 --%>
                <p>
                    <asp:LinkButton ID="updateButton" Text="Update" runat="server" class="CommandButton"
                        BorderStyle="none" OnClick="UpdateBtn_Click" /> <%-- 更新按钮 --%>
                    &nbsp; <%-- 占位符 --%>
                    <asp:LinkButton ID="cancelButton" Text="Cancel" CausesValidation="False" runat="server"
                        class="CommandButton" BorderStyle="none" OnClick="CancelBtn_Click" /> <%-- 取消按钮 --%>
                    &nbsp; <%-- 占位符 --%>
                    <asp:LinkButton ID="deleteButton" Text="Delete this item" CausesValidation="False"
                        runat="server" class="CommandButton" BorderStyle="none" OnClick="DeleteBtn_Click" /> <%-- 删除按钮 --%>
                </p>
                
                <%-- 分割线 --%>
                <hr noshade size="1" width="500">
                
                <%-- 创建者信息 --%>
                <span class="Normal">
                    Created by <%-- 创建者标签 --%>
                    <asp:Label ID="CreatedBy" runat="server" /> <%-- 创建者标签 --%>
                    on <%-- 时间标签 --%>
                    <asp:Label ID="CreatedDate" runat="server" /> <%-- 创建时间标签 --%>
                    <br>
                </span>
                
                <%-- 空白段落 --%>
                <p>
                </p>
            </td>
        </tr>
    </table>
</asp:Content>
