using System;
using Microsoft.Practices.Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class EditLinks : PortalPage<EditLinks>
    {
        private int itemId;
        private int moduleId;

        [Dependency]
        public ILinksDb LinkDB { private get; set; }

        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        //****************************************************************
        //
        // The Page_Load event on this Page is used to obtain the 
        // ItemId of the link to edit.
        //
        // It then uses the ASPNET.StarterKit.Portal.LinkDB() data component
        // to populate the page's edit controls with the links details.
        //
        //****************************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            // Determine ModuleId of Links Portal Module
            moduleId = Int32.Parse(Request.Params["Mid"]);

            // Verify that the current user has access to edit this module
            if (PortalSecurity.HasEditPermissions(moduleId) == false)
            {
                Response.Redirect("~/Admin/EditAccessDenied.aspx");
            }

            // Determine ItemId of Link to Update
            if (Request.Params["ItemId"] != null)
            {
                itemId = Int32.Parse(Request.Params["ItemId"]);
            }

            // If the page is being requested the first time, determine if an
            // link itemId value is specified, and if so populate page
            // contents with the link details

            if (Page.IsPostBack == false)
            {
                if (itemId != 0)
                {
                    // Obtain a single row of link information
                    ILinkItem item = LinkDB.GetSingleLink(itemId);

                    // Security check.  verify that itemid is within the module.
                    if (item.ModuleId != moduleId)
                    {
                        Response.Redirect("~/Admin/EditAccessDenied.aspx");
                    }

                    TitleField.Text = item.Title;
                    DescriptionField.Text = item.Description;
                    UrlField.Text = item.Url;
                    MobileUrlField.Text = item.MobileUrl;
                    ViewOrderField.Text = item.ViewOrder.Value.ToString();
                    CreatedBy.Text = item.CreatedByUser;
                    CreatedDate.Text = item.CreatedDate.Value.ToShortDateString();
                }

                // Store URL Referrer to return to portal
                ViewState["UrlReferrer"] = Request.UrlReferrer.ToString();
            }
        }

        //****************************************************************
        //
        // The UpdateBtn_Click event handler on this Page is used to either
        // create or update a link.  It  uses the ASPNET.StarterKit.Portal.LinkDB()
        // data component to encapsulate all data functionality.
        //
        //****************************************************************

        protected void UpdateBtn_Click(Object sender, EventArgs e)
        {
            if (Page.IsValid)
            {
                // Create an instance of the Link DB component
                if (itemId == 0)
                {
                    // Add the link within the Links table
                    LinkDB.AddLink(moduleId, Context.User.Identity.Name, TitleField.Text, UrlField.Text,
                                   MobileUrlField.Text, Int32.Parse(ViewOrderField.Text), DescriptionField.Text);
                }
                else
                {
                    // Update the link within the Links table
                    LinkDB.UpdateLink(itemId, Context.User.Identity.Name, TitleField.Text, UrlField.Text,
                                      MobileUrlField.Text, Int32.Parse(ViewOrderField.Text), DescriptionField.Text);
                }

                // Redirect back to the portal home page
                Response.Redirect((String) ViewState["UrlReferrer"]);
            }
        }

        //****************************************************************
        //
        // The DeleteBtn_Click event handler on this Page is used to delete 
        // a link.  It  uses the ASPNET.StarterKit.Portal.LinksDB()
        // data component to encapsulate all data functionality.
        //
        //****************************************************************

        protected void DeleteBtn_Click(Object sender, EventArgs e)
        {
            // Only attempt to delete the item if it is an existing item
            // (new items will have "ItemId" of 0)

            if (itemId != 0)
            {
                LinkDB.DeleteLink(itemId);
            }

            // Redirect back to the portal home page
            Response.Redirect((String) ViewState["UrlReferrer"]);
        }

        //****************************************************************
        //
        // The CancelBtn_Click event handler on this Page is used to cancel
        // out of the page, and return the user back to the portal home
        // page.
        //
        //****************************************************************

        protected void CancelBtn_Click(Object sender, EventArgs e)
        {
            // Redirect back to the portal home page
            Response.Redirect((String) ViewState["UrlReferrer"]);
        }
    }
}