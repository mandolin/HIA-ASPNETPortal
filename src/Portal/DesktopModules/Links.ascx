<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Links" CodeBehind="Links.ascx.cs" AutoEventWireup="True" %>
<%--
    <lang>
        <zh-CN>共享模块标题控件承载链接编辑入口和主题化标题；动作是否可见仍由服务器权限上下文决定。</zh-CN>
        <en>The shared module-title control hosts the link-edit entry and themed title; action visibility remains a server-side permission decision.</en>
    </lang>
--%>
<%@ Register TagPrefix="Portal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>
<%--
    <lang>
        <zh-CN>标题控件使用站内 EditLinks 页面作为修复旧链接的入口，标记层不直接承载保存或授权逻辑。</zh-CN>
        <en>The title control uses the in-site EditLinks page to repair legacy links; the markup does not contain save or authorization logic.</en>
    </lang>
--%>
<portal:title editurl="~/DesktopModules/EditLinks.aspx" edittext="Add Link" runat="server" id="Title1" />
<%--
    <lang>
        <zh-CN>普通链接以主题化链接组呈现；编辑入口仍走既有 EditLinks 页面。</zh-CN>
        <en>Standard links render as a themed link group; editing still routes through the existing EditLinks page.</en>
    </lang>
--%>
<%--
    <lang>
        <zh-CN>链接列表由当前模块数据服务绑定；标题和说明按展示输出规则处理，客户端项不作为编辑目标或 URL 信任来源。</zh-CN>
        <en>The current module data service binds the link list; titles and descriptions follow display-output rules, and client items are not trusted as edit targets or URL sources.</en>
    </lang>
--%>
<asp:datalist id="myDataList" CssClass="portal-content-link-list" RepeatLayout="Flow" runat="server">
    <itemtemplate>
        <div class="portal-content-link-row">
            <%--
                <lang>
                    <zh-CN>编辑地址只在 IsEditable 上下文使用站内 ItemID 路径；浏览地址必须通过导航策略，非法旧值回退为编码文本。</zh-CN>
                    <en>The edit address uses an in-site ItemID path only in the IsEditable context; browse URLs must pass navigation policy, with invalid legacy values falling back to encoded text.</en>
                </lang>
            --%>
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
