using System;
using Microsoft.Practices.Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class Announcements : PortalModuleControl<Announcements>
    {
        [Dependency]
        public IAnnouncementsDb AnnouncementsDB { private get; set; }

        //*******************************************************
        //
        // The Page_Load event handler on this User Control is used to
        // obtain a DataSet of announcement information from the Announcements
        // table, and then databind the results to a templated DataList
        // server control.  It uses the ASPNET.StarterKit.Portal.AnnouncementsDB()
        // data component to encapsulate all data functionality.
        //
        //*******************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            // Obtain announcement information from Announcements table
            // and bind to the datalist control
            // DataBind Announcements to DataList Control
            myDataList.DataSource = AnnouncementsDB.GetAnnouncements(ModuleId);
            myDataList.DataBind();
        }
    }
}