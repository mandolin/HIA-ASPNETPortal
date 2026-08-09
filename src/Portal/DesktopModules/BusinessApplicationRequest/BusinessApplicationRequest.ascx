<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="BusinessApplicationRequest.ascx.cs" Inherits="ASPNET.StarterKit.Portal.BusinessApplicationRequest" %>

<%--
    <lang>
        <zh-CN>P19.4 抽象业务申请样板：只提交低敏纯文本申请，用来验证企业能力模块与轻量 Workflow Backbone 的最小闭环。</zh-CN>
        <en>P19.4 abstract business-application sample: submits low-sensitivity plain-text requests to validate the minimal loop between enterprise capability modules and the lightweight workflow backbone.</en>
    </lang>
--%>
<div class="business-application-request">
    <div class="business-application-title">业务申请 / Business Application</div>
    <asp:Label ID="MessageLabel" CssClass="business-application-message" EnableViewState="false" runat="server" />

    <%--
        <lang>
            <zh-CN>RequestPanel 是否可见由服务器根据模块可用性和当前上下文决定；页面不在客户端推断普通用户是否可提交。</zh-CN>
            <en>The server decides RequestPanel visibility from module availability and current context; the page does not infer client-side whether a user may submit.</en>
        </lang>
    --%>
    <asp:Panel ID="RequestPanel" CssClass="business-application-panel" Visible="false" runat="server">
        <%--
            <lang>
                <zh-CN>第一版表单只保留标题、分类、摘要和说明，刻意不引入领域字段、附件或动态脚本，以免样板过早绑定某个实际行业场景。</zh-CN>
                <en>The first form keeps only title, category, summary, and body, intentionally avoiding domain fields, attachments, or dynamic scripts so the sample does not bind too early to a specific industry scenario.</en>
            </lang>
        --%>
        <div class="business-application-form-grid">
            <%--
                <lang>
                    <zh-CN>标题、分类、摘要和说明构成低敏纯文本申请；长度、分类白名单、规范化和敏感信息边界仍由服务器负责。</zh-CN>
                    <en>Title, category, summary, and body form a low-sensitivity plain-text request; length, category allowlists, normalization, and sensitive-data boundaries remain server-owned.</en>
                </lang>
            --%>
            <div class="business-application-form-field business-application-form-field-wide">
                <span class="SubHead business-application-label">标题</span>
                <asp:TextBox ID="TitleTextBox" CssClass="NormalTextBox business-application-input" MaxLength="200" runat="server" />
            </div>
            <div class="business-application-form-field">
                <span class="SubHead business-application-label">分类</span>
                <asp:DropDownList ID="CategoryList" CssClass="NormalTextBox business-application-input" runat="server" />
            </div>
            <div class="business-application-form-field business-application-form-field-wide">
                <span class="SubHead business-application-label">摘要</span>
                <asp:TextBox ID="SummaryTextBox" CssClass="NormalTextBox business-application-input" MaxLength="500" runat="server" />
            </div>
            <div class="business-application-form-field business-application-form-field-full">
                <span class="SubHead business-application-label">申请说明</span>
                <asp:TextBox ID="BodyTextBox" CssClass="NormalTextBox business-application-input business-application-body"
                    MaxLength="4000" TextMode="MultiLine" Rows="6" runat="server" />
            </div>
        </div>

        <%--
            <lang>
                <zh-CN>SubmitButton_Click 进入申请写入流程；最近申请列表使用编码绑定展示服务器结果，不把页面文本当作提交成功证明。</zh-CN>
                <en>SubmitButton_Click enters the application write flow; the recent-application list uses encoded bindings for server results, and page text is not proof of submission success.</en>
            </lang>
        --%>
        <div class="business-application-actions">
            <asp:Button ID="SubmitButton" CssClass="CommandButton" Text="提交申请" OnClick="SubmitButton_Click" runat="server" />
        </div>

        <div class="business-application-subtitle">最近申请</div>
        <div class="business-application-list-wrap">
            <asp:Repeater ID="RecentApplicationsRepeater" runat="server">
                <HeaderTemplate>
                    <table class="business-application-list" cellspacing="0" cellpadding="4" border="0">
                        <tr>
                            <th>UTC</th>
                            <th>编号</th>
                            <th>标题</th>
                            <th>状态</th>
                            <th>审核意见</th>
                        </tr>
                </HeaderTemplate>
                <ItemTemplate>
                        <tr>
                            <td><%#: Eval("SubmittedUtcText") %></td>
                            <td><%#: Eval("ApplicationCode") %></td>
                            <td><%#: Eval("Title") %></td>
                            <td><%#: Eval("ApplicationStatus") %></td>
                            <td><%#: Eval("ReviewComment") %></td>
                        </tr>
                </ItemTemplate>
                <FooterTemplate>
                    </table>
                </FooterTemplate>
            </asp:Repeater>
        </div>
    </asp:Panel>
</div>
