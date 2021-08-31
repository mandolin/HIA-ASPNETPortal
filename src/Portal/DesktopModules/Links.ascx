<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Links" CodeBehind="Links.ascx.cs" AutoEventWireup="True" %>
<%@ Register TagPrefix="Portal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>
<portal:title editurl="~/DesktopModules/EditLinks.aspx" edittext="Add Link" runat="server" id="Title1" />
<asp:datalist id="myDataList" cellpadding="4" width="100%" runat="server">
    <itemtemplate>
        <span class="Normal">
            <asp:hyperlink id="editLink" imageurl="<%# linkImage %>" navigateurl='<%# ChooseURL(Convert.ToString(DataBinder.Eval(Container.DataItem, "ItemID")),
                                        ModuleId.ToString(), (string) DataBinder.Eval(Container.DataItem, "Url")) %>' target='<%# ChooseTarget() %>' tooltip='<%# ChooseTip((string) DataBinder.Eval(Container.DataItem, "Description")) %>' runat="server" />
            <asp:hyperlink text='<%# DataBinder.Eval(Container.DataItem, "Title") %>' navigateurl='<%# DataBinder.Eval(Container.DataItem, "Url") %>' tooltip='<%# DataBinder.Eval(Container.DataItem, "Description") %>' target="_new" runat="server"/>
        </span>
        <br>
    </itemtemplate>
</asp:datalist>