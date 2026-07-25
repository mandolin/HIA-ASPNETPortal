namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>由容器组件配置节解析出的轻量条目。</zh-CN>
    ///   <en>Lightweight item parsed from the container-component configuration section.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该结构只保存类型名字符串，实际类型加载和控件生命周期仍由旧门户容器机制负责。</zh-CN>
    ///   <en>This structure stores only the type-name string; actual type loading and control lifecycle remain the responsibility of the legacy portal container mechanism.</en>
    /// </lang>
    /// </remarks>
    public struct ContainerComponentItem
    {
        /// <summary>
        /// <l>
        ///   <zh-CN>容器组件类型名。</zh-CN>
        ///   <en>Container-component type name.</en>
        /// </l>
        /// </summary>
        public string TypeName;
    }
}
