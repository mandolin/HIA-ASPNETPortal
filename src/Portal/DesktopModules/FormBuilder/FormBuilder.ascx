<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="FormBuilder.ascx.cs" Inherits="ASPNET.StarterKit.Portal.FormBuilder" %>
<%-- <lang>
  <zh-CN>表单构建器模块：依据可信本地 form.json 渲染表单、采集提交并落库；能力由本体之外以独立模块形式提供，经容纳机制装载。</zh-CN>
  <en>Form Builder module: renders a form from the trusted local form.json, collects submissions and persists them; the capability is provided externally as an independent module and loaded by the hosting mechanism.</en>
</lang> --%>
<div class="form-builder">
    <div class="form-builder-title" role="heading" aria-level="2">
        <asp:Literal ID="TitleLiteral" runat="server" />
    </div>
    <asp:Label ID="MessageLabel" CssClass="form-builder-message" runat="server" />
    <asp:Literal ID="FormLiteral" runat="server" />
    <div class="form-builder-actions">
        <asp:Button ID="SubmitButton" CssClass="CommandButton" OnClick="SubmitButton_Click" runat="server" />
    </div>
</div>
