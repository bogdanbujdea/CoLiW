using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Facebook;
using System.Windows.Forms;

namespace CoLiW
{
    public class Facebook:FacebookClient
    {

        public FacebookLoginForm FbLoginForm;

        public bool Login()
        {

            return false;
        }

        public bool Login(string appId, string appSecret)
        {
            return false;
        }

        public bool AddPhoto(PhotoDetails photoDetails)
        {
            return false;
        }

        public UserDetails GetUserDetails(string userName)
        {
            return null;
        }

        public bool CreateAlbum(AlbumDetails albumDetails)
        {
            return false;
        }

        public bool Logout()
        {
            return false;
        }

        Facebook()
        {
            FbLoginForm.Browser.Navigated +=Browser_Navigated;
        }


        void Browser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {

        }
    }
}
