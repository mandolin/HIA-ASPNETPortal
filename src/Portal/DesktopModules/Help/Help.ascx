<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Help.ascx.cs" Inherits="ASPNET.StarterKit.Portal.Help" %>
<%--
    <lang>
        <zh-CN>「帮助与说明」页：解释登录方式、当前可用功能与获取支持的途径。
        内容随代码发布，不来自数据库、不接受请求参数，因此对匿名访客也安全（ADR 0027：后台可编辑 HTML 在净化方案落地前不公开）。</zh-CN>
        <en>"Help and Notes" page: explains how to sign in, which capabilities are currently available, and how to get support.
        Content ships with the code, comes from no database, and accepts no request parameter, so it is safe for anonymous visitors
        (ADR 0027: administrator-editable HTML is not exposed before a sanitization decision exists).</en>
    </lang>
--%>
<style type="text/css">
    /* <lang><zh-CN>样式只使用主题已声明的变量，浅色与深色主题自动跟随，不写死颜色。</zh-CN>
       <en>Styles consume only theme-declared variables so light and dark themes follow automatically without hard-coded colors.</en></lang> */
    .help-page { color: inherit; }
    .help-page h2 { font-size: 20px; margin: 0 0 6px; }
    .help-page .help-lead { font-size: 13px; opacity: .78; margin: 0 0 18px; }
    .help-page h3 { font-size: 15px; margin: 20px 0 8px; }
    .help-page ul { margin: 0; padding-left: 20px; }
    .help-page li { font-size: 13px; line-height: 1.9; }
    .help-page .help-card { border: 1px solid var(--portal-border); border-radius: 12px; padding: 16px 18px; background: var(--portal-surface); }
    .help-page .help-muted { font-size: 12px; opacity: .72; margin-top: 16px; }
</style>
<div class="help-page">
    <h2>帮助与说明</h2>
    <p class="help-lead">Help and Notes</p>

    <div class="help-card">
        <h3>如何登录</h3>
        <ul>
            <li>使用企业账号（用户名、邮箱或员工编号）与密码登录。</li>
            <li>首次登录后请先在「我的信息」确认员工资料，资料有误可提交更正申请。</li>
            <li>忘记密码请联系平台管理员重置；登录会话超时后重新登录即可。</li>
        </ul>

        <h3>当前可用功能</h3>
        <ul>
            <li><strong>首页</strong>：登录入口与公开公告（仅显示标记为「游客可见」的条目）。</li>
            <li><strong>我的待办</strong>：汇总指派给你的未完成事项（需登录）。</li>
            <li><strong>业务办理</strong>：发起业务申请并查看审批进度（需登录）。</li>
            <li><strong>请假与报销</strong>：提交申请并跟踪审批状态（需登录）。</li>
            <li><strong>我的信息</strong>：确认或更正员工资料（需登录）。</li>
        </ul>

        <h3>遇到问题</h3>
        <ul>
            <li>页面显示「无权限」：说明当前账号未被授予该功能所需的角色，请联系平台管理员。</li>
            <li>功能入口点击后提示登录：该功能仅对已登录员工开放，属预期行为。</li>
            <li>其他问题：请提供页面地址、操作时间与错误提示，便于定位。</li>
        </ul>

        <p class="help-muted">说明：本页内容随版本发布，管理员无法在后台编辑，以避免对匿名访客暴露可编辑内容。</p>
    </div>
</div>
