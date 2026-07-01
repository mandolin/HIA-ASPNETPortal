<%@ Page 
    Language="c#" 
    CodeBehind="ModuleDefinitions.aspx.cs" 
    AutoEventWireup="True" 
    Inherits="ASPNET.StarterKit.Portal.ModuleDefinitions" 
    MasterPageFile="~/Default.master" %>

<%--
    The ModuleDefinitions.aspx page is used to create and edit module definitions within
    the Portal application.
--%>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 主要表格布局 --%>
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="150">
                &nbsp;
            </td>
            <td width="*">
                <%-- 标题表格 --%>
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

                <%-- 输入表单 --%>
                <table width="750" cellspacing="0" cellpadding="0" border="0">
                    <tr>
                        <td width="100" class="SubHead">
                            Friendly Name:  <%-- 友好名称 --%>
                        </td>
                        <td rowspan="5">
                            &nbsp;
                        </td>
                        <td>
                            <%-- 友好名称输入框 --%>
                            <asp:TextBox 
                                ID="FriendlyName" 
                                CssClass="NormalTextBox" 
                                Width="390" 
                                Columns="30" 
                                MaxLength="150" 
                                runat="server" />
                        </td>
                        <td width="25" rowspan="5">
                            &nbsp;
                        </td>
                        <td class="Normal" width="250">
                            <%-- 必填字段验证器 --%>
                            <asp:RequiredFieldValidator 
                                ID="Req1" 
                                Display="Static" 
                                ErrorMessage="Enter a Module Name" 
                                ControlToValidate="FriendlyName" 
                                runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead" nowrap>
                            Desktop Source:  <%-- 桌面源路径 --%>
                        </td>
                        <td>
                            <%-- 桌面源路径输入框 --%>
                            <asp:TextBox 
                                ID="DesktopSrc" 
                                CssClass="NormalTextBox" 
                                Width="390" 
                                Columns="30" 
                                MaxLength="150" 
                                runat="server" />
                        </td>
                        <td class="Normal">
                            <%-- 必填字段验证器 --%>
                            <asp:RequiredFieldValidator 
                                ID="Req2" 
                                Display="Static" 
                                ErrorMessage="You Must Enter Source Path for the Desktop Module" 
                                ControlToValidate="DesktopSrc" 
                                runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">
                            Mobile Source:  <%-- 移动端源路径 --%>
                        </td>
                        <td>
                            <%-- 移动端源路径输入框 --%>
                            <asp:TextBox 
                                ID="MobileSrc" 
                                CssClass="NormalTextBox" 
                                Width="390" 
                                Columns="30" 
                                MaxLength="150" 
                                runat="server" />
                        </td>
                        <td>
                            &nbsp;
                        </td>
                    </tr>
                </table>

                <%-- 按钮区域 --%>
                <p>
                    <%-- 更新按钮 --%>
                    <asp:LinkButton 
                        ID="updateButton" 
                        Text="Update" 
                        runat="server" 
                        class="CommandButton" 
                        BorderStyle="none" 
                        OnClick="UpdateBtn_Click" />
                    &nbsp;
                    <%-- 取消按钮 --%>
                    <asp:LinkButton 
                        ID="cancelButton" 
                        Text="Cancel" 
                        CausesValidation="False" 
                        runat="server" 
                        class="CommandButton" 
                        BorderStyle="none" 
                        OnClick="CancelBtn_Click" />
                    &nbsp;
                    <%-- 删除按钮 --%>
                    <asp:LinkButton 
                        ID="deleteButton" 
                        Text="Delete this module type" 
                        CausesValidation="False" 
                        runat="server" 
                        class="CommandButton" 
                        BorderStyle="none" 
                        OnClick="DeleteBtn_Click" />
                </p>
            </td>
        </tr>
    </table>
</asp:Content>