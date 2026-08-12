/* <lang><zh-CN>本脚本面向已由 `Portal_CreateDB.sql` 重建完成的本地 `Portal` 数据库，负责灌入 ASP.NET Portal Starter Kit 的演示门户数据；它会先清空同名基础表再重新写入固定主键种子，因此只能用于可重建环境，不能直接套用于保留业务数据的实例。</zh-CN><en>This script targets the local `Portal` database after `Portal_CreateDB.sql` has rebuilt the schema, and loads the ASP.NET Portal Starter Kit demo seed data; it first clears the same foundational tables and then writes fixed-key seed rows, so it is suitable only for rebuildable environments and must not be applied directly to instances that retain business data.</en></lang> */
use [Portal]

/* <lang><zh-CN>清空阶段按旧门户外键依赖的反向顺序删除设置、成员关系、用户、内容记录、模块、Tab、全局配置和角色；这里使用 `DELETE` 而不是 `TRUNCATE`，是为了兼容存在外键约束与 identity seed 显式恢复的安装路径。</zh-CN><en>The reset phase deletes settings, memberships, users, content records, modules, tabs, globals, and roles in the reverse order of the legacy portal foreign-key dependencies; it uses `DELETE` rather than `TRUNCATE` to remain compatible with foreign-key constraints and the explicit identity reseeding path.</en></lang> */
/****** Object:  Table [dbo].[PortalCfg_ModuleSettings]    Script Date: 03/01/2012 22:46:17 ******/
DELETE FROM [PortalCfg_ModuleSettings]
GO


/****** Object:  Table [dbo].[Portal_UserRoles]    Script Date: 03/01/2012 22:46:17 ******/
DELETE FROM [Portal_UserRoles]
GO
/****** Object:  Table [dbo].[Portal_Users]    Script Date: 03/01/2012 22:46:17 ******/
DELETE FROM [Portal_Users]
GO

/****** Object:  Table [dbo].[Portal_Announcements]    Script Date: 03/01/2012 22:46:17 ******/
DELETE FROM [Portal_Announcements]
GO
/****** Object:  Table [dbo].[Portal_Contacts]    Script Date: 03/01/2012 22:46:17 ******/
DELETE FROM [Portal_Contacts]
GO
/****** Object:  Table [dbo].[Portal_Discussion]    Script Date: 03/01/2012 22:46:17 ******/
DELETE FROM [Portal_Discussion]
GO
/****** Object:  Table [dbo].[Portal_Documents]    Script Date: 03/01/2012 22:46:17 ******/
DELETE FROM [Portal_Documents]
GO
/* <lang><zh-CN>事件种子提供首页事件模块的两条固定样例记录，日期使用 SQL Server 十六进制 DateTime literal 以复现原 Starter Kit 数据；迁移时应保持语义而非依赖当前区域设置解析。</zh-CN><en>The event seed provides two fixed sample rows for the home events module, with dates represented as SQL Server hexadecimal DateTime literals to reproduce the original Starter Kit data; migrations should preserve that meaning rather than depend on current-locale parsing.</en></lang> */
/****** Object:  Table [dbo].[Portal_Events]    Script Date: 03/01/2012 22:46:17 ******/
DELETE FROM [Portal_Events]
GO
/****** Object:  Table [dbo].[Portal_HtmlText]    Script Date: 03/01/2012 22:46:17 ******/
DELETE FROM [Portal_HtmlText]
GO
/* <lang><zh-CN>链接种子为两个 Quick Links 模块提供固定显示顺序和历史示例 URL；`ViewOrder` 的奇数间隔是旧管理界面可插入排序的演示数据，不应在注释批次中压缩。</zh-CN><en>The links seed provides fixed display ordering and historical sample URLs for two Quick Links modules; the odd-numbered `ViewOrder` spacing is demo data for insertion-style ordering in the legacy admin UI and should not be compacted during a comment-only pass.</en></lang> */
/****** Object:  Table [dbo].[Portal_Links]    Script Date: 03/01/2012 22:46:17 ******/
DELETE FROM [Portal_Links]
GO
/****** Object:  Table [dbo].[PortalCfg_Modules]    Script Date: 03/01/2012 22:46:17 ******/
DELETE FROM [PortalCfg_Modules]
GO
/****** Object:  Table [dbo].[PortalCfg_Tabs]    Script Date: 03/01/2012 22:46:17 ******/
DELETE FROM [PortalCfg_Tabs]
GO
/* <lang><zh-CN>门户全局设置种子恢复 `PortalId = 1` 的站点名称和编辑按钮默认策略；这些值是后续 Tab、模块和后台设置页共同读取的门户级基线。</zh-CN><en>The portal-global seed restores the `PortalId = 1` site name and default edit-button policy; these values form the portal-wide baseline read by later tabs, modules, and the admin site-settings page.</en></lang> */
/****** Object:  Table [dbo].[PortalCfg_Globals]    Script Date: 03/01/2012 22:46:17 ******/
DELETE FROM [PortalCfg_Globals]
GO
/****** Object:  Table [dbo].[Portal_Roles]    Script Date: 03/01/2012 22:46:17 ******/
DELETE FROM [Portal_Roles]
GO
/* <lang><zh-CN>角色种子只创建固定 `RoleID = 0` 的 `Admins` 角色；后续用户、Tab 访问规则、模块编辑规则都以这个历史角色名称为授权锚点，因此主键和文本值必须保持与旧 Web Forms 代码契约一致。</zh-CN><en>The role seed creates only the fixed `RoleID = 0` `Admins` role; later user, tab-access, and module-editing rules use this historical role name as their authorization anchor, so both the primary key and the text value must remain aligned with the legacy Web Forms code contract.</en></lang> */
/****** Object:  Table [dbo].[Portal_Roles]    Script Date: 03/01/2012 22:46:17 ******/
SET IDENTITY_INSERT [Portal_Roles] ON
INSERT [Portal_Roles] ([RoleID], [PortalID], [RoleName]) VALUES (0, 0, N'Admins')
SET IDENTITY_INSERT [Portal_Roles] OFF

