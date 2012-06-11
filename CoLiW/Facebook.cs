using System;
using System.Dynamic;
using System.Security.Authentication;
using System.Windows.Forms;
using Facebook;

namespace CoLiW
{
    public class Facebook : FacebookClient
    {
        public FacebookLoginForm FbLoginForm;


        public Facebook()
        {
            FbLoginForm = new FacebookLoginForm();
            FbLoginForm.Browser.Navigated += BrowserNavigated;
        }

        public bool Login(bool forceLogin)
        {
            if (FbLoginForm.IsLoggedIn && forceLogin == false)
                //if the user is logged in and force login = false, then exit
                return true;
            if (forceLogin && FbLoginForm.IsLoggedIn)
                //if the user is logged in, and forcelogin = true, then logout first
                Logout();
            FbLoginForm.Browser.Navigate(GetLoginUri("publish_stream"));
            if (FbLoginForm.ShowDialog() == DialogResult.No)
                return false;
            return true;
        }

        public Uri GetLoginUri(string extendedPermissions)
        {
            try
            {
                dynamic parameters = new ExpandoObject();
                parameters.client_id = AppId;
                parameters.redirect_uri = "https://www.facebook.com/connect/login_success.html";

                // The requested response: an access token (token), an authorization code (code), or both (code token).
                parameters.response_type = "token";

                // list of additional display modes can be found at http://developers.facebook.com/docs/reference/dialogs/#display
                parameters.display = "popup";

                // add the 'scope' parameter only if we have extendedPermissions.
                if (!string.IsNullOrWhiteSpace(extendedPermissions))
                    parameters.scope = extendedPermissions;

                // generate the login url

                Uri loginUri = GetLoginUrl(parameters);
                return loginUri;
            }
            catch (Exception exception)
            {
                return new Uri(exception.ToString());
            }
        }


        public bool Login(string appId, string appSecret, bool forceLogin)
        {
            try
            {
                ValidateLoginCredentials(appId, appSecret);

                AppId = appId;
                AppSecret = appSecret;

                Login(forceLogin);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private bool ValidateLoginCredentials(string appId, string appSecret)
        {
            try
            {
                if (appId.Length != 15)
                    throw new InvalidCredentialException("The application id doesn't have a length of 15 characters");
                for (int i = 'A'; i < 'z'; i++)
                {
                    string iChar = "";
                    iChar += (char) i;
                    if (appId.Contains(iChar))
                    {
                        throw new InvalidCredentialException("The application id contains invalid characters");
                    }
                }
                if (appSecret.Length != 32)
                    throw new InvalidCredentialException("The application secret doesn't have a length of 32 characters");
                return true;
            }
            catch (Exception exception)
            {
                if (exception is InvalidCredentialException)
                    return false;
                throw new Exception("Something went wrong when validating your credentials");
            }
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


        private void BrowserNavigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            if(FbLoginForm.IsLoggedIn == false)
            {
                FacebookOAuthResult result;
                if(TryParseOAuthCallbackUrl(e.Url, out result))
                {
                    AccessToken = result.AccessToken;
                    FbLoginForm.DialogResult = DialogResult.OK;
                    FbLoginForm.IsLoggedIn = true;
                    FbLoginForm.Close();
                }
            }
            else
            {
                FbLoginForm.Close();
            }
        }
    }
}