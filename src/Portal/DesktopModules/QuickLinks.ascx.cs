using System;
using Microsoft.Practices.Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class QuickLinks : PortalModuleControl<QuickLinks>
    {
        protected string linkImage = "";

        [Dependency]
        public ILinksDb LinkDB { private get; set; }

        //*******************************************************
        //
        // The Page_Load event handler on this User Control is used to
        // obtain a DataReader of link information from the Links
        // table, and then databind the results to a templated DataList
        // server control.  It uses the ASPNET.StarterKit.Portal.LinkDB()
        // data component to encapsulate all data functionality.
        //
        //*******************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            // Set the link image type
            if (IsEditable)
            {
                linkImage = "~/images/edit.gif";
            }
            else
            {
                linkImage = "~/images/navlink.gif";
            }

            // Obtain links information from the Links table
            // and bind to the list control
            myDataList.DataSource = LinkDB.GetLinks(ModuleId);
            myDataList.DataBind();

            // Ensure that only users in role may add links
            if (PortalSecurity.IsInRoles(ModuleConfiguration.AuthorizedEditRoles))
            {
                EditButton.Text = "Add Link";
                EditButton.NavigateUrl = "~/DesktopModules/EditLinks.aspx?mid=" + ModuleId;
            }
        }

        protected string ChooseURL(string itemID, string modID, string URL)
        {
            if (IsEditable)
            {
                return "~/DesktopModules/EditLinks.aspx?ItemID=" + itemID + "&mid=" + modID;
            }
            else
            {
                return URL;
            }
        }
    }
}