/* <lang><zh-CN>`Portal_HtmlText` 种子包含大量作为 SQL Unicode 字符串保存的演示 HTML；注释必须停留在 SQL 语句外层，避免进入文本常量后改变页面正文、HTML 转义、图片路径或移动端摘要。</zh-CN><en>The `Portal_HtmlText` seed contains large demo HTML fragments stored as SQL Unicode strings; comments must stay outside the SQL statement bodies to avoid changing page text, HTML escaping, image paths, or mobile summaries.</en></lang> */
/****** Object:  Table [dbo].[Portal_HtmlText]    Script Date: 03/01/2012 22:46:17 ******/
INSERT [Portal_HtmlText] ([ModuleID], [DesktopHtml], [MobileSummary], [MobileDetails]) VALUES (2, N'&lt;table cellSpacing=&quot;0&quot; cellPadding=&quot;5&quot; border=&quot;0&quot;&gt;
    &lt;tr valign=&quot;top&quot;&gt;
        &lt;td&gt;
            &lt;a target=&quot;_blank&quot; href=&quot;http://www.ibuyspy.com&quot;&gt;
                &lt;img src=&quot;data/logoneg.gif&quot; border=&quot;0&quot; align=&quot;left&quot; hspace=&quot;10&quot;&gt;
            &lt;/a&gt;
        &lt;/td&gt;
        &lt;td class=&quot;Normal&quot; width=&quot;100%&quot;&gt;
            The &lt;b&gt;Portal Starter Kit&lt;/b&gt; has everything you need and is flexible enough to serve as the hub application for an enterprises internal operations or as an internet portal. It provides online news and information sharing, event and sales information, interactive discussion forums and employee contact information. In a nutshell, the Portal Starter Kit has everything needed to maintain and run a fast-growing commercial enterprise.
            &lt;br&gt;
            &lt;br&gt;
            Feel free to browse the site and explore. Sign in to obtain access to different modules within the framework, as well as view the restricted sections of the site.
        &lt;/td&gt;
    &lt;/tr&gt;
&lt;/table&gt;', N'Welcome to the &lt;b&gt;Portal Starter Kit&lt;/b&gt;, the Intranet Home for corporate employees or as your personal portal.', N'This site can serve as the hub application for an enterprises internal operations.  It provides online news, event and sales information, along with interactive discussion forums and employee contact information.  In a nutshell, everything needed to maintain and run a fast-growing commercial enterprise.  Feel free to browse the site and explore.  

&lt;br&gt;&lt;br&gt;Sign in with a desktop browser to obtain edit access to different modules within the framework, as well as view the restricted sections of the site.
')
INSERT [Portal_HtmlText] ([ModuleID], [DesktopHtml], [MobileSummary], [MobileDetails]) VALUES (5, N'&lt;span class=&quot;Normal&quot;&gt;
The QLT2112 &lt;a href=&quot;http://www.ibuyspy.com/store/ProductDetails.aspx?productID=399&quot;&gt;&lt;b&gt;Document Transportation System&lt;/b&gt;&lt;/a&gt; is on special this week to clear an overstock.  Purchasers of the P38 &lt;a href=&quot;http://www.ibuyspy.com/store/ProductDetails.aspx?productID=357&quot;&gt;Escape Vehicle (Air)&lt;/a&gt; receive one free.
&lt;p&gt;
&lt;img align=&quot;left&quot; src=&quot;data/qlt2112.gif&quot;&gt;
&lt;/p&gt;
&lt;/span&gt;
', N'The QLT2112 &lt;a href=&quot;http://www.ibuyspy.com/store&quot;&gt;&lt;b&gt;Document Transportation System&lt;/b&gt;&lt;/a&gt; is on special this week to clear an overstock.', N'The QLT2112 &lt;a href=&quot;http://www.ibuyspy.com/store/ProductDetails.aspx?productID=399&quot;&gt;&lt;b&gt;Document Transportation System&lt;/b&gt;&lt;/a&gt; is on special this week to clear an overstock.  Purchasers of the P38 &lt;a href=&quot;http://www.ibuyspy.com/store/ProductDetails.aspx?productID=357&quot;&gt;Escape Vehicle (Air)&lt;/a&gt; receive one free.')
INSERT [Portal_HtmlText] ([ModuleID], [DesktopHtml], [MobileSummary], [MobileDetails]) VALUES (7, N'&lt;span class=&quot;Normal&quot;&gt;
&lt;img align=&quot;right&quot; hspace=&quot;0&quot; src=&quot;data/hart.gif&quot;&gt;
&lt;p&gt;&lt;b&gt;Nancy Hart&lt;/b&gt; served as a scout, guide and spy for the Confederate army, carrying messages between the Southern Armies. She hung around isolated Federal outposts, acting as a peddlar, to report their strength, population and vulnerability to General Jackson. Nancy was twenty years old when she was captured by the Yankees and jailed in a dilapidated house with guards constantly patrolling the building. Nancy gained the trust of one of her guards, got his weapon from him, shot him and escaped.&lt;/p&gt;
&lt;/span&gt;
', N'', N'')
INSERT [Portal_HtmlText] ([ModuleID], [DesktopHtml], [MobileSummary], [MobileDetails]) VALUES (11, N'&lt;table cellspacing=&quot;0&quot; cellpadding=&quot;0&quot; border=&quot;0&quot;&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot;&gt;
            &lt;img src=&quot;data/enigma.gif&quot;&gt;
        &lt;/td&gt;
    &lt;/tr&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot;&gt;
            &lt;br&gt;The WWII &lt;b&gt;Enigma&lt;/b&gt; cypher was based on a system of three rotors that substituted cipher text letters for plain text letters. The innovation that made Enigma machine so powerful was the spinning of its rotors. As the plain text letter passed through the first rotor, the first rotor would rotate one position. The other two rotors would remain stationary until the first rotor had rotated 26 times. Then the second rotor would rotate one position. After the second rotor had rotated 26 times, the third rotor would rotate one position.  As a result, an ''s'' could be encoded as a ''b'' in the first part of the message, and then as an ''m'' later in the same message.  
        &lt;/td&gt;
    &lt;/tr&gt;
&lt;/table&gt;
', N'', N'')
INSERT [Portal_HtmlText] ([ModuleID], [DesktopHtml], [MobileSummary], [MobileDetails]) VALUES (17, N'&lt;span class=&quot;Normal&quot;&gt;
    &lt;img align=&quot;right&quot; hspace=&quot;0&quot; src=&quot;data/vanlew.gif&quot;&gt;
    &lt;p&gt;
        &lt;b&gt;Elizabeth Van Lew&lt;/b&gt; asked to be allowed to visit Union prisoners held by the Confederates in Richmond and began taking them food and medicines. She realized that many of the prisoners had been marched through Confederate lines on their way to Richmond and were full of useful information about Confederate movements. She became a spy for the North for the next four years, setting up a network of couriers, and devising a code. For her efforts during the Civil War, Elizabeth Van Lew was made Postmaster of Richmond by General Grant. 
    &lt;/p&gt;
&lt;/span&gt;
', N'', N'')
INSERT [Portal_HtmlText] ([ModuleID], [DesktopHtml], [MobileSummary], [MobileDetails]) VALUES (22, N'&lt;table cellspacing=0 cellpadding=0 border=0&gt;
    &lt;tr&gt;
        &lt;td class=Normal width=&quot;100%&quot;&gt;
            The &lt;b&gt;ASP.NET Portal Starter Kit&lt;/b&gt; demonstrates how you can use ASP.NET and the .NET Framework to build either an intranet or Internet portal application. The &lt;b&gt;Portal Starter Kit&lt;/b&gt; offers all the functionality of typical portal applications, including:&lt;br&gt;&lt;br&gt;

            &lt;table cellspacing=0 cellpadding=0 border=0&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td width=&quot;140&quot;&gt;
                        &lt;image src=&quot;data/sample.gif&quot;&gt;
                    &lt;/td&gt;
                    &lt;td&gt;&amp;nbsp;&lt;/td&gt;
                    &lt;td class=&quot;Normal&quot; width=&quot;*&quot;&gt;

                        &lt;ul&gt;
                            &lt;li&gt;
                            &lt;a href=&quot;#basicmod&quot;&gt;10 basic portal modules&lt;/a&gt; for common types of content
                            &lt;li&gt;
                            &lt;a href=&quot;#custommod&quot;&gt;Custom portal modules&lt;/a&gt; based on a &quot;pluggable&quot; framework that is simple to extend
                            &lt;li&gt;
                            &lt;a href=&quot;#admintool&quot;&gt;Online administration&lt;/a&gt; of portal layout, content and security
                            &lt;li&gt;
                            &lt;a href=&quot;#security&quot;&gt;Roles-based security&lt;/a&gt; for viewing content, editing content, and administering 
                            the portal
                            &lt;/li&gt;
                        &lt;/ul&gt;


                        All code contained in the Portal Starter Kit download package is free for use
                in your own applications.  But if you prefer, you may customize the portal for your own use without writing a line of code.  The portal includes built-in Administration pages for setting up your portal, adding content, and setting security options.&lt;br&gt;
                    &lt;/td&gt;
                &lt;/tr&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td colspan=3 class=&quot;Normal&quot;&gt;
                        &lt;br&gt;
                        &lt;u&gt;Getting Started with the Portal&lt;/u&gt;&lt;br&gt;
                        This page explains how users interact with the portal, and how to use the Administration tool to customize it.  To browse the source code and read about it works, click the &lt;a href=&quot;Docs/Docs.htm&quot; target=&quot;_new&quot;&gt;Portal Documentation&lt;/a&gt; link at the top of the page. 
                    &lt;/td&gt;
                &lt;/tr&gt;
        &lt;/table&gt;    
            
        &lt;/td&gt;
    &lt;/tr&gt;
&lt;/table&gt;', N'The &lt;b&gt;ASP.NET Portal Starter Kit&lt;/b&gt; demonstrates how you can use ASP.NET and the .NET Framework to build a either an intranet or Internet portal application.', N'')
INSERT [Portal_HtmlText] ([ModuleID], [DesktopHtml], [MobileSummary], [MobileDetails]) VALUES (23, N'&lt;a name=&quot;tabs&quot;&gt;
&lt;table cellspacing=0 cellpadding=0 border=0&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot; width=&quot;100%&quot;&gt;

            Content in the portal is grouped by &lt;b&gt;Tabs&lt;/b&gt;.  For example, the portal has five content tabs:&lt;br&gt;&lt;br&gt;
            
        &lt;/td&gt;
    &lt;/tr&gt;
    &lt;tr&gt;
        &lt;td&gt;
            &lt;img src=&quot;data/tabbar.gif&quot;&gt;    
        &lt;/td&gt;
    &lt;/tr&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot;&gt;

            You can create tabs that are visible only to certain users.  For example, you might create a private tab that only users in the &quot;Managers&quot; role can view.  See &lt;a href=&quot;#layout&quot;&gt;Managing Portal Layout&lt;/a&gt; to learn how to create a tab, and &lt;a href=&quot;#security&quot;&gt;Managing User Security&lt;/a&gt; to learn how to control access to a tab.
        
        &lt;/td&gt;
    &lt;/tr&gt;
&lt;/table&gt;
', N'', N'')
INSERT [Portal_HtmlText] ([ModuleID], [DesktopHtml], [MobileSummary], [MobileDetails]) VALUES (24, N'&lt;a name=&quot;modules&quot;&gt;
&lt;table cellspacing=0 cellpadding=5 border=0&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot; width=&quot;100%&quot;&gt;

            Portal Modules are modular pieces of code and UI that each present some functionality to the user, like a threaded discussion list, or render data, graphics and text, like a &quot;sales by region&quot; report.  Typically, several portal modules are grouped on a portal tab.  For example, the Home tab of the Portal has seven modules:&lt;br&gt;
        &lt;/td&gt;
    &lt;/tr&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot;&gt;
            &lt;img align=&quot;left&quot; src=&quot;data/whataremodules.gif&quot;&gt;
        &lt;/td&gt;
    &lt;/tr&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot;&gt;
            When a user browses a tab in the portal, the portal framework reads a description of the tab from it''s configuration file, and automatically assembles a page from the portal modules associated with the tab.  The Home tab is composed from these modules:
            
            &lt;ol&gt;
            &lt;li&gt;&lt;u&gt;Sign-in module&lt;/u&gt;:  the portal framework inserts this module on the first tab automatically if the user is not yet authenticated.
            &lt;li&gt;&lt;u&gt;QuickLinks module&lt;/u&gt;:  a list of ASP.NET links rendered compactly.
            &lt;li&gt;&lt;u&gt;Html/Text module&lt;/u&gt;:  an Html snippet, including an image, that introduces the Portal Starter Kit.  An alternate, text-only version is supplied to Mobile users.
            &lt;li&gt;&lt;u&gt;Announcements module&lt;/u&gt;:  a list of IBuySpy.com news items, briefly summarized, with links for more information.
            &lt;li&gt;&lt;u&gt;Events module&lt;/u&gt;:  a list of upcoming IBuySpy.com events, including time, location and a brief description.
            &lt;li&gt;&lt;u&gt;Another Html/Text module&lt;/u&gt;:  an Html snippet, including an image, that describes this week''s special on IBuySpy.com.
            &lt;li&gt;&lt;u&gt;XML module&lt;/u&gt;:  the results of an XSL/T transform on an XML file that shows recent revenue trends for IBuySpy.com.
            &lt;/ol&gt;
            
            &lt;a name=&quot;basicmod&quot;&gt;
            &lt;u&gt;Built-In Portal Modules&lt;/u&gt;
            &lt;br&gt;
            You can use multiple instances of a module type in the portal, for example an HR &lt;i&gt;Links&lt;/i&gt; module and a Products &lt;i&gt;Links&lt;/i&gt; module.  The Portal Starter Kit provides 10 basic Desktop module types, listed below.  Four of these--Announcements, Contacts, Events and HTML/Text--support an alternate rendering for Mobile devices.&lt;br&gt;&lt;br&gt;

            &lt;table cellpadding=&quot;5&quot; cellspacing=&quot;0&quot; border=&quot;0&quot;&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td class=&quot;Normal&quot;&gt;
                        &lt;hr&gt;&lt;br&gt;
                        &lt;img align=&quot;right&quot; hspace=&quot;10&quot; vspace=&quot;10&quot; src=&quot;data/m_ann.gif&quot;&gt;

                        &lt;b&gt;Announcements&lt;/b&gt;&lt;br&gt;
                        This module renders a list of announcements. Each announcement includes title, text and a &quot;read more&quot; link, and can be set to automatically expire after a particular date.  Announcements includes an edit page, which allows authorized users to edit the data stored in the SQL database.
                    &lt;/td&gt;    
                &lt;/tr&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td class=&quot;Normal&quot;&gt;
                        &lt;hr&gt;&lt;br&gt;
                        &lt;img align=&quot;right&quot; hspace=&quot;10&quot; vspace=&quot;10&quot; src=&quot;data/m_con.gif&quot;&gt;

                        &lt;b&gt;Contacts&lt;/b&gt;&lt;br&gt;
                        This module renders contact information for a group of people, for example a project team.  The Mobile version of this module also provides a Call link to phone a contact when the module is browsed from a wireless telephone.  Contacts includes an edit page, which allows authorized users to edit the Contacts data stored in the SQL database.
                    &lt;/td&gt;    
                &lt;/tr&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td class=&quot;Normal&quot;&gt;
                        &lt;hr&gt;&lt;br&gt;
                        &lt;img align=&quot;right&quot; hspace=&quot;10&quot; vspace=&quot;10&quot; src=&quot;data/m_dsc.gif&quot;&gt;

                        &lt;b&gt;Discussion&lt;/b&gt;&lt;br&gt;
                        This module renders a group of message threads on a specific topic.  Discussion includes a Read/Reply Message page, which allows authorized users to reply to exising messages or add a new message thread.  The data for Discussion is stored in the SQL database.
                    &lt;/td&gt;    
                &lt;/tr&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td class=&quot;Normal&quot;&gt;
                        &lt;hr&gt;&lt;br&gt;
                        &lt;img align=&quot;right&quot; hspace=&quot;10&quot; vspace=&quot;10&quot; src=&quot;data/m_doc.gif&quot;&gt;

                        &lt;b&gt;Documents&lt;/b&gt;&lt;br&gt;
                        This module renders a list of documents, including links to browse or download the document.  Documents includes an edit page, which allows authorized users to edit the information about the Documents (for example, a friendly title) stored in the SQL database.  The document itself may be linked to via URL or uploaded and stored in the SQL database.
                    &lt;/td&gt;    
                &lt;/tr&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td class=&quot;Normal&quot;&gt;
                        &lt;hr&gt;&lt;br&gt;
                        &lt;img align=&quot;right&quot; hspace=&quot;10&quot; vspace=&quot;10&quot; src=&quot;data/m_evt.gif&quot;&gt;

                        &lt;b&gt;Events&lt;/b&gt;&lt;br&gt;
                        This module renders a list of upcoming events, including time and location.  Individual events can be set to automatically expire from the list after a particular date.  Events includes an edit page, which allows authorized users to edit the Events data stored in the SQL database.
                    &lt;/td&gt;    
                &lt;/tr&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td class=&quot;Normal&quot;&gt;
                        &lt;hr&gt;&lt;br&gt;
                        &lt;img align=&quot;right&quot; hspace=&quot;10&quot; vspace=&quot;10&quot; src=&quot;data/m_htm.gif&quot;&gt;

                        &lt;b&gt;Html/Text&lt;/b&gt;&lt;br&gt;
                        This module renders a snippet of HTML or text.  The Html/Text module includes an edit page, which allows authorized users to the HTML or text snippets directly.  The snippets are stored in the SQL database.
                    &lt;/td&gt;    
                &lt;/tr&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td class=&quot;Normal&quot;&gt;
                        &lt;hr&gt;&lt;br&gt;
                        &lt;img align=&quot;right&quot; hspace=&quot;10&quot; vspace=&quot;10&quot; src=&quot;data/m_img.gif&quot;&gt;

                        &lt;b&gt;Image&lt;/b&gt;&lt;br&gt;
                        This module renders an image using an HTML IMG tag.  The module simply sets the IMG tag''s src attribute to a relative or absolute URL, so the image file does not need to reside within the portal.  The module also exposes height and width attributes, which permits you to scale the image.  Image includes an edit page, which persists these settings to the portal''s configuration file.                        
                    &lt;/td&gt;    
                &lt;/tr&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td class=&quot;Normal&quot;&gt;
                        &lt;hr&gt;&lt;br&gt;
                        &lt;img align=&quot;right&quot; hspace=&quot;10&quot; vspace=&quot;10&quot; src=&quot;data/m_lnk.gif&quot;&gt;

                        &lt;b&gt;Links&lt;/b&gt;&lt;br&gt;
                        This module renders a list of hyperlinks.  Links includes an edit page, which allows authorized users to edit the Links data stored in the SQL database.
                    &lt;/td&gt;    
                &lt;/tr&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td class=&quot;Normal&quot;&gt;
                        &lt;hr&gt;&lt;br&gt;
                        &lt;img align=&quot;right&quot; hspace=&quot;10&quot; vspace=&quot;10&quot; src=&quot;data/m_ql.gif&quot;&gt;

                        &lt;b&gt;QuickLinks&lt;/b&gt;&lt;br&gt;
                        Like Links, this module renders a list of hyperlinks.  Rather than rendering it''s title, however, QuickLinks shows the title &quot;Quick Launch.&quot;  It''s compact rendering and generic title make it ideal for a set of ''global'' links that appears on several tabs in the portal.  QuickLinks shares the Links edit page, which allows authorized users to edit the QuickLinks data stored in the SQL database.
                    &lt;/td&gt;    
                &lt;/tr&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td class=&quot;Normal&quot;&gt;
                        &lt;hr&gt;&lt;br&gt;
                        &lt;img align=&quot;right&quot; hspace=&quot;10&quot; vspace=&quot;10&quot; src=&quot;data/m_xml.gif&quot;&gt;

                        &lt;b&gt;Xml/Xsl&lt;/b&gt;&lt;br&gt;
                        This module renders the result of an XML/XSL transform.  The XML and XSL files are identified by their UNC paths in the xmlsrc and xslsrc properties of the module.  The Xml/Xsl module includes an edit page, which persists these settings to the SQL database.
                    &lt;/td&gt;    
                &lt;/tr&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td class=&quot;Normal&quot;&gt;
                        &lt;hr&gt;&lt;br&gt;
                    &lt;/td&gt;    
                &lt;/tr&gt;
            &lt;/table&gt;
            
            &lt;a name=&quot;custommod&quot;&gt;
            &lt;u&gt;Custom Portal Modules&lt;/u&gt;
            &lt;br&gt;
            You can create your own custom modules and add them to the portal framework.  See the &lt;a href=&quot;docs/docs.htm&quot;&gt;Portal Documentation&lt;/a&gt; for more information about how to create a custom module.&lt;br&gt;&lt;br&gt;
            See &lt;a href=&quot;#layout&quot;&gt;Managing Portal Layout&lt;/a&gt; below to learn about how to add your custom modules to the portal administration system.
            
        &lt;/td&gt;
    &lt;/tr&gt;
&lt;/table&gt;', N'', N'')
INSERT [Portal_HtmlText] ([ModuleID], [DesktopHtml], [MobileSummary], [MobileDetails]) VALUES (25, N'&lt;table cellspacing=0 cellpadding=5 border=0&gt;
    &lt;tr&gt;
        &lt;td&gt;
            &lt;table cellspacing=0 cellpadding=0 border=0&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td width=&quot;*&quot; class=&quot;Normal&quot;&gt;
                    
                        &lt;a name=&quot;admintool&quot;&gt;
                        &lt;u&gt;Using the Admin Tool&lt;/u&gt;&lt;br&gt;
                        The Portal Starter Kit provides an online Admin tool that authenticated users in the &quot;Admins&quot; role can use to set up the layout, content and security of the portal.&lt;br&gt;&lt;br&gt;

                        You must sign in on the Home tab to use the Admin tool.  If you''ve never signed in before, you''ll need to add yourself to the portal database using the &quot;register&quot; button.  After signing in, you''ll see a new tab called &quot;Admin&quot; at the top of the page.  Click it to go to the Admin tool.&lt;br&gt;&lt;br&gt;
        
                        
                        &lt;a name=&quot;sitesettings&quot;&gt;
                        &lt;u&gt;Site Settings&lt;/u&gt;&lt;br&gt;&lt;br&gt;
                        &lt;table cellspacing=0 cellpadding=0 border=0&gt;
                            &lt;tr&gt;
                                &lt;td width=&quot;235&quot;&gt;
                                    &lt;img src=&quot;data/sitesettings.gif&quot;&gt;
                                &lt;/td&gt;
                            &lt;/tr&gt;
                            &lt;tr&gt;
                                &lt;td class=&quot;Normal&quot;&gt;
                                    &lt;br&gt;
                                    The &lt;b&gt;Site Settings&lt;/b&gt; section of the Admin tool lets you to set the portal''s title, and whether to show edit links to all users.  When changing one of these settings, by sure to click the Apply Changes button at the bottom of the section.
                                &lt;/td&gt;
                            &lt;/tr&gt;
                       &lt;/table&gt;    

                    &lt;/td&gt;
                    &lt;td&gt;&amp;nbsp;&amp;nbsp;&lt;/td&gt;
                    &lt;td width=&quot;420&quot;&gt;
                        &lt;img src=&quot;data/admin.gif&quot;&gt;
                    &lt;/td&gt;
                &lt;/tr&gt;
            &lt;/table&gt;    
        &lt;/td&gt;
    &lt;/tr&gt;
&lt;/table&gt;
', N'', N'')
INSERT [Portal_HtmlText] ([ModuleID], [DesktopHtml], [MobileSummary], [MobileDetails]) VALUES (26, N'&lt;table cellspacing=0 cellpadding=5 border=0&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot;&gt;
            &lt;a name=&quot;layout&quot;&gt;
            &lt;b&gt;Note:&lt;/b&gt; Portal layout is managed using the &lt;a href=&quot;#admintool&quot;&gt;Admin tool&lt;/a&gt; described above.&lt;br&gt;&lt;br&gt;
        &lt;/td&gt;
    &lt;/tr&gt;
    &lt;tr&gt;
        &lt;td&gt;
            &lt;table cellspacing=0 cellpadding=0 border=0&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td width=&quot;235&quot; rowspan=&quot;2&quot;&gt;
                        &lt;img align=&quot;left&quot; src=&quot;data/tabs.gif&quot;&gt;
                    &lt;/td&gt;
                    &lt;td&gt;&amp;nbsp;&amp;nbsp;&lt;/td&gt;
                    &lt;td class=&quot;Normal&quot;&gt;
                        
                        &lt;u&gt;Working with Tabs&lt;/u&gt;&lt;br&gt;
                        The &lt;b&gt;Tabs&lt;/b&gt; section lets you add and remove tabs, and change the order of the tabs.  
                        &lt;ul&gt;
                        &lt;li&gt;To &lt;b&gt;add&lt;/b&gt; a new tab to the portal, click the &quot;Add new tab&quot; link (1).  
                        &lt;li&gt;To &lt;b&gt;modify&lt;/b&gt; an existing tab, first select the tab to modify (2) then click the pencil button (3).
                        &lt;li&gt;To &lt;b&gt;reorder&lt;/b&gt; the tabs, click the tab name (2), then click the up or down button (3).
                        &lt;li&gt;To &lt;b&gt;delete&lt;/b&gt; a tab, click the tab name (2), then click the X button (3).
                        &lt;/ul&gt;
                    &lt;/td&gt;
                &lt;/tr&gt;
           &lt;/table&gt;    
        &lt;/td&gt;
    &lt;/tr&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot;&gt;
            &lt;table cellspacing=0 cellpadding=0 border=0&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td width=&quot;420&quot; rowspan=&quot;2&quot;&gt;
                        &lt;img align=&quot;left&quot; src=&quot;data/layout.gif&quot;&gt;
                    &lt;/td&gt;
                    &lt;td&gt;&amp;nbsp;&amp;nbsp;&lt;/td&gt;
                    &lt;td width=&quot;*&quot; class=&quot;Normal&quot;&gt;
                    
                        &lt;a name=&quot;workmodules&quot;&gt;&lt;/a&gt;
                        &lt;u&gt;Working With Modules on a Tab&lt;/u&gt;&lt;br&gt;
                        The &lt;b&gt;Tab Name and Layout&lt;/b&gt; page lets you manipulate the modules for the selected tab.  Use this page to set the tab name and which roles may view the tab.  Optionally, you can use this page to make the tab visible to Mobile users, and set a different (often abbreviated) tab name for mobile viewing (1).&lt;br&gt;&lt;br&gt;
                        
                        To open the Tab Name and Layout page, select the tab you wish to modify in the Tabs section of the Admin page, then click the pencil button.  See &lt;a href=&quot;#layout&quot;&gt;Portal Layout&lt;/a&gt; above.
                        &lt;ul&gt;
                        &lt;li&gt;To &lt;b&gt;add a new module&lt;/b&gt; to the Tab, pick a Module Type from the list, and give your module a name (2).  Then click the &quot;Add to Organize Modules below&quot; link (3).  The module is added to the bottom of the center Content Pane (4).
                        &lt;li&gt;To &lt;b&gt;add an existing module&lt;/b&gt; to this Tab, click the Exising Module radio button (2).  Pick the module you wish by name from the Module Type list (2).  Then click the &quot;Add to Organize Modules below&quot; link (3).
                        &lt;li&gt;To &lt;b&gt;move a module&lt;/b&gt; within the tab, first select the module to move (4) then click the up, down, right or left button (4).
                        &lt;li&gt;To &lt;b&gt;delete a module&lt;/b&gt; from this tab, click the module name (4), then click the X button (4).
                        &lt;li&gt;To &lt;b&gt;change a module''s name&lt;/b&gt;, set it''s caching timeout or control which roles may modify the first select the tab to modify it''s data click the module name (4), then click the pencil button (4).
                        &lt;/ul&gt;
                    &lt;/td&gt;
                &lt;/tr&gt;
            &lt;/table&gt;    
            
        &lt;/td&gt;
    &lt;/tr&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot;&gt;
            &lt;table cellspacing=0 cellpadding=0 border=0&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td width=&quot;235&quot; rowspan=&quot;2&quot;&gt;
                        &lt;img align=&quot;left&quot; src=&quot;data/modulesettings.gif&quot;&gt;
                    &lt;/td&gt;
                    &lt;td&gt;&amp;nbsp;&amp;nbsp;&lt;/td&gt;
                    &lt;td width=&quot;*&quot; class=&quot;Normal&quot;&gt;
                    
                        &lt;a name=&quot;modulesettings&quot;&gt;&lt;/a&gt;
                        &lt;u&gt;Modules Settings&lt;/u&gt;&lt;br&gt;
                        The &lt;b&gt;Modules Settings&lt;/b&gt; page lets you change a module''s name, set it''s cache timeout, set edit permissions for the module''s data, and indicate whether the module should be visible to mobile users.  Click the Apply Modules Changes button to save the changes.&lt;br&gt;&lt;br&gt;
                        
                        To open the Module Settings page, select the module you wish to modify in the Tab Name and Layout page, then click the pencil button.  See &lt;a href=&quot;#workmodules&quot;&gt;Working with Modules on a Tab&lt;/a&gt; above.&lt;br&gt;&lt;br&gt;
                        
                        For information about setting edit permissions, see &lt;a href=&quot;#authorization&quot;&gt;Roles-Based Authorization&lt;/a&gt; below.
                    &lt;/td&gt;
                &lt;/tr&gt;
            &lt;/table&gt;    
        &lt;/td&gt;
    &lt;/tr&gt;
        &lt;tr&gt;
        &lt;td class=&quot;Normal&quot;&gt;
            &lt;table cellspacing=0 cellpadding=0 border=0&gt;
                &lt;tr valign=&quot;middle&quot;&gt;
                    &lt;td&gt;
                        &lt;img align=&quot;bottom&quot; src=&quot;data/moduledefs.gif&quot;&gt;
                    &lt;/td&gt;
                    &lt;td&gt;&amp;nbsp;&amp;nbsp;&lt;/td&gt;
                    &lt;td width=&quot;*&quot; class=&quot;Normal&quot;&gt;
                                    
                        &lt;a name=&quot;moduledefs&quot;&gt;&lt;/a&gt;
                        &lt;u&gt;Defining New Module Types&lt;/u&gt;&lt;br&gt;
                            The &lt;b&gt;Module Definitions&lt;/b&gt; section lets you add or change a module type definition.  To modify an existing definition, click the pencil button next to the definition name.  To add a new definition click the Add New Module Type button.&lt;br&gt;&lt;br&gt;
                            
                            On the &lt;b&gt;Module Type Definition&lt;/b&gt; page, set a Friendly name and path to the Desktop module source file.  If applicable, set a path to the Mobile version of the module as well.  Due to the ASP.NET security restrictions, the module files must be located within the portal''s application directory or subdirectories.&lt;br&gt;&lt;br&gt;&lt;br&gt;&lt;br&gt;
                        &lt;img src=&quot;data/moduletypedef.gif&quot;&gt;
                    &lt;/td&gt;
                &lt;/tr&gt;
            &lt;/table&gt;    
        &lt;/td&gt;
    &lt;/tr&gt;
&lt;/table&gt;', N'', N'')
INSERT [Portal_HtmlText] ([ModuleID], [DesktopHtml], [MobileSummary], [MobileDetails]) VALUES (27, N'&lt;table cellspacing=0 cellpadding=5 border=0&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot;&gt;
        
            &lt;a name=&quot;security&quot;&gt;
            Portal security is managed using the &lt;a href=&quot;#admintool&quot;&gt;Admin tool&lt;/a&gt; described above.&lt;br&gt;&lt;br&gt;
        
            &lt;a name=&quot;authentication&quot;&gt;
            &lt;u&gt;Authentication&lt;/u&gt;&lt;br&gt;

            &lt;i&gt;Authentication&lt;/i&gt; validates a user''s crenditials.  The Portal Starter Kit sample supports two forms of authentication.  
            
            &lt;ul&gt;
            &lt;li&gt;&lt;b&gt;Forms-Based/Cookie authentication&lt;/b&gt; collects a user name and password in a simple input form, then validates them against the Users table in the database.  This type of authentication is typically used for Internet and extranet portals.

            &lt;li&gt;With &lt;b&gt;Windows/NTLM authentication&lt;/b&gt;, either the Windows SAM or Active Directory is used to store and validate all username/password credentials.  This type of authentication is typically used  for intranet-based portals.
           
            &lt;/ul&gt;
            When you install the ASP.NET Portal Starter Kit, Forms Authentication is enabled by default.  To change the authentication mode, edit the web.config file in the root portal directory:&lt;/td&gt;
    &lt;/tr&gt;
    &lt;tr&gt;
        &lt;td&gt;
            &lt;img align=&quot;left&quot; src=&quot;data/config_auth_win.gif&quot;&gt;
            &lt;img align=&quot;left&quot; src=&quot;data/config_auth_forms.gif&quot;&gt;
        &lt;/td&gt;
    &lt;/tr&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot;&gt;
        
            &lt;a name=&quot;authorization&quot;&gt;
            &lt;u&gt;Roles-Based Authorization&lt;/u&gt;&lt;br&gt;

            &lt;i&gt;Authorization&lt;/i&gt; is used to control access to the modules and tabs in the portal, including the Admin tab.  The ASP.NET Portal Starter Kit uses roles-based authorization.  The portal administrator uses these steps to set up roles-based authorization:
            
            &lt;ol&gt;
            &lt;li&gt;&lt;a href=&quot;#createrole&quot;&gt;&lt;b&gt;Create a role&lt;/b&gt;&lt;/a&gt;, for example &quot;Managers&quot; or &quot;HR&quot;.
            &lt;li&gt;&lt;a href=&quot;#addtorole&quot;&gt;&lt;b&gt;Add users to the role&lt;/b&gt;&lt;/a&gt;, for example &quot;CORP01\andreabr&quot;, &quot;CORP01\tomka&quot;, and &quot;CORP01\marklo&quot;.
            &lt;li&gt;&lt;a href=&quot;#tabperms&quot;&gt;&lt;b&gt;Set view permission for tabs&lt;/b&gt;&lt;/a&gt;, for example, limit viewing of the &quot;FY01 Budget&quot; tab to users in the &quot;Managers&quot; role.
            &lt;li&gt;&lt;a href=&quot;#editperms&quot;&gt;&lt;b&gt;Set edit permission for modules&lt;/b&gt;&lt;/a&gt;, for example, limit permission to edit information in the &quot;HR/Benefits News&quot; module to users in the &quot;HR&quot; role.
            &lt;/ol&gt;
            
            The roles-based authorization system in the Portal Starter Kit works independently of the authentication mode.  Role membership data is stored in the portal''s configuration system, and does not rely on the ASP.NET configuration system or Windows groups.  

            &lt;ul&gt;&lt;li&gt;
            &lt;b&gt;IMPORTANT NOTE&lt;/b&gt;: &lt;i&gt;The &quot;All Users&quot; member is a special value that, if present, adds all authenticated users to the role.  When you first install the Portal Starter Kit, the &quot;Admins&quot; and &quot;Power Users&quot; roles contain the All Users member.  Remove this member to make the these role secure.&lt;/i&gt;
            &lt;/li&gt;&lt;/ul&gt;

        &lt;/td&gt;
    &lt;/tr&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot;&gt;
            &lt;a name=&quot;createrole&quot;&gt;
            &lt;u&gt;Creating and Managing Roles&lt;/u&gt;&lt;br&gt;
        
            &lt;table cellspacing=0 cellpadding=0 border=0&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td class=&quot;Normal&quot; width=&quot;*&quot;&gt;
                        The &lt;b&gt;Security Roles&lt;/b&gt; section of the Admin tab lets you define roles for the portal.  
                        &lt;ul&gt;
                        &lt;li&gt;To &lt;b&gt;create&lt;/b&gt; a new role to the portal, click the &quot;Add new role&quot; link.  
                        &lt;li&gt;To &lt;b&gt;edit&lt;/b&gt; an existing role, click the pencil button next to the role name.  See &lt;a href=&quot;#addtorole&quot;&gt;Adding Users to a Role&lt;/a&gt; below.
                        &lt;li&gt;To &lt;b&gt;delete&lt;/b&gt; a role, click the X button next to the role name.
                        &lt;/ul&gt;
                    &lt;/td&gt;
                    &lt;td&gt;&amp;nbsp;&amp;nbsp;&lt;/td&gt;
                    &lt;td width=&quot;320&quot;&gt;
                        &lt;img src=&quot;data/roles1.gif&quot;&gt;
                        &lt;br&gt;
                        &lt;img align=&quot;left&quot; src=&quot;data/roles.gif&quot;&gt;
                    &lt;/td&gt;
                &lt;/tr&gt;
           &lt;/table&gt;    
        &lt;/td&gt;
    &lt;/tr&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot;&gt;
            &lt;a name=&quot;addtorole&quot;&gt;
            &lt;u&gt;Adding Users to a Role&lt;/u&gt;&lt;br&gt;

            &lt;table cellspacing=0 cellpadding=0 border=0&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td width=&quot;*&quot; class=&quot;Normal&quot;&gt;
                        The &lt;b&gt;Role Membership&lt;/b&gt; page lets you manage the add or delete users for the selected role.&lt;br&gt;&lt;br&gt;
                        
                        To open the Role Name and Membership page, select the role you wish to modify in the Security Roles section of the Admin page, then click the Change Role Members button.  See &lt;a href=&quot;#createrole&quot;&gt;Creating and Managing Roles&lt;/a&gt; above.
                        &lt;ul&gt;
                        &lt;li&gt;To &lt;b&gt;add&lt;/b&gt; a member to the role, select the user name from the dropdown and click the &quot;Add to Role&quot; link.
                        &lt;li&gt;To &lt;b&gt;delete&lt;/b&gt; a member from the role,  click the X button to the left of the member name.
                        &lt;/ul&gt;
                    &lt;/td&gt;
                    &lt;td&gt;&amp;nbsp;&amp;nbsp;&lt;/td&gt;
                    &lt;td width=&quot;320&quot;&gt;
                        &lt;img align=&quot;left&quot; src=&quot;data/rolemembership.gif&quot;&gt;
                    &lt;/td&gt;
                &lt;/tr&gt;
            &lt;/table&gt;  

        &lt;/td&gt;
    &lt;/tr&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot;&gt;
        
            &lt;a name=&quot;tabperms&quot;&gt;&lt;/a&gt;
            &lt;u&gt;Setting View Permission for a Tab&lt;/u&gt;&lt;br&gt;

            You can show show or hide an entire tab depending on whether a user is in an authorized role.  For example, you can limit viewing of the &quot;FY01 Budget&quot; tab to users in the &quot;Managers&quot; role. 
              
            &lt;br&gt;&lt;br&gt;
            
            &lt;table cellspacing=0 cellpadding=0 border=0&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td width=&quot;220&quot;&gt;
                        &lt;img src=&quot;data/layout.gif&quot;&gt;
                    &lt;/td&gt;
                    &lt;td&gt;&amp;nbsp;&amp;nbsp;&lt;/td&gt;
                    &lt;td width=&quot;*&quot; class=&quot;Normal&quot;&gt;
                        &lt;ul&gt;
                        &lt;li&gt;To set which security roles may view a tab, go to the &lt;a href=&quot;#workmodules&quot;&gt;Tab Name and Layout&lt;/a&gt; page for the tab.  Check the desired roles in the &quot;Authorized Roles&quot; section, then click the &quot;Save Tab Changes&quot; button.
                        &lt;/ul&gt;
                    &lt;/td&gt;
                &lt;/tr&gt;
            &lt;/table&gt;  

        &lt;/td&gt;
    &lt;/tr&gt;
    &lt;tr&gt;
        &lt;td class=&quot;Normal&quot;&gt;
        
            &lt;a name=&quot;editperms&quot;&gt;&lt;/a&gt;
            &lt;u&gt;Setting Edit Access for a Module&lt;/u&gt;&lt;br&gt;

            Permission to edit module data is granted by security role on a per-module basis.  
            
            &lt;ul&gt;
            &lt;li&gt;To set the roles that may edit data for a specific module, go to the &lt;a href=&quot;#modulesettings&quot;&gt;Module Settings&lt;/a&gt; page for the module.  Check the desired roles in the &quot;Roles that can edit content&quot; section, then click the &quot;Apply Module Changes&quot; button.
            &lt;/ul&gt;
            
            &lt;table cellspacing=0 cellpadding=0 border=0&gt;
                &lt;tr valign=&quot;top&quot;&gt;
                    &lt;td width=&quot;220&quot;&gt;
                        &lt;img src=&quot;data/modulesettings.gif&quot;&gt;
                    &lt;/td&gt;
                    &lt;td&gt;&amp;nbsp;&amp;nbsp;&lt;/td&gt;
                    &lt;td width=&quot;*&quot; class=&quot;Normal&quot;&gt;
                    
                        Normally, a module''s Edit button is shown only to users who have permission to edit the module''s data.  If you wish, however, you can show the Edit button to all users.  When an unauthorized user clicks the Edit button she recieves an &quot;Edit Access Denied&quot; message, which prompts her to contact the portal administrator to set up edit access.   
                        
                        &lt;ul&gt;
                        &lt;li&gt;This is a portal-wide setting.  To show the Edit button to all users -- even those who do not have edit access -- go to the &lt;b&gt;Site Settings&lt;/b&gt; section on the main Admin page and check the &quot;Always show Edit button&quot; checkbox, then click the &quot;Apply  Changes&quot; button.
                        &lt;/ul&gt;
                    &lt;/td&gt;
                &lt;/tr&gt;
            &lt;/table&gt;  

        &lt;/td&gt;
    &lt;/tr&gt;
&lt;/table&gt;', N'', N'')

/****** Object:  Table [dbo].[PortalCfg_Globals]    Script Date: 03/01/2012 22:46:17 ******/
SET IDENTITY_INSERT [PortalCfg_Globals] ON
INSERT [PortalCfg_Globals] ([PortalId], [PortalName], [AlwaysShowEditButton]) VALUES (1, N'ASP.NET Portal Starter Kit', 0)
SET IDENTITY_INSERT [PortalCfg_Globals] OFF
/* <lang><zh-CN>默认用户种子创建历史演示管理员账号 `admin` 及其旧式口令哈希；该值只用于 Starter Kit 可重建样例环境，生产或共享环境必须通过正式账号初始化流程替换。</zh-CN><en>The default-user seed creates the historical demo administrator account `admin` and its legacy password hash; the value is only for rebuildable Starter Kit sample environments, and production or shared environments must replace it through the formal account-initialization flow.</en></lang> */
/****** Object:  Table [dbo].[Portal_Users]    Script Date: 03/01/2012 22:46:17 ******/
SET IDENTITY_INSERT [Portal_Users] ON
INSERT [Portal_Users] ([UserID], [Name], [Password], [Email]) VALUES (1, N'admin', N'21-23-2F-29-7A-57-A5-A7-43-89-4A-0E-4A-80-1F-C3', N'admin')
SET IDENTITY_INSERT [Portal_Users] OFF
/* <lang><zh-CN>用户角色种子把固定管理员用户绑定到固定 `Admins` 角色；该桥接行必须在用户和角色种子之后写入，以维持旧后台管理页的初始授权入口。</zh-CN><en>The user-role seed binds the fixed administrator user to the fixed `Admins` role; this bridge row must be written after the user and role seeds so the legacy admin pages retain their initial authorization entry point.</en></lang> */
/****** Object:  Table [dbo].[Portal_UserRoles]    Script Date: 03/01/2012 22:46:17 ******/
INSERT [Portal_UserRoles] ([UserID], [RoleID]) VALUES (1, 0)
/* <lang><zh-CN>Tab 种子定义单门户六个固定导航节点及其顺序、移动端显示和访问角色字符串；旧运行时代码直接读取这些主键与分号分隔角色文本，因此这里不重排、不重编号。</zh-CN><en>The tab seed defines the six fixed navigation nodes for the single portal, including ordering, mobile visibility, and role-list strings; the legacy runtime reads these keys and semicolon-delimited role values directly, so this script does not reorder or renumber them.</en></lang> */
/****** Object:  Table [dbo].[PortalCfg_Tabs]    Script Date: 03/01/2012 22:46:17 ******/
SET IDENTITY_INSERT [PortalCfg_Tabs] ON
INSERT [PortalCfg_Tabs] ([TabId], [TabName], [TabOrder], [AccessRoles], [ShowMobile], [MobileTabName], [PortalId]) VALUES (1, N'Home', 1, N'All Users;', 1, N'Home', 1)
INSERT [PortalCfg_Tabs] ([TabId], [TabName], [TabOrder], [AccessRoles], [ShowMobile], [MobileTabName], [PortalId]) VALUES (2, N'Employee Info', 3, N'All Users;', 1, N'HR', 1)
INSERT [PortalCfg_Tabs] ([TabId], [TabName], [TabOrder], [AccessRoles], [ShowMobile], [MobileTabName], [PortalId]) VALUES (3, N'Product Info', 5, N'All Users;', 1, N'Products', 1)
INSERT [PortalCfg_Tabs] ([TabId], [TabName], [TabOrder], [AccessRoles], [ShowMobile], [MobileTabName], [PortalId]) VALUES (4, N'Discussions', 7, N'All Users;', 0, N'Discussions', 1)
INSERT [PortalCfg_Tabs] ([TabId], [TabName], [TabOrder], [AccessRoles], [ShowMobile], [MobileTabName], [PortalId]) VALUES (5, N'About the Portal', 9, N'All Users;', 1, N'About', 1)
INSERT [PortalCfg_Tabs] ([TabId], [TabName], [TabOrder], [AccessRoles], [ShowMobile], [MobileTabName], [PortalId]) VALUES (6, N'Admin', 13, N'Admins;', 0, N'Admin', 1)
SET IDENTITY_INSERT [PortalCfg_Tabs] OFF
/* <lang><zh-CN>模块实例种子把三十二个演示模块固定到 Tab、Pane、模块定义、缓存和编辑角色；这些主键会被内容表和模块设置继续引用，变更需要同步全链路迁移而不能只改一行。</zh-CN><en>The module-instance seed pins thirty-two demo modules to tabs, panes, module definitions, cache policy, and edit roles; these primary keys are referenced by content tables and module settings, so changing them requires a whole-chain migration rather than a single-row edit.</en></lang> */
/****** Object:  Table [dbo].[PortalCfg_Modules]    Script Date: 03/01/2012 22:46:17 ******/
SET IDENTITY_INSERT [PortalCfg_Modules] ON
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (1, N'QuickLinks', 1, N'Admins;', N'LeftPane', 0, 0, 8, 1)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (2, N'Welcome to the Portal Starter Kit', 1, N'Admins;', N'ContentPane', 1, 0, 5, 1)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (3, N'News and Features', 2, N'Admins;', N'ContentPane', 1, 0, 1, 1)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (4, N'Upcoming Events', 3, N'Admins;', N'ContentPane', 1, 0, 4, 1)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (5, N'This Weeks Special', 1, N'Admins;', N'RightPane', 0, 0, 5, 1)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (6, N'Top Movers', 2, N'Admins;', N'RightPane', 0, 0, 9, 1)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (7, N'Spy Diary', 1, N'Admins;', N'LeftPane', 0, 0, 5, 2)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (8, N'HR Benefits', 1, N'Admins;', N'ContentPane', 1, 0, 1, 2)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (9, N'Employee Contact Information', 2, N'Admins;', N'ContentPane', 1, 0, 2, 2)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (10, N'New Employee Documentation', 3, N'Admins;', N'ContentPane', 0, 0, 10, 2)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (11, N'Spy Diary', 1, N'Admins;', N'LeftPane', 0, 0, 5, 3)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (12, N'Competition: TradeCraft', 1, N'Admins;', N'ContentPane', 1, 0, 1, 3)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (13, N'Competition: Surveillance', 2, N'Admins;', N'ContentPane', 1, 0, 1, 3)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (14, N'Competition: Protection', 3, N'Admins;', N'ContentPane', 1, 0, 1, 3)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (15, N'Night Vision Goggles', 1, N'Admins;', N'RightPane', 0, 0, 6, 3)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (16, N'Competitors to Watch', 2, N'Admins;', N'RightPane', 0, 0, 8, 3)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (17, N'Spy Diary', 1, N'Admins;', N'LeftPane', 0, 0, 5, 4)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (18, N'TradeCraft Techniques and Gear', 1, N'Admins;', N'ContentPane', 0, 0, 3, 4)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (19, N'Recipes From the Field', 2, N'Admins;', N'ContentPane', 0, 0, 3, 4)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (20, N'GoodReads', 3, N'Admins;', N'ContentPane', 0, 0, 10, 4)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (21, N'Quick Links', 1, N'Admins;', N'LeftPane', 0, 0, 8, 5)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (22, N'About the Portal Starter Kit', 1, N'Admins;', N'ContentPane', 1, 0, 5, 5)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (23, N'Portal Tabs', 2, N'Admins;', N'ContentPane', 0, 0, 5, 5)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (24, N'Portal Modules', 3, N'Admins', N'ContentPane', 0, 0, 5, 5)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (25, N'Managing the Portal', 4, N'Admins;', N'ContentPane', 0, 0, 5, 5)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (26, N'Managing Portal Layout', 5, N'Admins;', N'ContentPane', 0, 0, 5, 5)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (27, N'Managing User Security', 6, N'Admins;', N'ContentPane', 0, 0, 5, 5)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (28, N'Module Definitions', 1, N'Admins;', N'RightPane', 0, 0, 11, 6)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (29, N'Site Settings', 1, N'Admins;', N'ContentPane', 0, 0, 14, 6)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (30, N'Tabs', 2, N'Admins;', N'ContentPane', 0, 0, 13, 6)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (31, N'Security Roles', 3, N'Admins;', N'ContentPane', 0, 0, 12, 6)
INSERT [PortalCfg_Modules] ([ModuleId], [ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId]) VALUES (32, N'Manage Users', 4, N'Admins;', N'ContentPane', 0, 0, 15, 6)
SET IDENTITY_INSERT [PortalCfg_Modules] OFF

/****** Object:  Table [dbo].[Portal_Events]    Script Date: 03/01/2012 22:46:17 ******/
SET IDENTITY_INSERT [Portal_Events] ON
INSERT [Portal_Events] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [WhereWhen], [Description], [ExpireDate]) VALUES (1, 4, N'JennaJ@ibuyspy.com', CAST(0x0000917A0103F0FC AS DateTime), N'Spy-o-Rama', N'This Saturday, usual secret time and place...', N'It''s back!  The premier regional swap meet for spy paraphernalia of every description.  Shop early for some amazing bargins.', CAST(0x0000B3C400000000 AS DateTime))
INSERT [Portal_Events] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [WhereWhen], [Description], [ExpireDate]) VALUES (2, 4, N'JennaJ@ibuyspy.com', CAST(0x0000917A01047AFC AS DateTime), N'Dark Ops Sock Hop', N'Saturday, 8pm to ?, Dark Ops Cafe', N'Back by popular demand!  Practice your surveillance of the opposite sex, and dance some too.  Great opportunity for a brush pass!', CAST(0x0000B3C400000000 AS DateTime))
SET IDENTITY_INSERT [Portal_Events] OFF
/* <lang><zh-CN>文档种子只写入元数据和 `~/uploads/sample.doc` 路径，二进制 `Content`、`ContentType` 与大小保持 NULL；真实文件可用性由部署包或运行环境单独保证。</zh-CN><en>The document seed writes only metadata and the `~/uploads/sample.doc` path, leaving binary `Content`, `ContentType`, and size as NULL; the deployable package or runtime environment is responsible for making the physical file available.</en></lang> */
/****** Object:  Table [dbo].[Portal_Documents]    Script Date: 03/01/2012 22:46:17 ******/
SET IDENTITY_INSERT [Portal_Documents] ON
INSERT [Portal_Documents] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [FileNameUrl], [FileFriendlyName], [Category], [Content], [ContentType], [ContentSize]) VALUES (1, 10, N'JennaJ@ibuyspy.com', CAST(0x0000917B00CF6025 AS DateTime), N'~/uploads/sample.doc', N'Employee Handbook', N'New Employee Info', NULL, NULL, NULL)
INSERT [Portal_Documents] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [FileNameUrl], [FileFriendlyName], [Category], [Content], [ContentType], [ContentSize]) VALUES (2, 10, N'JennaJ@ibuyspy.com', CAST(0x0000917B00D02A6F AS DateTime), N'~/uploads/sample.doc', N'Annual Reviews', N'New Employee Info', NULL, NULL, NULL)
INSERT [Portal_Documents] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [FileNameUrl], [FileFriendlyName], [Category], [Content], [ContentType], [ContentSize]) VALUES (3, 10, N'JennaJ@ibuyspy.com', CAST(0x0000917B00D082C7 AS DateTime), N'~/uploads/sample.doc', N'Vacation Policy', N'New Employee Info', NULL, NULL, NULL)
INSERT [Portal_Documents] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [FileNameUrl], [FileFriendlyName], [Category], [Content], [ContentType], [ContentSize]) VALUES (4, 20, N'TomVZ@ibuyspy.com', CAST(0x0000917B00CF6025 AS DateTime), N'~/uploads/sample.doc', N'Secret Diary of a Field Operative', N'Dossiers', NULL, NULL, NULL)
INSERT [Portal_Documents] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [FileNameUrl], [FileFriendlyName], [Category], [Content], [ContentType], [ContentSize]) VALUES (5, 20, N'TomVZ@ibuyspy.com', CAST(0x0000917B00D02A6F AS DateTime), N'~/uploads/sample.doc', N'Toaster Boat Users Guide', N'Documentation', NULL, NULL, NULL)
INSERT [Portal_Documents] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [FileNameUrl], [FileFriendlyName], [Category], [Content], [ContentType], [ContentSize]) VALUES (6, 20, N'TomVZ@ibuyspy.com', CAST(0x0000917B00D082C7 AS DateTime), N'~/uploads/sample.doc', N'Mistranslated: Translator Moustache Meets Interpreter Earrings', N'Spy Humor', NULL, NULL, NULL)
INSERT [Portal_Documents] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [FileNameUrl], [FileFriendlyName], [Category], [Content], [ContentType], [ContentSize]) VALUES (7, 20, N'TomVZ@ibuyspy.com', CAST(0x0000917B00CF6025 AS DateTime), N'~/uploads/sample.doc', N'The Edible Tape Recipe Book', N'Documentation', NULL, NULL, NULL)
SET IDENTITY_INSERT [Portal_Documents] OFF
/* <lang><zh-CN>讨论区种子保留旧 Starter Kit 的 `DisplayOrder` 拼接时间线模型：回复行通过连续时间戳字符串表达层级排序，不应被“修正”为单个日期或普通数字排序键。</zh-CN><en>The discussion seed preserves the legacy Starter Kit `DisplayOrder` concatenated-timeline model: reply rows encode hierarchy ordering through chained timestamp strings and must not be “corrected” into single dates or ordinary numeric sort keys.</en></lang> */
/****** Object:  Table [dbo].[Portal_Discussion]    Script Date: 03/01/2012 22:46:17 ******/
SET IDENTITY_INSERT [Portal_Discussion] ON
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (1, 19, N'Edible Tape Puttanesca', CAST(0x0000917B00E99FD0 AS DateTime), N'Had this last night in the Dark Ops Cafe and -- WOW -- is it good!  Red sauce with olives, capers and anchovies.', N'2001-12-20 14:10:36.317', N'MaryK@ibuyspy.com')
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (2, 19, N'Re: Edible Tape Puttanesca', CAST(0x0000917B00EA0114 AS DateTime), N'Their Edible Tape Carbonara is terrific too.  I think they add number two pencil shavings.', N'2001-12-20 14:10:36.3172001-12-20 14:11:59.090', N'JennaJ@ibuyspy.com')
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (3, 19, N'Help - need a Survival Bar recipe', CAST(0x0000917B00EDC45C AS DateTime), N'My wife''s boss is coming for dinner this week and we wanted to serve survival bar.  Anybody have a favorite recipe?', N'2001-12-20 14:25:41.333', N'RajivP@ibuyspy.com')
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (4, 19, N'Re: Help - need a Survival Bar recipe', CAST(0x0000917B00EE18BC AS DateTime), N'I saute it with some garlic and onions in butter and white wine.  When it softens up (about an hour), I finish the dish with a little lemon and pine nuts, and serve over edible tape.  Yum!', N'2001-12-20 14:25:41.3332001-12-20 14:26:53.180', N'ManishG@ibuyspy.com')
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (5, 19, N'Re: Help - need a Survival Bar recipe', CAST(0x0000917B00EE7C58 AS DateTime), N'Survival bar can be pretty chewy, so I throw it in the pressure cooker for about a half hour with onions, carrots, celery and a bay leaf.', N'2001-12-20 14:25:41.3332001-12-20 14:28:18.367', N'TomVZ@ibuyspy.com')
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (6, 19, N'Re: Help - need a Survival Bar recipe', CAST(0x0000917B00EECADC AS DateTime), N'There''s just one thing that can improve a survival bar: Ketchup ;)', N'2001-12-20 14:25:41.3332001-12-20 14:28:18.3672001-12-20 14:29:25.987', N'BrettH@ibuyspy.com')
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (7, 19, N'Eat my shoelaces', CAST(0x0000917B00EF2770 AS DateTime), N'I just tried the new Glow in the Dark Shoelaces and they are *yummy*!  Even better with ketchup...', N'2001-12-20 14:30:44.013', N'BrettH@ibuyspy.com')
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (8, 18, N'What''s the best dead drop mark?', CAST(0x000090210085B8D0 AS DateTime), N'I know this is a total newbie question, but how do I mark a dead drop?  What''s the best mark?', N'2001-01-08 08:06:51.607', N'MaryK@ibuyspy.com')
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (9, 18, N'Re: What''s the best dead drop mark?', CAST(0x0000902100860754 AS DateTime), N'I just use chalk.  It''s fast, writes on just about anything, and doesn''t stick around long enough to get noticed.', N'2001-01-08 08:06:51.6072001-01-08 08:07:59.177', N'JennaJ@ibuyspy.com')
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (10, 18, N'Re: What''s the best dead drop mark?', CAST(0x0000902200881B5C AS DateTime), N'I use chalk too -- it''s really easy to erase.', N'2001-01-08 08:06:51.6072001-01-08 08:07:59.1772001-01-09 08:15:32.970', N'BrettH@ibuyspy.com')
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (11, 18, N'Re: What''s the best dead drop mark?', CAST(0x000090220087F12C AS DateTime), N'There are several things to consider in making your mark: it has to made (and later erased) quickly and discretely, durable enough to stick around until it''s read, easily ignored by passers by, and not missed when it''s gone.  Lots of folks like chalk, but I find it washes away too easily in rainy weather.  Chewing gum (already chewed) works great, but you''ll want to place it well below eye level lest some zealous maintenance worker cleans it off before it has done the job.', N'2001-01-08 08:06:51.6072001-01-09 08:14:57.357', N'TomVZ@ibuyspy.com')
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (12, 18, N'Best night vision gear for nocturnal paint ball?', CAST(0x00009023008B545C AS DateTime), N'We''re going to start playing paint ball at night, and I''m looking for recommendations.  What night vision gear is best for this?', N'2001-01-10 08:27:17.640', N'BrettH@ibuyspy.com')
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (13, 18, N'Re: Best night vision gear for nocturnal paint ball?', CAST(0x00009023008D5478 AS DateTime), N'Well, you definitely want to use the goggle type since you''ll want your hands free (btw, I think you are crazy to play paintball at night).  The Viper 2 (<a href=''http://www.spyworld.com/Viper2.htm''>http://www.spyworld.com/Viper2.htm</a>) is pretty comfortable and not too pricey ($550, vs the thousands you pay for the military versions).  It has a built-in IR illuminator, which is a ''must have'' in your application.  Best of all, it will make you look just like a Borg... :)', N'2001-01-10 08:27:17.6402001-01-10 08:34:34.810', N'TomVZ@ibuyspy.com')
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (14, 18, N'Foreign language terms for ''mole''?', CAST(0x0000902400918F48 AS DateTime), N'Anyone know where I can find this?', N'2001-01-10 08:49:57.503', N'JennaJ@ibuyspy.com')
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (15, 18, N'Re: Foreign language terms for ''mole''?', CAST(0x000090240092A630 AS DateTime), N'There''s a great dictionary for intelligence terms both in English and the languages of the other major intelligence agencies: <i>The CIA Insider''s Dictionary of US and Foreign Intelligence, Counterintelligence & Tradecraft</i>.  Last time I looked Amazon offered it, but was out of stock.  Let me know if you want to borrow my copy.', N'2001-01-10 08:49:57.5032001-01-10 08:53:55.600', N'TomVZ@ibuyspy.com')
INSERT [Portal_Discussion] ([ItemID], [ModuleID], [Title], [CreatedDate], [Body], [DisplayOrder], [CreatedByUser]) VALUES (16, 18, N'AMT Mini Night Vision Monocular', CAST(0x000090250098B5C0 AS DateTime), N'Has anyone tried this yet?  It looks really good: tiny (9.5 oz), built-in IR illumination.  The only downside I see is that it seems to be limited to 1.5x magnification.', N'2001-01-12 09:15:59.640', N'ManishG@ibuyspy.com')
SET IDENTITY_INSERT [Portal_Discussion] OFF
/* <lang><zh-CN>联系人种子为员工联系模块提供演示姓名、角色、邮箱和电话文本；这些值是样例数据而非目录主数据，后续企业员工目录治理不得从此脚本推断真实组织结构。</zh-CN><en>The contacts seed provides demo names, roles, email addresses, and phone text for the employee-contact module; these values are sample data rather than directory master data, so later enterprise-directory work must not infer real organization structure from this script.</en></lang> */
/****** Object:  Table [dbo].[Portal_Contacts]    Script Date: 03/01/2012 22:46:17 ******/
SET IDENTITY_INSERT [Portal_Contacts] ON
INSERT [Portal_Contacts] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Name], [Role], [Email], [Contact1], [Contact2]) VALUES (1, 9, N'JennaJ@ibuyspy.com', CAST(0x0000917B00D5F363 AS DateTime), N'JennaJ', N'Program Lead', N'JennaJ@ibuyspy.com', N'home: 206-555-4434', N'mobile: 206-555-8381')
INSERT [Portal_Contacts] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Name], [Role], [Email], [Contact1], [Contact2]) VALUES (2, 9, N'JennaJ@ibuyspy.com', CAST(0x0000917B00D65215 AS DateTime), N'ManishG', N'Technical Lead', N'ManishG@ibuyspy.com', N'home: 425-555-9008', N'mobile: 425-555-7665')
INSERT [Portal_Contacts] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Name], [Role], [Email], [Contact1], [Contact2]) VALUES (3, 9, N'JennaJ@ibuyspy.com', CAST(0x0000917B00D74FE9 AS DateTime), N'BrettH', N'Development Lead', N'BrettH@ibuyspy.com', N'home: 206-555-5580', N'mobile: 206-555-1323')
INSERT [Portal_Contacts] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Name], [Role], [Email], [Contact1], [Contact2]) VALUES (4, 9, N'JennaJ@ibuyspy.com', CAST(0x0000917B00E8C75B AS DateTime), N'MaryK', N'Test Lead', N'MaryK@ibuyspy.com', N'home: 206-555-7729', N'mobile: 206-555-8585')
INSERT [Portal_Contacts] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Name], [Role], [Email], [Contact1], [Contact2]) VALUES (5, 9, N'JennaJ@ibuyspy.com', CAST(0x0000917B00E8D6CD AS DateTime), N'RajivP', N'Fullfillment Lead', N'RajivP@ibuyspy.com', N'home: 425-555-7787', N'mobile: 425-555-4443')
INSERT [Portal_Contacts] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Name], [Role], [Email], [Contact1], [Contact2]) VALUES (6, 9, N'JennaJ@ibuyspy.com', CAST(0x0000917B00D70883 AS DateTime), N'TomVZ', N'Secret Agent', N'TomVZ@ibuyspy.com', N'shoe phone: 206-555-4433', N'fountain pen: 206-555-9985')
SET IDENTITY_INSERT [Portal_Contacts] OFF
/* <lang><zh-CN>公告种子混合站内 `notimplemented.aspx` 链接和历史外部 URL，用于演示公告模块的链接字段与过期日期；离线/合规环境可屏蔽访问，但不要在 seed 注释治理中改写 URL 文本。</zh-CN><en>The announcements seed mixes in-site `notimplemented.aspx` links and historical external URLs to demonstrate announcement link fields and expiration dates; offline or compliance environments may block navigation, but seed-comment governance must not rewrite the URL text.</en></lang> */
/****** Object:  Table [dbo].[Portal_Announcements]    Script Date: 03/01/2012 22:46:17 ******/
SET IDENTITY_INSERT [Portal_Announcements] ON
INSERT [Portal_Announcements] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [MoreLink], [MobileMoreLink], [ExpireDate], [Description]) VALUES (1, 8, N'JennaJ@ibuyspy.com', CAST(0x0000917A0103F0FC AS DateTime), N'Open Enrollment and Payroll Checklist', N'~/admin/notimplemented.aspx?title=Open%20Enrollment%20and%20Payroll%20Checklist', N'', CAST(0x0000B3C400000000 AS DateTime), N'Please take a few moments to review this year-end checklist that will guide you through the Benefits Open Enrollment process and instruct you on how to ensure your payroll information is accurate for 2001.')
INSERT [Portal_Announcements] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [MoreLink], [MobileMoreLink], [ExpireDate], [Description]) VALUES (2, 8, N'JennaJ@ibuyspy.com', CAST(0x0000917A0104AD21 AS DateTime), N'Selecting Your Primary Care Provider', N'~/admin/notimplemented.aspx?title=Selecting%20Your%20Primary%20Care%20Provider', N'', CAST(0x0000B3C400000000 AS DateTime), N'Learn how to find the Primary Care Provider (PCP) that best suits your needs with this list of things to think about and questions to ask yourself and your potential PCP.')
INSERT [Portal_Announcements] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [MoreLink], [MobileMoreLink], [ExpireDate], [Description]) VALUES (3, 3, N'JennaJ@ibuyspy.com', CAST(0x0000919000BA2F8F AS DateTime), N'Q4 Sales Rise 200% Over Last Year', N'~/admin/notimplemented.aspx?title=Q4%20Sales%20Rise%20200%%20Over%20Last%20Year', N'', CAST(0x0000B3C400000000 AS DateTime), N'IBuySpy.com online sales for the crucial fourth quarter of last year rose nearly 200% over the previous year, despite a lackluster holiday sales overall. ')
INSERT [Portal_Announcements] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [MoreLink], [MobileMoreLink], [ExpireDate], [Description]) VALUES (4, 13, N'JennaJ@ibuyspy.com', CAST(0x0000919000656657 AS DateTime), N'Envelope X-Ray Spray', N'http://www.spyworld.net/Surveil3.htm', N'', CAST(0x0000B3C400000000 AS DateTime), N'Envelope X-RAY Spray turns opaque paper temporarily translucent, allowing the user to view the contents of an envelope without ever opening it. SpyWorld, $42.95.')
INSERT [Portal_Announcements] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [MoreLink], [MobileMoreLink], [ExpireDate], [Description]) VALUES (5, 13, N'JennaJ@ibuyspy.com', CAST(0x000091900068427D AS DateTime), N'Wrist Watch Video Camera', N'http://www.spyworld.net/Surveil1.htm', N'', CAST(0x0000B3C400000000 AS DateTime), N'This is not a device from a James bond movie, but a real, sophisticated video camera disguised as a watch.  SpyWorld, $489.95.')
INSERT [Portal_Announcements] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [MoreLink], [MobileMoreLink], [ExpireDate], [Description]) VALUES (6, 13, N'JennaJ@ibuyspy.com', CAST(0x0000919000B0E041 AS DateTime), N'Bionic Ear', N'http://www.spyworld.net/Surveil3.htm', N'', CAST(0x0000B3C400000000 AS DateTime), N'Zoom in on a whisper at up to 100 yards away, door shutting at 4 blocks, dog barking up to 2 miles away.  SpyWorld, $198.95.')
INSERT [Portal_Announcements] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [MoreLink], [MobileMoreLink], [ExpireDate], [Description]) VALUES (7, 13, N'JennaJ@ibuyspy.com', CAST(0x0000919000B0F699 AS DateTime), N'CAMCopter', N'http://www.spyworld.com/camcopter.htm', N'', CAST(0x0000B3C400000000 AS DateTime), N'The CAMCopter is a remotely controlled, autonomous Aerial Vehicle System developed under military specifications design to carry various sensors that transmit data and live video.  SpyWorld, $490,000.00.')
INSERT [Portal_Announcements] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [MoreLink], [MobileMoreLink], [ExpireDate], [Description]) VALUES (8, 12, N'JennaJ@ibuyspy.com', CAST(0x000091900073E5DC AS DateTime), N'Ultraviolet Pen', N'http://www.spyproducts.com/Theftpowders1.html', N'', CAST(0x0000973B00000000 AS DateTime), N'This felt-tipped pen is an inexpensive and convenient ultraviolet writing instrument.  SpyProducts, $6.95.')
INSERT [Portal_Announcements] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [MoreLink], [MobileMoreLink], [ExpireDate], [Description]) VALUES (9, 12, N'JennaJ@ibuyspy.com', CAST(0x0000919000750BDC AS DateTime), N'Micro Bug Detector ', N'http://www.spygear4u.com/hi_tech.htm', N'', CAST(0x0000B3C400000000 AS DateTime), N'Covert bug detection probe for room sweeping include a vibration mode.  SpyGear4U, $399.00.')
INSERT [Portal_Announcements] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [MoreLink], [MobileMoreLink], [ExpireDate], [Description]) VALUES (10, 12, N'JennaJ@ibuyspy.com', CAST(0x00009190007610E8 AS DateTime), N'Telephone Voice Changer', N'http://www.firstlineindustries.com/telvoicchani.html', N'', CAST(0x0000973B00000000 AS DateTime), N'Answer the telephone without anyone recognizing your voice.  FirstLine, $42.95.')
INSERT [Portal_Announcements] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [MoreLink], [MobileMoreLink], [ExpireDate], [Description]) VALUES (11, 14, N'JennaJ@ibuyspy.com', CAST(0x000091900072A959 AS DateTime), N'Air Taser', N'http://www.spyworld.com/3005.htm', N'', CAST(0x0000B3C400000000 AS DateTime), N'Uses compressed air to shoot two small probes up to 15 feet.  These probes are connected by wire to the launcher, which sends a powerful electric signal into the nervous system of an assailant.  SpyWorld, $285.95.')
SET IDENTITY_INSERT [Portal_Announcements] OFF

