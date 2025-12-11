<%-- 页面声明 --%>
<%@ Page Language="c#" CodeBehind="EditDocs.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditDocs"
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
                <table width="500" cellspacing="0" cellpadding="0">
                    <tr>
                        <td align="left" class="Head">
                            Document Details <%-- 文档详情标题 --%>
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <hr noshade size="1"> <%-- 分割线 --%>
                        </td>
                    </tr>
                </table>
                
                <%-- 输入表格 --%>
                <table width="726" cellspacing="0" cellpadding="0" border="0">
                    <%-- 名称输入行 --%>
                    <tr valign="top">
                        <td width="100" class="SubHead">
                            Name: <%-- 名称标签 --%>
                        </td>
                        <td>
                            &nbsp; <%-- 占位符 --%>
                        </td>
                        <td>
                            <asp:TextBox ID="NameField" CssClass="NormalTextBox" Width="353" Columns="28" MaxLength="150"
                                runat="server" /> <%-- 名称输入框 --%>
                        </td>
                        <td width="25" rowspan="6">
                            &nbsp; <%-- 占位符 --%>
                        </td>
                        <td class="Normal" width="250">
                            <asp:RequiredFieldValidator Display="Static" runat="server" ErrorMessage="You Must Enter a Valid Name"
                                ControlToValidate="NameField" ID="RequiredFieldValidator1" /> <%-- 名称必填验证器 --%>
                        </td>
                    </tr>
                    
                    <%-- 类别输入行 --%>
                    <tr valign="top">
                        <td class="SubHead">
                            Category: <%-- 类别标签 --%>
                        </td>
                        <td>
                            &nbsp; <%-- 占位符 --%>
                        </td>
                        <td>
                            <asp:TextBox ID="CategoryField" CssClass="NormalTextBox" Width="353" Columns="28"
                                MaxLength="50" runat="server" /> <%-- 类别输入框 --%>
                        </td>
                    </tr>
                    
                    <%-- 分割线 --%>
                    <tr>
                        <td>
                            &nbsp; <%-- 占位符 --%>
                        </td>
                        <td colspan="2">
                            <hr noshade size="1" width="100%"> <%-- 分割线 --%>
                        </td>
                    </tr>
                    
                    <%-- 浏览URL输入行 --%>
                    <tr valign="top">
                        <td width="100" class="SubHead">
                            URL to Browse: <%-- 浏览URL标签 --%>
                        </td>
                        <td>
                            &nbsp; <%-- 占位符 --%>
                        </td>
                        <td>
                            <asp:TextBox ID="PathField" CssClass="NormalTextBox" Width="353" Columns="28" MaxLength="250"
                                runat="server" /> <%-- 浏览URL输入框 --%>
                        </td>
                    </tr>
                    
                    <%-- 未知标签行 --%>
                    <tr>
                        <td class="SubHead">
                            ?or ? <%-- 未知标签 --%>
                        </td>
                        <td colspan="2">
                            &nbsp; <%-- 占位符 --%>
                            <br>
                            <br>
                        </td>
                    </tr>
                    
                    <%-- 上传选项行 --%>
                    <tr valign="top">
                        <td nowrap class="SubHead">
                            Upload to Web Server:&nbsp; <%-- 上传到服务器标签 --%>
                        </td>
                        <td>
                            &nbsp; <%-- 占位符 --%>
                        </td>
                        <td>
                            <asp:CheckBox ID="Upload" CssClass="Normal" Text="Upload document to server" runat="server" /> <%-- 上传文档到服务器复选框 --%>
                            <br />
                            <asp:CheckBox ID="storeInDatabase" CssClass="Normal" Text="Store in database (web farm support)" runat="server" /> <%-- 存储在数据库中复选框 --%>
                            <br />
                            <input type="file" id="FileUpload" width="300" style="font-family: verdana; width: 353px;" runat="server" name="FileUpload" /> <%-- 文件上传控件 --%>
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