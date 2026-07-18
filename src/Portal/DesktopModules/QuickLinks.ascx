<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.QuickLinks" CodeBehind="QuickLinks.ascx.cs" AutoEventWireup="True" %>
<%-- 中文 / English: 快捷链接改为紧凑链接组，保留新增入口和原数据绑定。 --%>
<div class="portal-quicklinks">
    <div class="portal-quicklinks-header">
        <span class="SubSubHead portal-quicklinks-title">Quick Launch</span>
        <asp:hyperlink id="EditButton" cssclass="CommandButton portal-content-edit-action" enableviewstate="false" runat="server" />
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
