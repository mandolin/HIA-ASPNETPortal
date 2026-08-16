using System.Reflection;
using System.Runtime.InteropServices;

// <lang>
//   <zh-CN>测试程序集元数据只服务本机回归发现，不暴露 COM，也不改变门户运行时程序集身份。</zh-CN>
//   <en>The test assembly metadata only supports local regression discovery; it does not expose COM or change Portal runtime assembly identity.</en>
// </lang>
[assembly: AssemblyTitle("Portal.Tests")]
[assembly: AssemblyDescription("Automated regression tests for HIA ASP.NET Portal contracts.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("HIA")]
[assembly: AssemblyProduct("HIA ASP.NET Portal")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("3f6b772a-9338-4e0f-9f4c-b415df488923")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
