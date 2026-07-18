<%-- 控制声明 --%>
<%@ Control Language="c#" Inherits="ASPNET.StarterKit.Portal.Announcements" CodeBehind="Announcements.ascx.cs" AutoEventWireup="True" %>

<%-- 注册自定义控件 --%>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx" %>

<%-- 使用自定义标题控件 --%>
<ASPNETPortal:title EditText="Add New Announcement" EditUrl="~/DesktopModules/EditAnnouncements.aspx" runat="server" id="Title1" />

<%-- 中文 / English: 公告列表保留 DataList 数据绑定，输出改为主题化条目结构。 --%>
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
            <div class="portal-content-item-actions">
                <asp:HyperLink id="moreLink" CssClass="portal-text-action"
                    NavigateUrl='<%# GetSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "MoreLink")) %>'
                    Visible='<%# HasSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "MoreLink")) %>'
                    runat="server" Text='<%$ Resources: lang, Announcements_readMore %>'/>
            </div>
        </div>
    </ItemTemplate>
</asp:DataList>
