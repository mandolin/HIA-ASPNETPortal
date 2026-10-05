<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Contacts" CodeBehind="Contacts.ascx.cs" AutoEventWireup="True" %>
<%--
    P81.3 空态需要引用共享渲染器。ASCX 标记**不进 msbuild**（构建/单测/XML 三道门禁都看不见），
    缺这条 Import 只在**运行期**抛 CS0103，故此处必须显式写出（沿用 W74 已验证的结论）。
--%>
<%@ Import Namespace="ASPNET.StarterKit.Portal" %>

<%--
    <lang>
        <zh-CN>共享模块标题控件承载联系人编辑入口和主题化标题；动作是否可见仍由服务器编辑上下文决定。</zh-CN>
        <en>The shared module-title control hosts the contact-edit entry and themed title; action visibility remains a server-side edit-context decision.</en>
    </lang>
--%>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx" %>

<ASPNETPortal:title EditText="Add New Contact" EditUrl="~/DesktopModules/EditContacts.aspx" runat="server" id="Title1" />

<%--
    <lang>
        <zh-CN>联系人仍按数据表格呈现，外层使用统一门户表格容器承载主题滚动和边框。</zh-CN>
        <en>Contacts still render as a data table, with the shared portal table wrapper providing themed scrolling and borders.</en>
    </lang>
--%>
<div class="portal-content-table-wrap">
<%--
    <lang>
        <zh-CN>联系人列表由当前模块数据服务绑定并关闭 ViewState；姓名、角色和电话等字段使用编码绑定，客户端行不作为编辑或数据来源。</zh-CN>
        <en>The current module data service binds the contact list with ViewState disabled; name, role, and phone fields use encoded binding, and client rows are not trusted as edit or data sources.</en>
    </lang>
--%>
<asp:Repeater ID="myDataGrid" EnableViewState="false" runat="server">
    <HeaderTemplate>
        <table class="portal-data-table portal-content-table" cellspacing="0" cellpadding="0" border="0" width="100%">
            <tr>
                <th scope="col"></th>
                <%--
                    <lang>
                        <zh-CN>P74.2 起改用本模块自有资源键（文案保持"姓名"不变），不再借用 EmployeeProfileCorrectionRequest 的键；同族的 LegacyEdit_LabelName 文案为"名称"，直接改用会造成可见文案变化，故不采用。</zh-CN>
                        <en>Since P74.2 this header uses the module's own resource key (the wording stays "姓名") instead of borrowing a key from EmployeeProfileCorrectionRequest; the sibling LegacyEdit_LabelName says "名称", so reusing it would change visible wording and was rejected.</en>
                    </lang>
                --%>
                <th scope="col"><%= lang.Contacts_LabelName %></th>
                <th scope="col"><%= lang.LegacyEdit_LabelRole %></th>
                <th scope="col"><%= lang.LegacyEdit_LabelEmail %></th>
                <th scope="col"><%= lang.LegacyEdit_LabelContact1 %></th>
                <th scope="col"><%= lang.LegacyEdit_LabelContact2 %></th>
            </tr>
    </HeaderTemplate>
    <ItemTemplate>
            <tr>
                <td class="portal-content-action-cell">
                    <%--
                        <lang>
                            <zh-CN>编辑链接只在当前用户具备模块编辑权限时显示。</zh-CN>
                            <en>The edit link is shown only when the current user has module edit permission.</en>
                        </lang>
                    --%>
                    <%--
                        <lang>
                            <zh-CN>邮箱链接只有在服务器生成非空 mailto 地址时显示；空值回退为编码文本，不把联系人字段强制变成可点击外部动作。</zh-CN>
                            <en>The email link appears only when the server produces a non-empty mailto address; blank values fall back to encoded text instead of forcing a contact field into a clickable external action.</en>
                        </lang>
                    --%>
                    <asp:HyperLink
                        ID="editLink"
                        CssClass="CommandButton portal-content-edit-action"
                        Text="<%$ Resources:lang,PortalModule_ButtonEdit %>"
                        NavigateUrl='<%# "~/DesktopModules/EditContacts.aspx?ItemID=" + DataBinder.Eval(Container.DataItem, "ItemID") + "&mid=" + ModuleId %>'
                        Visible='<%# IsEditable %>'
                        runat="server" />
                </td>
                <td class="Normal"><%#: DataBinder.Eval(Container.DataItem, "Name") %></td>
                <td class="Normal"><%#: DataBinder.Eval(Container.DataItem, "Role") %></td>
                <td class="Normal">
                    <asp:HyperLink
                        ID="emailLink"
                        Text='<%#: DataBinder.Eval(Container.DataItem, "Email") %>'
                        NavigateUrl='<%# GetMailToUrl(DataBinder.Eval(Container.DataItem, "Email")) %>'
                        Visible='<%# HasEmail(DataBinder.Eval(Container.DataItem, "Email")) %>'
                        runat="server" />
                    <asp:Label
                        ID="emailText"
                        Text='<%#: DataBinder.Eval(Container.DataItem, "Email") %>'
                        Visible='<%# !HasEmail(DataBinder.Eval(Container.DataItem, "Email")) %>'
                        runat="server" />
                </td>
                <td class="Normal"><%#: DataBinder.Eval(Container.DataItem, "Contact1") %></td>
                <td class="Normal"><%#: DataBinder.Eval(Container.DataItem, "Contact2") %></td>
            </tr>
    </ItemTemplate>
    <FooterTemplate>
        <%-- P81.3 空态：无联系人时给出说明行，避免出现"只有表头"的空白表格。 --%>
        <%= PortalEmptyStateRenderer.Render(ContactsEmptyStateRowCount, ContactsEmptyStateText, 6) %>
        </table>
    </FooterTemplate>
</asp:Repeater>
</div>
