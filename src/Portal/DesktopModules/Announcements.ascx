<%@ Control Language="c#" Inherits="ASPNET.StarterKit.Portal.Announcements" CodeBehind="Announcements.ascx.cs" AutoEventWireup="True" %>

<%--
    <lang>
        <zh-CN>共享模块标题控件承载公告编辑入口和主题化标题；动作是否可见由服务器编辑上下文决定。</zh-CN>
        <en>The shared module-title control hosts the announcement-edit entry and themed title; action visibility is decided by the server edit context.</en>
    </lang>
--%>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx" %>

<%--
    <lang>
        <zh-CN>标题控件只连接既有 EditAnnouncements 页面；公告数据绑定和有效记录筛选仍由 code-behind 与数据服务负责。</zh-CN>
        <en>The title control points only to the existing EditAnnouncements page; announcement binding and active-record filtering remain with code-behind and the data service.</en>
    </lang>
--%>
<ASPNETPortal:title EditText="Add New Announcement" EditUrl="~/DesktopModules/EditAnnouncements.aspx" runat="server" id="Title1" />

<%--
    <lang>
        <zh-CN>公告列表保留 DataList 数据绑定，输出改为主题化条目结构。</zh-CN>
        <en>The announcements list keeps DataList binding while rendering through themed item markup.</en>
    </lang>
--%>
<%--
    <lang>
        <zh-CN>公告列表关闭 ViewState，标题和正文使用编码绑定；旧记录的更多链接不因存在值就自动输出为可点击地址。</zh-CN>
        <en>The announcement list disables ViewState and uses encoded binding for title and body; a legacy read-more value is not emitted as a clickable address merely because it exists.</en>
    </lang>
--%>
<asp:DataList id="myDataList" CssClass="portal-content-list portal-announcement-list" RepeatLayout="Flow" EnableViewState="false" runat="server">
    <ItemTemplate>
        <div class="portal-content-list-item portal-announcement-item">
            <div class="portal-content-item-title-row">
                <span class="ItemTitle portal-content-item-title"><%#: DataBinder.Eval(Container.DataItem, "Title") %></span>
                <asp:HyperLink id="editLink" CssClass="CommandButton portal-content-edit-action" Text="Edit"
                    NavigateUrl='<%# "~/DesktopModules/EditAnnouncements.aspx?ItemID=" +
                                     DataBinder.Eval(Container.DataItem, "ItemId").ToString() +
                                     "&mid=" + ModuleId %>'
                    Visible='<%# IsEditable %>' runat="server" />
            </div>
            <div class="Normal portal-content-item-summary">
                <%#: DataBinder.Eval(Container.DataItem, "Description") %>
            </div>
            <%--
                <lang>
                    <zh-CN>更多链接只有在服务器导航策略通过时显示；非法地址保持隐藏，不把原始历史值回显给客户端。</zh-CN>
                    <en>The read-more link appears only after the server navigation policy passes; invalid addresses stay hidden instead of echoing raw legacy values to the client.</en>
                </lang>
            --%>
            <div class="portal-content-item-actions">
                <asp:HyperLink id="moreLink" CssClass="portal-text-action"
                    NavigateUrl='<%# GetSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "MoreLink")) %>'
                    Visible='<%# HasSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "MoreLink")) %>'
                    runat="server" Text='<%$ Resources: lang, Announcements_readMore %>'/>
            </div>
        </div>
    </ItemTemplate>
</asp:DataList>
