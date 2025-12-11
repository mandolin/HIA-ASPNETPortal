using System;
using System.Collections;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   PortalModuleControl 类定义了一个自定义基类，该基类被门户中的所有桌面门户模块继承。
    ///   
    ///   PortalModuleControl 类定义了门户特定的属性，这些属性由门户框架使用以正确显示门户模块。
    ///
    ///   The PortalModuleControl class defines a custom base class inherited by all
    ///   desktop portal modules within the Portal.
    /// 
    ///   The PortalModuleControl class defines portal specific properties
    ///   that are used by the portal framework to correctly display portal modules
    /// </summary>
    public class PortalModuleControl<T> : UserControl, IPortalModuleControl where T : class
    {
        // 私有字段变量

        // 是否可编辑
        private int _isEditable;

        // 模块配置
        private ModuleSettings _moduleConfiguration;

        // 模块设置
        private Hashtable _settings;

        /// <summary>
        /// 注入模块数据库接口
        /// </summary>
        [Dependency]
        public IModulesDb ModulesConfig { private get; set; } // 

        #region IPortalModuleControl 成员

        /// <summary>
        /// 模块Id
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // 设置属性不可浏览且不在设计器序列化中可见
        public int ModuleId
        {
            // 返回的是模块配置里的模块Id
            get { return _moduleConfiguration.ModuleId; } 
        }

        /// <summary>
        /// 获取或设置门户ID
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int PortalId { get; set; }

        /// <summary>
        /// 是否可编辑的状态
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsEditable
        {
            get
            {
                // 执行三态开关检查以避免每次访问属性时都进行安全角色查询（而是缓存结果）

                if (_isEditable == 0) // 如果状态未知
                {
                    // 从当前上下文中获取门户设置
                    var portalSettings = (PortalSettings)HttpContext.Current.Items["PortalSettings"];

                    if (portalSettings.AlwaysShowEditButton || // 如果总是显示编辑按钮
                        PortalSecurity.IsInRoles(_moduleConfiguration.AuthorizedEditRoles)) // 或者当前用户具有编辑权限
                    {
                        _isEditable = 1; // 设置为可编辑
                    }
                    else
                    {
                        _isEditable = 2; // 设置为不可编辑
                    }
                }

                return (_isEditable == 1); // 返回是否可编辑
            }
        }

        /// <summary>
        /// 模块配置
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ModuleSettings ModuleConfiguration
        {
            get { return _moduleConfiguration; } // 获取模块配置
            set { _moduleConfiguration = value; } // 设置模块配置
        }

        /// <summary>
        /// 模块设置
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Hashtable Settings
        {
            get
            {
                if (_settings == null) // 如果尚未加载设置
                {
                    _settings = ModulesConfig.GetModuleSettings(ModuleId); // 从数据库获取模块设置
                }

                return _settings; // 返回模块设置
            }
        }

        #endregion

        protected override void OnInit(EventArgs e) // 重写初始化事件
        {
            base.OnInit(e); // 调用基类的初始化方法

            // 注意: 由于是动态加载的模块，因此需要在这里调用 BuildItemWithCurrentContext 来动态注入依赖项
            Global.BuildItemWithCurrentContext<T>(this); // 动态注入依赖
        }
    }
}