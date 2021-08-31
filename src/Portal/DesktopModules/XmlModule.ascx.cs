using System;
using System.IO;
using System.Web.UI;

namespace ASPNET.StarterKit.Portal
{
    public partial class XmlModule : PortalModuleControl<XmlModule>
    {
        //*******************************************************
        //
        // The Page_Load event handler on this User Control uses
        // the Portal configuration system to obtain an xml document
        // and xsl/t transform file location.  It then sets these
        // properties on an <asp:Xml> server control.
        //
        //*******************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            var xmlsrc = (String) Settings["xmlsrc"];

            if ((xmlsrc != null) && (xmlsrc != ""))
            {
                if (File.Exists(Server.MapPath(xmlsrc)))
                {
                    xml1.DocumentSource = xmlsrc;
                }
                else
                {
                    Controls.Add(
                        new LiteralControl("<" + "br" + "><" + "span class=NormalRed" + ">" + "File " + xmlsrc +
                                           " not found.<" + "br" + ">"));
                }
            }

            var xslsrc = (String) Settings["xslsrc"];

            if ((xslsrc != null) && (xslsrc != ""))
            {
                if (File.Exists(Server.MapPath(xslsrc)))
                {
                    xml1.TransformSource = xslsrc;
                }
                else
                {
                    Controls.Add(
                        new LiteralControl("<" + "br" + "><" + "span class=NormalRed>File " + xslsrc + " not found.<" +
                                           "br" + ">"));
                }
            }
        }
    }
}