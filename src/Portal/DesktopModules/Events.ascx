<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Events" CodeBehind="Events.ascx.cs" AutoEventWireup="True" %>

<%--
    <lang>
        <zh-CN>共享模块标题控件承载事件编辑入口和主题化标题；动作是否可见由服务器编辑上下文决定。</zh-CN>
        <en>The shared module-title control hosts the event-edit entry and themed title; action visibility is decided by the server edit context.</en>
    </lang>
--%>
<%@ Register TagPrefix="Portal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<%--
    <lang>
        <zh-CN>标题控件只连接既有 EditEvents 页面；列表绑定、过期筛选和权限判断仍由 code-behind 与数据服务负责。</zh-CN>
        <en>The title control points only to the existing EditEvents page; list binding, expiry filtering, and permission decisions remain with code-behind and the data service.</en>
    </lang>
--%>
<portal:title EditText="Add New Event" EditUrl="~/DesktopModules/EditEvents.aspx" runat="server" id=Title1 />

<%--
    <lang>
        <zh-CN>保留 DataList 绑定和编辑入口，只重构每条事件的主题化展示结构。</zh-CN>
        <en>DataList binding and edit navigation are preserved while only the themed event-item markup is rebuilt.</en>
    </lang>
--%>
<%--
    <lang>
        <zh-CN>事件列表关闭 ViewState 并展示数据服务返回的当前模块事件；标题、时间地点和摘要使用编码绑定，避免把正文当作标记输出。</zh-CN>
        <en>The event list disables ViewState and renders events returned for the current module; title, time/place, and summary use encoded binding so content is not emitted as markup.</en>
    </lang>
--%>
<asp:DataList id="myDataList" CssClass="portal-content-list portal-event-list" RepeatLayout="Flow" EnableViewState="false" runat="server">
    <ItemTemplate>
        <div class="portal-content-list-item portal-event-item">
            <div class="portal-content-item-title-row">
                <span class="portal-content-item-title">
                    <asp:Label Text='<%#: DataBinder.Eval(Container.DataItem, "Title") %>' runat="server" />
                </span>
                <%--
                    <lang>
                        <zh-CN>每条编辑链接仅在 IsEditable 为真时可见，并沿用当前 ItemID；按钮可见不代表客户端获得写入授权。</zh-CN>
                        <en>Each edit link is visible only when IsEditable is true and keeps the current ItemID; button visibility does not grant client-side write authorization.</en>
                    </lang>
                --%>
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
