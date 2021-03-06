using System;
using Microsoft.Practices.Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class EditAnnouncements : PortalPage<EditAnnouncements>
    {
        private int itemId;
        private int moduleId;

        [Dependency]
        public IAnnouncementsDb AnnouncementsDB { private get; set; }

        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        //****************************************************************
        //
        // The Page_Load event on this Page is used to obtain the ModuleId
        // and ItemId of the announcement to edit.
        //
        // It then uses the ASPNET.StarterKit.Portal.AnnouncementsDB() data component
        // to populate the page's edit controls with the annoucement details.
        //
        //****************************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            // Determine ModuleId of Announcements Portal Module
            moduleId = Int32.Parse(Request.Params["Mid"]);

            // Verify that the current user has access to edit this module
            if (PortalSecurity.HasEditPermissions(moduleId) == false)
            {
                Response.Redirect("~/Admin/EditAccessDenied.aspx");
            }

            // Determine ItemId of Announcement to Update
            if (Request.Params["ItemId"] != null)
            {
                itemId = Int32.Parse(Request.Params["ItemId"]);
            }

            // If the page is being requested the first time, determine if an
            // announcement itemId value is specified, and if so populate page
            // contents with the announcement details

            if (Page.IsPostBack == false)
            {
                if (itemId != 0)
                {
                    // Obtain a single row of announcement information
                    IAnnouncementItem item = AnnouncementsDB.GetSingleAnnouncement(itemId);

                    // Security check.  verify that itemid is within the module.                    
                    if (item.ModuleId != moduleId)
                    {
                        Response.Redirect("~/Admin/EditAccessDenied.aspx");
                    }

                    TitleField.Text = item.Title;
                    MoreLinkField.Text = item.MoreLink;
                    MobileMoreField.Text = item.MobileMoreLink;
                    DescriptionField.Text = item.Description;
                    ExpireField.Text = item.ExpireDate.Value.ToShortDateString();
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
        // create or update an announcement.  It  uses the ASPNET.StarterKit.Portal.AnnouncementsDB()
        // data component to encapsulate all data functionality.
        //
        //****************************************************************

        protected void UpdateBtn_Click(Object sender, EventArgs e)
        {
            // Only Update if the Entered Data is Valid
            if (Page.IsValid)
            {
                if (itemId == 0)
                {
                    // Add the announcement within the Announcements table
                    AnnouncementsDB.AddAnnouncement(moduleId, Context.User.Identity.Name, TitleField.Text,
                                                    DateTime.Parse(ExpireField.Text), DescriptionField.Text,
                                                    MoreLinkField.Text,
                                                    MobileMoreField.Text);
                }
                else
                {
                    // Update the announcement within the Announcements table
                    AnnouncementsDB.UpdateAnnouncement(itemId, Context.User.Identity.Name, TitleField.Text,
                                                       DateTime.Parse(ExpireField.Text), DescriptionField.Text,
                                                       MoreLinkField.Text,
                                                       MobileMoreField.Text);
                }

                // Redirect back to the portal home page
                Response.Redirect((String) ViewState["UrlReferrer"]);
            }
        }

        //****************************************************************
        //
        // The DeleteBtn_Click event handler on this Page is used to delete an
        // an announcement.  It  uses the ASPNET.StarterKit.Portal.AnnouncementsDB()
        // data component to encapsulate all data functionality.
        //
        //****************************************************************

        protected void DeleteBtn_Click(Object sender, EventArgs e)
        {
            // Only attempt to delete the item if it is an existing item
            // (new items will have "ItemId" of 0)

            if (itemId != 0)
            {
                AnnouncementsDB.DeleteAnnouncement(itemId);
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