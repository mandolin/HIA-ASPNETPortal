using System.Collections.Generic;
using System.Configuration;
using System.Xml;

namespace ASPNET.StarterKit.Portal
{
    public class ContainerComponentHandler : IConfigurationSectionHandler
    {
        public object Create(object parent,
                             object configContext, XmlNode section)
        {
            var items = new List<ContainerComponentItem>();
            XmlNodeList nodes = section.SelectNodes("containerComponent");

            //process each Node "Proceso"
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