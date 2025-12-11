<%-- 控制声明 --%>
<%@ Control Language="c#" Inherits="ASPNET.StarterKit.Portal.Announcements" CodeBehind="Announcements.ascx.cs" AutoEventWireup="True" %>

<%-- 注册自定义控件 --%>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx" %>

<%-- 使用自定义标题控件 --%>
<ASPNETPortal:title EditText="Add New Announcement" EditUrl="~/DesktopModules/EditAnnouncements.aspx" runat="server" id="Title1" />

<%-- 数据列表控件 --%>
<asp:DataList id="myDataList" CellPadding="4" Width="98%" EnableViewState="false" runat="server">
    
    <ItemTemplate>
        <%-- 编辑链接 --%>
        <asp:HyperLink id="editLink" ImageUrl="~/images/edit.gif"
                       NavigateUrl='<%# "~/DesktopModules/EditAnnouncements.aspx?ItemID=" + 
                                        DataBinder.Eval(Container.DataItem, "ItemId").ToString() + 
                                        "&mid=" + ModuleId %>'
                       Visible='<%# IsEditable %>' runat="server" />
        
        <%-- 公告标题 --%>
        <span class="ItemTitle">
            <%# DataBinder.Eval(Container.DataItem, "Title") %>
        </span>
        
        <%-- 换行 --%>
        <br>
        
        <%-- 公告描述 --%>
        <span class="Normal">
            <%# DataBinder.Eval(Container.DataItem, "Description") %>
            &nbsp;
            
            <%-- 查看更多链接 --%>
            <asp:HyperLink id="moreLink" NavigateUrl='<%# DataBinder.Eval(Container.DataItem, "MoreLink") %>' 
                           Visible='<%# DataBinder.Eval(Container.DataItem, "MoreLink") != String.Empty %>' 
                           runat="server" Text='<%$ Resources: lang, Announcements_readMore %>'/>
        </span>
        
        <%-- 换行 --%>
        <br>
    </ItemTemplate>
</asp:DataList>