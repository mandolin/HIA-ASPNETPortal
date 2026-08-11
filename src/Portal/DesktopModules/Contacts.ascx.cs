using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>渲染当前模块的只读联系人列表。</zh-CN>
    ///   <en>Renders the read-only contact list for the current module.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>联系人姓名、角色、电话等普通字段由 ASCX 标记层编码输出；邮件链接只在存在非空地址时生成 <c>mailto:</c> URL，本控件不承担邮箱格式校验。</zh-CN>
    ///   <en>Ordinary fields such as name, role, and phone are encoded by the ASCX markup layer; the email link only produces a <c>mailto:</c> URL when a non-empty address exists, and this control does not validate email syntax.</en>
    /// </lang>
    /// </remarks>
    public partial class Contacts : PortalModuleControl<Contacts>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>联系人数据访问服务，用于按当前模块 ID 读取联系人数据。</zh-CN>
        ///   <en>Contact data-access service used to read contact data by the current module ID.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IContactsDb ContactsDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取并绑定当前模块联系人。普通字段在标记中通过编码数据绑定输出。</zh-CN>
        ///   <en>Reads and binds contacts for the current module. Ordinary fields are emitted through encoded data binding in the markup.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发页面加载的 Web Forms 事件源。</zh-CN>
        ///   <en>The Web Forms event source that triggered page loading.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>页面加载事件参数；当前实现不读取其内容。</zh-CN>
        ///   <en>The page-load event arguments; the current implementation does not read them.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>联系人列表没有复杂页面状态；每次请求都重新按模块读取，避免旧缓存显示错误模块的数据。</zh-CN>
            //   <en>The contact list has no complex page state; each request reloads by module so stale cache cannot show another module's data.</en>
            // </lang>
            myDataGrid.DataSource = ContactsDB.GetContacts(ModuleId);
            myDataGrid.DataBind();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将联系人邮箱转换为邮件链接地址；空值不会生成链接。</zh-CN>
        ///   <en>Converts the contact email into a mail-link URL; an empty value produces no link.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>数据绑定传入的邮箱字段值。</zh-CN>
        ///   <en>Email field value passed by data binding.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>去除首尾空白后的 <c>mailto:</c> URL；邮箱为空时返回空字符串。</zh-CN>
        ///   <en>A trimmed <c>mailto:</c> URL, or an empty string when the email is blank.</en>
        /// </l>
        /// </returns>
        protected string GetMailToUrl(object value)
        {
            // <lang>
            //   <zh-CN>email 是展示型联系人字段，不代表已验证身份；这里只做空白归一，不做登录语义推断。</zh-CN>
            //   <en>email is a display-oriented contact field rather than verified identity; this path only normalizes blanks and does not infer login semantics.</en>
            // </lang>
            string email = Convert.ToString(value);
            // <lang>
            //   <zh-CN>空邮箱返回空地址，让标记层隐藏链接；非空值只加 mailto 前缀，保留旧模块兼容行为。</zh-CN>
            //   <en>Blank email returns an empty address so markup can hide the link; non-blank values only receive the mailto prefix to preserve legacy module behavior.</en>
            // </lang>
            return string.IsNullOrWhiteSpace(email) ? string.Empty : "mailto:" + email.Trim();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前联系人行是否应显示邮件链接。</zh-CN>
        ///   <en>Determines whether the current contact row should show a mail link.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>数据绑定传入的邮箱字段值。</zh-CN>
        ///   <en>Email field value passed by data binding.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>邮箱值可生成非空 <c>mailto:</c> URL 时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the email value can produce a non-empty <c>mailto:</c> URL.</en>
        /// </l>
        /// </returns>
        protected bool HasEmail(object value)
        {
            // <lang>
            //   <zh-CN>是否显示邮件链接与实际 href 生成使用同一 helper，避免空白邮箱产生可见但无效的链接。</zh-CN>
            //   <en>Mail-link visibility and href generation use the same helper, avoiding visible but invalid links for blank email values.</en>
            // </lang>
            return !string.IsNullOrEmpty(GetMailToUrl(value));
        }
    }
}
