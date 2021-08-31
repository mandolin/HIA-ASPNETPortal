<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Discussion" CodeBehind="Discussion.ascx.cs" AutoEventWireup="True" %>
<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>
<ASPNETPortal:title id="Title1" runat="server" EditTarget="_new" EditUrl="~/DesktopModules/DiscussDetails.aspx" EditText="Add New Thread"></ASPNETPortal:title>
<%-- discussion list --%>
<asp:datalist id="TopLevelList" runat="server" DataKeyField="Parent" ItemStyle-Cssclass="Normal" width="98%">
    <ItemTemplate>
        <asp:ImageButton id="btnSelect" ImageUrl='<%# NodeImage((int) DataBinder.Eval(Container.DataItem, "ChildCount")) %>' CommandName="select" runat="server" />
        <asp:hyperlink Text='<%# DataBinder.Eval(Container.DataItem, "Title") %>' NavigateUrl='<%# FormatUrl((int) DataBinder.Eval(Container.DataItem, "ItemID")) %>' Target="_new" runat="server" ID="Hyperlink1" />, 
        from
        <%# DataBinder.Eval(Container.DataItem, "CreatedByUser") %>
        , posted
        <%# DataBinder.Eval(Container.DataItem, "CreatedDate", "{0:g}") %>
    </ItemTemplate>
    <SelectedItemTemplate>
        <asp:ImageButton id="btnCollapse" ImageUrl="~/images/minus.gif" runat="server" CommandName="collapse" />
        <asp:hyperlink Text='<%# DataBinder.Eval(Container.DataItem, "Title") %>' NavigateUrl='<%# FormatUrl((int) DataBinder.Eval(Container.DataItem, "ItemID")) %>' Target="_new" runat="server" ID="Hyperlink2" />, 
        from
        <%# DataBinder.Eval(Container.DataItem, "CreatedByUser") %>
        , posted
        <%# DataBinder.Eval(Container.DataItem, "CreatedDate", "{0:g}") %>
        <asp:DataList id="DetailList" ItemStyle-Cssclass="Normal" datasource="<%# GetThreadMessages() %>" runat="server">
            <ItemTemplate>
                <%# DataBinder.Eval(Container.DataItem, "Indent") %>
                <img src="<%= Global.GetApplicationPath(Request) %>/images/1x1.gif" height="15">
                <asp:hyperlink Text='<%# DataBinder.Eval(Container.DataItem, "Title") %>' NavigateUrl='<%# FormatUrl((int) DataBinder.Eval(Container.DataItem, "ItemID")) %>' Target="_new" runat="server" ID="Hyperlink3" />, 
                from
                <%# DataBinder.Eval(Container.DataItem, "CreatedByUser") %>
                , posted
                <%# DataBinder.Eval(Container.DataItem, "CreatedDate", "{0:g}") %>
            </ItemTemplate>
        </asp:DataList>
    </SelectedItemTemplate>
</asp:datalist>