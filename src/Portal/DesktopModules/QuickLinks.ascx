<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.QuickLinks" CodeBehind="QuickLinks.ascx.cs" AutoEventWireup="True" %>
<hr noshade size="1pt" width="98%">
<span class="SubSubHead">Quick Launch</span>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
<asp:hyperlink id="EditButton" cssclass="CommandButton" enableviewstate="false" runat="server" />
<asp:datalist id="myDataList" cellpadding="4" width="100%" enableviewstate="false" runat="server">
    <itemtemplate>
        <span class="Normal">
            <asp:hyperlink id="editLink" imageurl="<%# linkImage %>" navigateurl='<%# ChooseURL(Convert.ToString(DataBinder.Eval(Container.DataItem, "ItemID")),
                                        ModuleId.ToString(), (string) DataBinder.Eval(Container.DataItem, "Url")) %>' runat="server" />
            <a href='<%# DataBinder.Eval(Container.DataItem, "Url") %>'>
                <%# DataBinder.Eval(Container.DataItem, "Title") %>
            </a>
        </span>
        <br>
    </itemtemplate>
</asp:datalist>
<br>