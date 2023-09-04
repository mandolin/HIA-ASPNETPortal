注：原项目的系统需求，已过时，仅供参考


**此入门套件需要以下内容：**

（详情请参阅下文下载所需软件）

Windows 2000、Windows XP 或 Windows Server 2003。

Microsoft .NET Framework（可免费下载）。

SQL Server 2000 或 Microsoft SQL Server Desktop Engine (MSDE) 2000（可免费下载）。



**如果您使用的是 Windows 2003**

·          必须启用 Web 应用程序服务器角色。



**如果您使用的是 Windows XP Pro 或 Windows 2000**

·          您必须安装 Microsoft .NET Framework v1.0 或 v1.1（推荐使用 v1.1）

·          您必须安装 IIS 或使用 Cassini（见下文）。

·          \[仅限移动工具包] 如果您使用的是 Microsoft .NET Framework v1.0 并且想要使用此入门工具包的移动功能，则必须安装 Microsoft Mobile Internet Toolkit 1.0。



**如果您使用的是 Windows XP Home**

·          您必须安装 Microsoft .NET Framework v1.0 或 v1.1（推荐使用 v1.1）

·          您必须安装 Cassini。

·          \[仅限移动工具包] 如果您使用的是 Microsoft .NET Framework v1.0 并且想要使用此入门工具包的移动功能，则必须安装 Microsoft Mobile Internet Toolkit 1.0。



**获取软件：**



Cassini 是一款免费的测试网络服务器。 您可以通过以下方式获取它：

·          安装 Web Matrix，这是一个免费的 ASP.NET 开发工具，可从 <http://www.asp.net/webmatrix> 获取，或者

·          从 <http://www.asp.net/Projects/Cassini/Download/> 安装 Cassini



Microsoft .NET Framework 可从以下位置免费下载：

<http://www.asp.net/download-1.1.aspx>



MSDE 数据库引擎可从以下位置免费获得：

<http://www.asp.net/msde>



Microsoft Mobile Internet Toolkit 1.0（仅适用于 .NET Framework 1.0 用户）可从以下位置免费获取：

<http://www.asp.net/mobile>



**笔记：**

·       您需要管理权限

·       在继续之前卸载所有以前的版本

·       将创建/修改 PortalCSVS 虚拟目录

·       对于“本地”安装，将创建/修改门户数据库

·       对于“远程”安装，请记住在运行应用程序之前在 web.config 文件中配置数据库连接字符串，并使用提供的 SQL 脚本手动安装数据库。



**安装后：**

·       您可以通过 URL 访问该网站

http://localhost/PortalCSVS/\
使用用户名“guest”和密码“guest”登录以访问“管理员”功能\\

·       您可以通过以下方式从“开始”菜单访问源代码、自述文件和文档链接

开始 > 程序 > ASP.NET 入门工具包