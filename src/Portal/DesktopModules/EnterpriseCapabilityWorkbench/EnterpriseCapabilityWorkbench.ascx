<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="EnterpriseCapabilityWorkbench.ascx.cs" Inherits="ASPNET.StarterKit.Portal.EnterpriseCapabilityWorkbench" %>

<%--
    <lang>
        <zh-CN>P22.4 企业能力工作台首版：为普通用户提供企业协同事项提交和本人事项查看入口，后台处理仍复用现有 Admin 页面。</zh-CN>
        <en>P22.4 first enterprise-capability workbench: gives ordinary users an entry to submit collaboration items and view their own items, while administration handling continues to reuse existing Admin pages.</en>
    </lang>
--%>
<div class="enterprise-workbench">
    <div class="enterprise-workbench-title">企业能力工作台 / Enterprise Capability Workbench</div>
    <asp:Label ID="MessageLabel" CssClass="enterprise-workbench-message" EnableViewState="false" runat="server" />

    <asp:Panel ID="WorkbenchPanel" CssClass="enterprise-workbench-panel" Visible="false" runat="server">
        <%--
            <lang>
                <zh-CN>首版只暴露低敏协同事项字段，不引入附件、动态表单、脚本扩展或具体行业字段，避免在链接治理阶段过早绑定复杂业务。</zh-CN>
                <en>The first version exposes only low-sensitivity collaboration-item fields and avoids attachments, dynamic forms, script extensions, or domain-specific fields so the link-governance phase does not bind too early to complex business.</en>
            </lang>
        --%>
        <div class="enterprise-workbench-form-grid">
            <div class="enterprise-workbench-form-field enterprise-workbench-form-field-wide">
                <span class="SubHead enterprise-workbench-label">事项标题</span>
                <asp:TextBox ID="TitleTextBox" CssClass="NormalTextBox enterprise-workbench-input" MaxLength="200" runat="server" />
            </div>
            <div class="enterprise-workbench-form-field">
                <span class="SubHead enterprise-workbench-label">事项类型</span>
                <asp:DropDownList ID="ItemTypeList" CssClass="NormalTextBox enterprise-workbench-input" runat="server" />
            </div>
            <div class="enterprise-workbench-form-field">
                <span class="SubHead enterprise-workbench-label">优先级</span>
                <asp:DropDownList ID="PriorityList" CssClass="NormalTextBox enterprise-workbench-input" runat="server" />
            </div>
            <div class="enterprise-workbench-form-field enterprise-workbench-form-field-wide">
                <span class="SubHead enterprise-workbench-label">摘要</span>
                <asp:TextBox ID="SummaryTextBox" CssClass="NormalTextBox enterprise-workbench-input" MaxLength="500" runat="server" />
            </div>
            <div class="enterprise-workbench-form-field">
                <span class="SubHead enterprise-workbench-label">期限 UTC</span>
                <asp:TextBox ID="DueUtcTextBox" CssClass="NormalTextBox enterprise-workbench-input" MaxLength="19" runat="server" />
            </div>
            <div class="enterprise-workbench-form-field enterprise-workbench-form-field-full">
                <span class="SubHead enterprise-workbench-label">事项说明</span>
                <asp:TextBox ID="DescriptionTextBox" CssClass="NormalTextBox enterprise-workbench-input enterprise-workbench-body"
                    MaxLength="4000" TextMode="MultiLine" Rows="6" runat="server" />
            </div>
        </div>

        <div class="enterprise-workbench-actions">
            <asp:Button ID="SubmitButton" CssClass="CommandButton" Text="提交协同事项" OnClick="SubmitButton_Click" runat="server" />
        </div>

        <div class="enterprise-workbench-subtitle">我的最近事项</div>
        <div class="enterprise-workbench-list-wrap">
            <asp:Repeater ID="RecentItemsRepeater" OnItemCommand="RecentItemsRepeater_ItemCommand" runat="server">
                <HeaderTemplate>
                    <table class="enterprise-workbench-list" cellspacing="0" cellpadding="4" border="0">
                        <tr>
                            <th>UTC</th>
                            <th>编号</th>
                            <th>标题</th>
                            <th>状态</th>
                            <th>优先级</th>
                            <th>最近意见</th>
                            <th>参与跟进</th>
                        </tr>
                </HeaderTemplate>
                <ItemTemplate>
                        <tr>
                            <td><%#: Eval("LastActionUtcText") %></td>
                            <td><%#: Eval("ItemCode") %></td>
                            <td><%#: Eval("Title") %></td>
                            <td><%#: Eval("StatusText") %></td>
                            <td><%#: Eval("PriorityKey") %></td>
                            <td><%#: Eval("LastActionComment") %></td>
                            <td>
                                <div><span class="SubHead">最新评论：</span><%#: Eval("LatestParticipantComment") %></div>
                                <asp:TextBox ID="ParticipantCommentTextBox" CssClass="NormalTextBox enterprise-workbench-input" MaxLength="1000" TextMode="MultiLine" Rows="2" runat="server" />
                                <div class="enterprise-workbench-actions">
                                    <asp:Button ID="AddParticipantCommentButton" CssClass="CommandButton" Text="添加参与者评论" CommandName="AddParticipantComment" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                    <asp:Button ID="ResubmitButton" CssClass="CommandButton" Text="退回后重新提交" CommandName="Resubmit" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                </div>
                            </td>
                        </tr>
                </ItemTemplate>
                <FooterTemplate>
                    </table>
                </FooterTemplate>
            </asp:Repeater>
        </div>
    </asp:Panel>
</div>
