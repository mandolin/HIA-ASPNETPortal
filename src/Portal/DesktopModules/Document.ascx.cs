using System;
using Microsoft.Practices.Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class Document : PortalModuleControl<Document>
    {
        [Dependency]
        public IDocumentsDb DocumentDB { private get; set; }


        //*******************************************************
        //
        // The Page_Load event handler on this User Control is used to
        // obtain a SqlDataReader of document information from the 
        // Documents table, and then databind the results to a DataGrid
        // server control.  It uses the ASPNET.StarterKit.Portal.DocumentDB()
        // data component to encapsulate all data functionality.
        //
        //*******************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            // Obtain Document Data from Documents table
            // and bind to the datalist control
            myDataGrid.DataSource = DocumentDB.GetDocuments(ModuleId);
            myDataGrid.DataBind();
        }

        //*******************************************************
        //
        // GetBrowsePath() is a helper method used to create the url   
        // to the document.  If the size of the content stored in the   
        // database is non-zero, it creates a path to browse that.   
        // Otherwise, the FileNameUrl value is used.
        //
        // This method is used in the databinding expression for
        // the browse Hyperlink within the DataGrid, and is called 
        // for each row when DataGrid.DataBind() is called.  It is 
        // defined as a helper method here (as opposed to inline 
        // within the template) to improve code organization and
        // avoid embedding logic within the content template.
        //
        //*******************************************************

        protected string GetBrowsePath(String url, object size, int documentId)
        {
            if (size != DBNull.Value && (int) size > 0)
            {
                // if there is content in the database, create an 
                // url to browse it

                return "~/DesktopModules/ViewDocument.aspx?DocumentID=" + documentId;
            }
            else
            {
                // otherwise, return the FileNameUrl
                return url;
            }
        }
    }
}