using System.Collections.Generic;
using System.Configuration;
using System.Xml;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>解析容器组件配置节的 ASP.NET 配置处理器。</zh-CN>
    ///   <en>ASP.NET configuration handler that parses the container-component section.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>旧门户通过配置声明可用容器组件，本处理器只把 XML 节点转换为轻量列表，不实例化控件类型，也不验证物理路径。</zh-CN>
    ///   <en>The legacy portal declares available container components through configuration. This handler only converts XML nodes into a lightweight list; it does not instantiate control types or validate physical paths.</en>
    /// </lang>
    /// </remarks>
    public class ContainerComponentHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>从配置节中读取容器组件类型名列表。</zh-CN>
        ///   <en>Reads container-component type names from the configuration section.</en>
        /// </lang>
        /// </summary>
        /// <param name="parent">
        /// <l>
        ///   <zh-CN>父级配置对象；旧处理器不使用该值。</zh-CN>
        ///   <en>Parent configuration object; this legacy handler does not use it.</en>
        /// </l>
        /// </param>
        /// <param name="configContext">
        /// <l>
        ///   <zh-CN>配置上下文；当前解析逻辑保持与旧 Web.config 节一致。</zh-CN>
        ///   <en>Configuration context; the current parsing logic stays aligned with the legacy Web.config section.</en>
        /// </l>
        /// </param>
        /// <param name="section">
        /// <l>
        ///   <zh-CN>包含 containerComponent 子节点的配置节。</zh-CN>
        ///   <en>Configuration section containing containerComponent child nodes.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>由配置声明生成的容器组件条目列表。</zh-CN>
        ///   <en>List of container-component entries produced from configuration declarations.</en>
        /// </l>
        /// </returns>
        public object Create(object parent,
                             object configContext, XmlNode section)
        {
            // <lang>
            //   <zh-CN>返回列表是配置节的轻量投影，生命周期限定在配置系统读取结果中。</zh-CN>
            //   <en>The returned list is a lightweight projection of the configuration section and its lifetime is limited to the configuration-system read result.</en>
            // </lang>
            var items = new List<ContainerComponentItem>();
            // <lang>
            //   <zh-CN>节点查询只匹配直接声明的 containerComponent 条目，不解析嵌套配置或外部文件。</zh-CN>
            //   <en>The node query matches only directly declared containerComponent entries and does not parse nested configuration or external files.</en>
            // </lang>
            XmlNodeList nodes = section.SelectNodes("containerComponent");

            // <lang>
            //   <zh-CN>逐个读取 typeName 属性，保持旧配置节只登记类型名的简单语义。</zh-CN>
            //   <en>Read each typeName attribute and preserve the old section's simple type-name registration semantics.</en>
            // </lang>
            foreach (XmlNode node in nodes)
            {
                // <lang>
                //   <zh-CN>每个条目都是值类型副本，仅承载配置中的类型名字符串，不触发反射或控件创建。</zh-CN>
                //   <en>Each entry is a value-type copy carrying only the configured type-name string and does not trigger reflection or control creation.</en>
                // </lang>
                var item = new ContainerComponentItem();
                // <lang>
                //   <zh-CN>typeName 属性沿用旧 Web.config 必填约定；缺失属性继续让配置读取失败，避免静默注册空组件。</zh-CN>
                //   <en>The typeName attribute follows the legacy Web.config required convention; a missing attribute still fails configuration reading instead of silently registering an empty component.</en>
                // </lang>
                item.TypeName = node.Attributes["typeName"].InnerText;
                // <lang>
                //   <zh-CN>追加顺序保持 XML 声明顺序，供旧容器机制按原配置顺序消费。</zh-CN>
                //   <en>The append order preserves XML declaration order for the legacy container mechanism to consume in original configuration order.</en>
                // </lang>
                items.Add(item);
            }
            // <lang>
            //   <zh-CN>返回对象类型保持为旧配置处理器预期的 object，实际内容为容器组件列表。</zh-CN>
            //   <en>The return type remains object as expected by the legacy configuration handler, while the actual content is the container-component list.</en>
            // </lang>
            return items;
        }

    }
}
