using System;
using Microsoft.Practices.Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class ViewDocument : PortalPage<ViewDocument>
    {
        private int documentId = -1;

        [Dependency]
        public IDocumentsDb DocumentDB { private get; set; }

        //*******************************************************
        //
        // The Page_Load event handler on this Page is used to
        // obtain obtain the contents of a document from the 
        // Documents table, construct an HTTP Response of the
        // correct type for the document, and then stream the 
        // document contents to the response.  It uses the 
        // ASPNET.StarterKit.Portal.DocumentDB() data component to encapsulate 
        // the data access functionality.
        //
        //*******************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.Params["DocumentId"] != null)
            {
                documentId = Int32.Parse(Request.Params["DocumentId"]);
            }

            if (documentId != -1)
            {
                // Obtain Document Data from Documents table
                IDocumentItemDetails item = DocumentDB.GetDocumentContent(documentId);
                
                // Serve up the file by name
                Response.AppendHeader("content-disposition", "filename=" + item.FileNameUrl);

                // set the content type for the Response to that of the 
                // document to display.  For example. "application/msword"
                Response.ContentType = item.ContentType;

                // output the actual document contents to the response output stream
                Response.OutputStream.Write(item.Content, 0, item.ContentSize.Value);

                // end the response
                Response.End();
            }
        }
    }
}