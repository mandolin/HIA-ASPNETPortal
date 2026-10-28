using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>「帮助与说明」静态内容模块。</zh-CN>
    ///   <en>Static "Help and Notes" content module.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本模块刻意做成**纯静态**：不读取数据库、不读取任何系统设置、不判定授权、不接受请求参数，
    ///   因此可以安全地对匿名访客渲染。内容固化在控件标记里，**不提供后台编辑入口**——这是 ADR 0027 的边界：
    ///   后台可编辑的 HTML 片段在净化方案落地前不得公开给匿名访客；帮助内容改为随代码发布，从根上消除该 XSS 面。
    ///   可见性仍由页签的 `AccessRoles` 决定，本控件不参与授权判断。</zh-CN>
    ///   <en>This module is deliberately **purely static**: it reads no database, no system setting, performs no authorization
    ///   decision, and accepts no request parameter, so it can be rendered safely to anonymous visitors. Its content is fixed in
    ///   the control markup with **no administration editing surface** — that is the ADR 0027 boundary: administrator-editable HTML
    ///   fragments must not be exposed to anonymous visitors before a sanitization decision exists, so help content ships with the
    ///   code and removes that XSS surface entirely. Visibility is still governed by the tab's `AccessRoles`; this control never
    ///   participates in authorization.</en>
    /// </lang>
    /// </remarks>
    public partial class Help : PortalModuleControl<Help>
    {
    }
}
