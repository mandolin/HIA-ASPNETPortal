<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="MyWorkItems.ascx.cs" Inherits="ASPNET.StarterKit.Portal.MyWorkItems" %>

<%--
    <lang>
        <zh-CN>W67 前台「我的待办」聚合入口：只呈现当前用户本人或其持有角色键所指派的未完成待办，不提供就地办理，也不暴露业务正文。</zh-CN>
        <en>W67 front-end "My To-Do Items" aggregation entry: it lists only unfinished work items assigned to the current user or to role keys the user holds, offers no inline processing, and exposes no domain content.</en>
    </lang>
--%>
<%--
    <lang>
        <zh-CN>整块模块在无权限或未认证时由服务器隐藏；标记层不承载任何授权判断，也不因客户端参数改变可见性。</zh-CN>
        <en>The server hides the whole block when the viewer is unauthenticated or lacks permission; the markup carries no authorization decision and never changes visibility from client parameters.</en>
    </lang>
--%>
<%--
    <lang>
        <zh-CN>文本一律走运行时资源表达式，避免新增资源键时同步维护生成属性类；资源缺失时由代码后置回退为键名。</zh-CN>
        <en>All text goes through runtime resource expressions so new resource keys do not require maintaining the generated property class; the code-behind falls back to the key name when a resource is missing.</en>
    </lang>
--%>
<div class="my-work-items">
    <div class="my-work-items-title"><asp:Literal Text="<%$ Resources:lang,MyWorkItems_Heading %>" runat="server" /></div>

    <%--
        <lang>
            <zh-CN>失败态与空态必须分开呈现：读取失败不能退化成"暂无待办"，否则用户会把故障误认为无事项。</zh-CN>
            <en>Failure and empty states must render separately: a read failure must not degrade into "no to-do items", otherwise users mistake a fault for having nothing to do.</en>
        </lang>
    --%>
    <asp:Panel ID="FailurePanel" CssClass="my-work-items-message" Visible="false" runat="server">
        <asp:Label ID="FailureLabel" Text="<%$ Resources:lang,MyWorkItems_ErrorLoadFailed %>" runat="server" />
    </asp:Panel>

    <asp:Panel ID="ContentPanel" Visible="false" runat="server">
        <%--
            <lang>
                <zh-CN>筛选只提供"全部"与"仅超期"两项，沿用服务器回发，不引入客户端组件以保证 IE9+ 可用。</zh-CN>
                <en>The filter offers only "All" and "Overdue only" through server postbacks, with no client-side component so IE9+ support is preserved.</en>
            </lang>
        --%>
        <div class="my-work-items-filter">
            <asp:LinkButton ID="AllFilterButton" OnClick="AllFilterButton_Click" Text="<%$ Resources:lang,MyWorkItems_FilterAll %>" runat="server" />
            &nbsp;|&nbsp;
            <asp:LinkButton ID="OverdueFilterButton" OnClick="OverdueFilterButton_Click" Text="<%$ Resources:lang,MyWorkItems_FilterOverdue %>" runat="server" />
        </div>

        <%--
            <lang>
                <zh-CN>列表使用原生表格与语义化表头；超期除状态样式外必须带文本标记，不能仅靠颜色传达（可访问性约束）。</zh-CN>
                <en>The list uses a native table with semantic headers; overdue state must carry a text marker in addition to styling so the meaning is not conveyed by color alone (accessibility constraint).</en>
            </lang>
        --%>
        <asp:Repeater ID="WorkItemRepeater" OnItemDataBound="WorkItemRepeater_ItemDataBound" runat="server">
            <HeaderTemplate>
                <table class="my-work-items-table">
                    <thead>
                        <tr>
                            <th scope="col"><asp:Literal Text="<%$ Resources:lang,MyWorkItems_ColumnType %>" runat="server" /></th>
                            <th scope="col"><asp:Literal Text="<%$ Resources:lang,MyWorkItems_ColumnItem %>" runat="server" /></th>
                            <th scope="col"><asp:Literal Text="<%$ Resources:lang,MyWorkItems_ColumnCreated %>" runat="server" /></th>
                            <th scope="col"><asp:Literal Text="<%$ Resources:lang,MyWorkItems_ColumnDue %>" runat="server" /></th>
                        </tr>
                    </thead>
                    <tbody>
            </HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td><asp:Label ID="KindLabel" runat="server" /></td>
                    <td>
                        <%--
                            <lang>
                                <zh-CN>事项标题在目标可达时为链接、不可达时为纯文本加提示；可达性由服务器判定，避免把用户送到无权访问的页面。</zh-CN>
                                <en>The item title renders as a link when the target is reachable and as plain text with a hint when it is not; the server decides reachability so users are never sent to a page they cannot access.</en>
                            </lang>
                        --%>
                        <asp:HyperLink ID="ItemLink" Visible="false" runat="server" />
                        <asp:Label ID="ItemTitleLabel" runat="server" />
                        <asp:Label ID="UnavailableLabel" CssClass="my-work-items-unavailable" Visible="false" Text="<%$ Resources:lang,MyWorkItems_NoOnlineEntry %>" runat="server" />
                    </td>
                    <td><%#: Eval("CreatedUtc") %></td>
                    <td>
                        <asp:Label ID="DueLabel" runat="server" />
                        <asp:Label ID="OverdueLabel" CssClass="my-work-items-overdue" Visible="false" runat="server" />
                    </td>
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                    </tbody>
                </table>
            </FooterTemplate>
        </asp:Repeater>

        <%--
            <lang>
                <zh-CN>空态与"筛选无结果"使用不同文案，使"当前没有待办"和"没有符合条件的待办"可区分。</zh-CN>
                <en>The empty state and the "no matching filter" state use different wording so "no to-do items" is distinguishable from "no matching to-do items".</en>
            </lang>
        --%>
        <asp:Panel ID="EmptyPanel" CssClass="my-work-items-message" Visible="false" runat="server">
            <asp:Label ID="EmptyLabel" runat="server" />
        </asp:Panel>
    </asp:Panel>
</div>
