using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;

// 定义一个命名空间，该命名空间可能属于某个Portal项目的安全模块
namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// PortalSecurity 类封装了两个辅助方法，使开发人员能够轻松地检查当前浏览器客户端的角色状态。
    /// </summary>
    public class PortalSecurity : IPortalSecurity
    {
        // 声明接口类型的私有成员变量
        private readonly IModulesDb _modulesConfig;
        private readonly ITabsDb _tabsConfig;

        // 构造函数初始化配置数据库接口实例
        public PortalSecurity(ITabsDb tabsConfig, IModulesDb modulesConfig)
        {
            _tabsConfig = tabsConfig;
            _modulesConfig = modulesConfig;
        }

        #region IPortalSecurity Members

        /// <summary>
        /// HasEditPermissions 方法用于检查当前浏览器客户端是否有权限编辑指定的门户模块设置。
        /// </summary>
        public bool HasEditPermissions(int moduleId)
        {
            // 获取指定ID的模块信息
            IModuleItem module = _modulesConfig.GetSingleModule(moduleId);

            // 获取模块允许编辑的角色以及所属标签页的访问角色
            string editRoles = module.EditRoles;
            string accessRoles = _tabsConfig.GetSingleTab(module.TabId.Value).AccessRoles;

            // 检查当前用户是否在允许访问或编辑的角色列表中
            if (!IsInRoles(accessRoles) || !IsInRoles(editRoles))
            {
                return false;
            }
            return true;
        }

        #endregion

        /// <summary>
        /// Encrypt 方法将明文字符串加密为哈希字符串。
        /// </summary>
        public static string Encrypt(string cleanString)
        {
            // 将输入字符串转换成字节数组
            byte[] clearBytes = Encoding.UTF8.GetBytes(cleanString);

            // 创建MD5哈希算法实例并计算哈希值
            using (HashAlgorithm algorithm = MD5.Create())
            {
                byte[] hashedBytes = algorithm.ComputeHash(clearBytes);

                // 将字节数组转换为十六进制字符串
                return BitConverter.ToString(hashedBytes);
            }
        }

        /// <summary>
        /// IsInRole 方法用于检查当前浏览器客户端是否具有指定的角色。
        /// </summary>
        public static bool IsInRole(string role)
        {
            // 使用当前HttpContext来检查用户是否在指定的角色中
            return HttpContext.Current?.User?.IsInRole(role) ?? false;
        }

        /// <summary>
        /// IsInRoles 方法用于检查当前浏览器客户端是否处于一组角色中的任何一个。
        /// </summary>
        public static bool IsInRoles(string roles)
        {
            // 获取当前的HttpContext
            HttpContext context = HttpContext.Current;

            if (context == null)
            {
                return false;
            }

            // 统一使用旧角色字符串解析器，兼容末尾分号、空项和多余空白。
            foreach (string role in PortalRoleParser.Parse(roles))
            {
                // 如果角色为 All Users，或者当前用户在指定角色中，则允许访问。
                if (string.Equals(role, PortalRoleNames.AllUsers, StringComparison.OrdinalIgnoreCase) ||
                    (context.User?.IsInRole(role) ?? false))
                {
                    return true;
                }
            }

            // 如果没有匹配任何角色，则返回false
            return false;
        }
    }
}
