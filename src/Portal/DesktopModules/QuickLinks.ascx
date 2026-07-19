<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.QuickLinks" CodeBehind="QuickLinks.ascx.cs" AutoEventWireup="True" %>
<%-- 中文 / English: 快捷链接改为紧凑链接组，保留新增入口和原数据绑定。 --%>
<div class="portal-quicklinks">
    <%-- 中文 / English: P8.3 让快捷链接自有头部接入统一模块标题/动作区契约。 --%>
    <div class="portal-module-header portal-quicklinks-header">
        <div class="portal-module-title-wrap">
            <span class="SubSubHead portal-module-title portal-quicklinks-title">Quick Launch</span>
        </div>
        <asp:Panel id="QuickLinkActions" cssclass="portal-module-actions" EnableViewState="false" runat="server">
            <asp:hyperlink id="EditButton" cssclass="CommandButton portal-module-action portal-secondary-action portal-content-edit-action" enableviewstate="false" runat="server" />
        </asp:Panel>
    </div>
<asp:datalist id="myDataList" CssClass="portal-content-link-list portal-quicklinks-list" RepeatLayout="Flow" enableviewstate="false" runat="server">
    <itemtemplate>
        <div class="portal-content-link-row">
            <asp:hyperlink id="editLink" CssClass="CommandButton portal-content-edit-action" Text="Edit"
                navigateurl='<%# ChooseUrl(DataBinder.Eval(Container.DataItem, "ItemID"), DataBinder.Eval(Container.DataItem, "Url")) %>'
                visible='<%# IsEditable %>' runat="server" />
            <span class="Normal portal-content-link-main">
                <asp:HyperLink ID="quickLink" CssClass="portal-content-link-title"
                    Text='<%#: DataBinder.Eval(Container.DataItem, "Title") %>'
                    NavigateUrl='<%# GetSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "Url")) %>' Target="_blank"
                    Visible='<%# HasSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "Url")) %>' runat="server" />
                <asp:Label ID="quickLinkText" CssClass="portal-content-link-title portal-disabled-text"
                    Text='<%#: DataBinder.Eval(Container.DataItem, "Title") %>'
                    Visible='<%# !HasSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "Url")) %>' runat="server" />
            </span>
        </div>
    </itemtemplate>
</asp:datalist>
</div>
