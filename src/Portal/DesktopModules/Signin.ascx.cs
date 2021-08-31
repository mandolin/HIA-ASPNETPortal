using System;
using System.Web.Security;
using System.Web.UI;
using Microsoft.Practices.Unity;
using Resources;

namespace ASPNET.StarterKit.Portal
{
    public partial class Signin : PortalModuleControl<Signin>
    {
        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        protected void LoginBtn_Click(Object sender, ImageClickEventArgs e)
        {
            // Attempt to Validate User Credentials using UsersDB
            string userId = UsersDB.Login(email.Text, PortalSecurity.Encrypt(password.Text));

            if (!string.IsNullOrEmpty(userId))
            {
                // Use security system to set the UserID within a client-side Cookie
                FormsAuthentication.SetAuthCookie(email.Text, RememberCheckbox.Checked);

                // Redirect browser back to originating page
                Response.Redirect(Request.ApplicationPath);
            }
            else
            {
                Message.Text = string.Format("<br>{0}<br/>", lang.Signin_LoginFaild);
            }
        }
    }
}