<%-- 用户控件声明 --%>
<%@ Control 
Inherits="ASPNET.StarterKit.Portal.ModuleDefs" 
CodeBehind="ModuleDefs.ascx.cs" 
Language="c#" 
AutoEventWireup="True" %>

<%-- 注册一个自定义标签，用于显示标题 --%>
<%@ Register 
TagPrefix="ASPNETPortal" 
TagName="Title" 
Src="~/DesktopModuleTitle.ascx"%>

<%-- 标题组件 --%>
<ASPNETPortal:title runat="server" id="Title1" />

<%-- 表格布局 --%>
<table cellpadding="2" cellspacing="0" border="0">
    <%-- 数据列表的第一行 --%>
    <tr valign="top">
        <td>
            <%-- 数据列表控件，用于显示模块定义 --%>
            <asp:DataList 
                id="defsList" 
                DataKeyField="ModuleDefID" 
                runat="server" 
                OnItemCommand="DefsList_ItemCommand">
                
                <ItemTemplate>
                    <%-- 编辑按钮 --%>
                    <asp:ImageButton 
                        ImageUrl="~/images/edit.gif" 
                        AlternateText="Edit this item" 
                        runat="server" />
                    &nbsp;&nbsp;
                    <%-- 显示模块定义的友好名称 --%>
                    <asp:Label 
                        Text='<%# DataBinder.Eval(Container.DataItem, "FriendlyName") %>' 
                        CssClass="Normal" 
                        runat="server" />
                </ItemTemplate>
            </asp:DataList>
        </td>
    </tr>
    <%-- 数据列表的最后一行 --%>
    <tr>
        <td>
            <%-- 添加新模块类型的链接按钮 --%>
            <asp:LinkButton 
                cssclass="CommandButton" 
                Text="Add New Module Type" 
                runat="server" 
                id="AddDefBtn" 
                onclick="AddDef_Click" />
        </td>
    </tr>
</table>