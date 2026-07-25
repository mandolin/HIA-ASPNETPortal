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
            var items = new List<ContainerComponentItem>();
            XmlNodeList nodes = section.SelectNodes("containerComponent");

            // <lang>
            //   <zh-CN>逐个读取 typeName 属性，保持旧配置节只登记类型名的简单语义。</zh-CN>
            //   <en>Read each typeName attribute and preserve the old section's simple type-name registration semantics.</en>
            // </lang>
            foreach (XmlNode node in nodes)
            {
                var item = new ContainerComponentItem();
                item.TypeName = node.Attributes["typeName"].InnerText;
                items.Add(item);
            }
            return items;
        }

    }
}
