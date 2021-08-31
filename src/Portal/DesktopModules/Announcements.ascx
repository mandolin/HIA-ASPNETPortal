<%@ Control Language="c#" Inherits="ASPNET.StarterKit.Portal.Announcements" CodeBehind="Announcements.ascx.cs" AutoEventWireup="True" %>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<ASPNETPortal:title EditText="Add New Announcement" EditUrl="~/DesktopModules/EditAnnouncements.aspx" runat="server" id=Title1 />
<asp:DataList id="myDataList" CellPadding="4" Width="98%" EnableViewState="false" runat="server">
    <ItemTemplate>
        <asp:HyperLink id="editLink" ImageUrl="~/images/edit.gif" NavigateUrl='<%# "~/DesktopModules/EditAnnouncements.aspx?ItemID=" +
                              DataBinder.Eval(Container.DataItem, "ItemId") + "&mid=" + ModuleId %>' Visible="<%# IsEditable %>" runat="server" />
        <span class="ItemTitle">
            <%# DataBinder.Eval(Container.DataItem, "Title") %>
        </span>
        <br>
        <span class="Normal">
            <%# DataBinder.Eval(Container.DataItem, "Description") %>
            &nbsp;
            <asp:HyperLink id="moreLink" NavigateUrl='<%# DataBinder.Eval(Container.DataItem, "MoreLink") %>' Visible='<%# DataBinder.Eval(Container.DataItem, "MoreLink") != String.Empty %>' runat="server" Text='<%$ Resources: lang, Announcements_readMore %>'/>
        </span>
        <br>
    </ItemTemplate>
</asp:DataList>