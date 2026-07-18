<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Links" CodeBehind="Links.ascx.cs" AutoEventWireup="True" %>
<%@ Register TagPrefix="Portal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>
<portal:title editurl="~/DesktopModules/EditLinks.aspx" edittext="Add Link" runat="server" id="Title1" />
<%-- 中文 / English: 普通链接以主题化链接组呈现；编辑入口仍走既有 EditLinks 页面。 --%>
<asp:datalist id="myDataList" CssClass="portal-content-link-list" RepeatLayout="Flow" runat="server">
    <itemtemplate>
        <div class="portal-content-link-row">
            <asp:hyperlink id="editLink" CssClass="CommandButton portal-content-edit-action" Text="Edit"
                navigateurl='<%# ChooseUrl(DataBinder.Eval(Container.DataItem, "ItemID"), DataBinder.Eval(Container.DataItem, "Url")) %>'
                target='<%# ChooseTarget() %>' tooltip='<%# ChooseTip(DataBinder.Eval(Container.DataItem, "Description")) %>'
                visible='<%# IsEditable %>' runat="server" />
            <span class="Normal portal-content-link-main">
                <asp:hyperlink CssClass="portal-content-link-title" text='<%#: DataBinder.Eval(Container.DataItem, "Title") %>'
                    navigateurl='<%# GetSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "Url")) %>'
                    tooltip='<%# DataBinder.Eval(Container.DataItem, "Description") %>' target="_blank"
                    visible='<%# HasSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "Url")) %>' runat="server"/>
                <asp:Label ID="linkText" CssClass="portal-content-link-title portal-disabled-text"
                    Text='<%#: DataBinder.Eval(Container.DataItem, "Title") %>'
                    Visible='<%# !HasSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "Url")) %>' runat="server" />
            </span>
        </div>
    </itemtemplate>
</asp:datalist>
