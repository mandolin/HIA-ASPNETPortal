using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class Contacts : PortalModuleControl<Contacts>
    {
        [Dependency]
        public IContactsDb ContactsDB { private get; set; }


        //*******************************************************
        //
        // The Page_Load event handler on this User Control is used to
        // obtain a DataReader of contact information from the Contacts
        // table, and then databind the results to a DataGrid
        // server control.  It uses the ASPNET.StarterKit.Portal.ContactsDB()
        // data component to encapsulate all data functionality.
        //
        //*******************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            // Obtain contact information from Contacts table
            // and bind to the Repeater control. The old field name is kept to avoid wider churn.
            myDataGrid.DataSource = ContactsDB.GetContacts(ModuleId);
            myDataGrid.DataBind();
        }
    }
}