/****** Object:  Table [dbo].[Portal_Links]    Script Date: 03/01/2012 22:46:17 ******/
SET IDENTITY_INSERT [Portal_Links] ON
INSERT [Portal_Links] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [Url], [MobileUrl], [ViewOrder], [Description]) VALUES (1, 1, N'JennaJ@ibuyspy.com', CAST(0x0000917B00F2292C AS DateTime), N'ASP.NET Site', N'http://www.asp.net', N'', 1, N'')
INSERT [Portal_Links] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [Url], [MobileUrl], [ViewOrder], [Description]) VALUES (2, 1, N'JennaJ@ibuyspy.com', CAST(0x0000917B00F2292C AS DateTime), N'GotDotNet.com', N'http://www.gotdotnet.com', N'', 3, N'')
INSERT [Portal_Links] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [Url], [MobileUrl], [ViewOrder], [Description]) VALUES (3, 1, N'JennaJ@ibuyspy.com', CAST(0x0000917B00F2292C AS DateTime), N'ASP.NET on MSDN', N'http://msdn.microsoft.com/net/aspnet', N'', 5, N'')
INSERT [Portal_Links] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [Url], [MobileUrl], [ViewOrder], [Description]) VALUES (4, 1, N'JennaJ@ibuyspy.com', CAST(0x0000917B00F2292C AS DateTime), N'QuickStart Samples', N'http://www.gotdotnet.com/quickstart/aspplus', N'', 7, N'')
INSERT [Portal_Links] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [Url], [MobileUrl], [ViewOrder], [Description]) VALUES (5, 16, N'JennaJ@ibuyspy.com', CAST(0x0000917B00F2292C AS DateTime), N'SpyWorld', N'http://www.SpyWorld.com', N'', 1, N'')
INSERT [Portal_Links] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [Url], [MobileUrl], [ViewOrder], [Description]) VALUES (6, 16, N'JennaJ@ibuyspy.com', CAST(0x0000917B00F2292C AS DateTime), N'SpyGear4U', N'http://www.SpyGear4U.com', N'', 3, N'')
INSERT [Portal_Links] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [Url], [MobileUrl], [ViewOrder], [Description]) VALUES (7, 16, N'JennaJ@ibuyspy.com', CAST(0x0000917B00F2292C AS DateTime), N'GlobalSpy', N'http://www.GlobalSpy.com', N'', 5, N'')
INSERT [Portal_Links] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [Url], [MobileUrl], [ViewOrder], [Description]) VALUES (8, 16, N'JennaJ@ibuyspy.com', CAST(0x0000917B00F2292C AS DateTime), N'SpyProducts', N'http://www.SpyProducts.com', N'', 7, N'')
INSERT [Portal_Links] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [Url], [MobileUrl], [ViewOrder], [Description]) VALUES (9, 21, N'JennaJ@ibuyspy.com', CAST(0x0000917B00F2292C AS DateTime), N'ASP.NET Site', N'http://www.asp.net', N'', 1, N'')
INSERT [Portal_Links] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [Url], [MobileUrl], [ViewOrder], [Description]) VALUES (10, 21, N'JennaJ@ibuyspy.com', CAST(0x0000917B00F2292C AS DateTime), N'GotDotNet.com', N'http://www.gotdotnet.com', N'', 3, N'')
INSERT [Portal_Links] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [Url], [MobileUrl], [ViewOrder], [Description]) VALUES (11, 21, N'JennaJ@ibuyspy.com', CAST(0x0000917B00F2292C AS DateTime), N'ASP.NET on MSDN', N'http://msdn.microsoft.com/net/aspnet', N'', 5, N'')
INSERT [Portal_Links] ([ItemID], [ModuleID], [CreatedByUser], [CreatedDate], [Title], [Url], [MobileUrl], [ViewOrder], [Description]) VALUES (12, 21, N'JennaJ@ibuyspy.com', CAST(0x0000917B00F2292C AS DateTime), N'QuickStart Samples', N'http://www.gotdotnet.com/quickstart/aspplus', N'', 7, N'')
SET IDENTITY_INSERT [Portal_Links] OFF
/* <lang><zh-CN>模块设置种子为 XML/XSL 渲染模块和图片模块写入相对资源路径；字段名和值被运行时代码按字符串读取，因此治理注释只能描述契约，不能顺手改名或迁移路径。</zh-CN><en>The module-settings seed writes relative resource paths for the XML/XSL rendering module and the image module; runtime code reads both setting names and values as strings, so comment governance may describe the contract but must not opportunistically rename settings or migrate paths.</en></lang> */
/****** Object:  Table [dbo].[PortalCfg_ModuleSettings]    Script Date: 03/01/2012 22:46:17 ******/
SET IDENTITY_INSERT [PortalCfg_ModuleSettings] ON
INSERT [PortalCfg_ModuleSettings] ([ModuleSettingId], [SettingName], [SettingText], [ModuleId]) VALUES (1, N'xmlsrc', N'~/data/sales.xml', 6)
INSERT [PortalCfg_ModuleSettings] ([ModuleSettingId], [SettingName], [SettingText], [ModuleId]) VALUES (2, N'xslsrc', N'~/data/sales.xsl', 6)
INSERT [PortalCfg_ModuleSettings] ([ModuleSettingId], [SettingName], [SettingText], [ModuleId]) VALUES (3, N'src', N'~/data/nightvis.gif', 15)
SET IDENTITY_INSERT [PortalCfg_ModuleSettings] OFF
