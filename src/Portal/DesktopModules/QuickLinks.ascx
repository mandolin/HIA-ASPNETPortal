<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.QuickLinks" CodeBehind="QuickLinks.ascx.cs" AutoEventWireup="True" %>
<hr noshade size="1pt" width="98%">
<span class="SubSubHead">Quick Launch</span>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
<asp:hyperlink id="EditButton" cssclass="CommandButton" enableviewstate="false" runat="server" />
<asp:datalist id="myDataList" cellpadding="4" width="100%" enableviewstate="false" runat="server">
    <itemtemplate>
        <span class="Normal">
            <asp:hyperlink id="editLink" imageurl="<%# linkImage %>" navigateurl='<%# ChooseUrl(DataBinder.Eval(Container.DataItem, "ItemID"), DataBinder.Eval(Container.DataItem, "Url")) %>' visible='<%# CanRenderNavigation(DataBinder.Eval(Container.DataItem, "Url")) %>' runat="server" />
            <asp:HyperLink ID="quickLink" Text='<%#: DataBinder.Eval(Container.DataItem, "Title") %>' NavigateUrl='<%# GetSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "Url")) %>' Target="_blank" Visible='<%# HasSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "Url")) %>' runat="server" />
            <asp:Label ID="quickLinkText" Text='<%#: DataBinder.Eval(Container.DataItem, "Title") %>' Visible='<%# !HasSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "Url")) %>' runat="server" />
        </span>
        <br>
    </itemtemplate>
</asp:datalist>
<br>
