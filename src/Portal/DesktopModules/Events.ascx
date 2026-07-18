<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Events" CodeBehind="Events.ascx.cs" AutoEventWireup="True" %>

<%@ Register TagPrefix="Portal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<%-- 注册门户标题控件 --%>
<portal:title EditText="Add New Event" EditUrl="~/DesktopModules/EditEvents.aspx" runat="server" id=Title1 />

<%-- 中文：保留 DataList 绑定和编辑入口，只重构每条事件的主题化展示结构。English: Keep DataList binding and edit navigation while rebuilding the themed event-item markup. --%>
<asp:DataList id="myDataList" CssClass="portal-content-list portal-event-list" RepeatLayout="Flow" EnableViewState="false" runat="server">
    <ItemTemplate>
        <div class="portal-content-list-item portal-event-item">
            <div class="portal-content-item-title-row">
                <span class="portal-content-item-title">
                    <asp:Label Text='<%#: DataBinder.Eval(Container.DataItem, "Title") %>' runat="server" />
                </span>
                <asp:HyperLink id="editLink"
                               CssClass="portal-button portal-button-secondary portal-button-compact portal-content-edit-action"
                               Text="Edit"
                               NavigateUrl='<%# "~/DesktopModules/EditEvents.aspx?ItemID=" +
                                                DataBinder.Eval(Container.DataItem, "ItemID") +
                                                "&mid=" + ModuleId %>'
                               Visible="<%# IsEditable %>"
                               runat="server" />
            </div>
            <div class="portal-content-item-meta">
                <%#: DataBinder.Eval(Container.DataItem, "WhereWhen") %>
            </div>
            <div class="portal-content-item-summary">
                <%#: DataBinder.Eval(Container.DataItem, "Description") %>
            </div>
        </div>
    </ItemTemplate>
</asp:DataList>
