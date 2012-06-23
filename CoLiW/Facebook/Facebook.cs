using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Windows.Forms;
using Facebook;

#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="{Faculty of Computer Science A. I. Cuza}">
//
// CoLiW is an eperiment for using web applications in the command line
// Copyright (C) 2012 Faculty of Computer Science A. I. Cuza
//
// This program is free software: you can redistribute it and/or modify
// it under the +terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/. 
// </copyright>
// <summary>
// Email: coliw@gmail.com
// </summary>
// -------------------------------------------------------------------------------------------------------------------

#endregion

namespace CoLiW
{
    public class Facebook : FacebookClient, IWebApp
    {
        public FacebookLoginForm FbLoginForm;

        public Uri LogoutUri { get; set; }

        public Facebook()
        {
            FbLoginForm = new FacebookLoginForm();
            FbLoginForm.Browser.Navigated += BrowserNavigated;
        }
        
        public Facebook(string appId, string appSecret)
        {
            AppId = appId;
            AppSecret = appSecret;
            FbLoginForm = new FacebookLoginForm();
            FbLoginForm.Browser.Navigated += BrowserNavigated;

        }

        public List<string> GetUpdates(string username)
        {
            var query = string.Format("SELECT message FROM status WHERE uid={0}", GetUserDetails(username).Id);

            dynamic parameters = new ExpandoObject();
            parameters.q = query;
            dynamic results = Get("/fql", parameters);
            return results;
        }

        public bool IsLoggedIn()
        {
            try
            {
                FbLoginForm.IsLoggedIn = true; //assume that the user is logged in
                UserDetails me = GetUserDetails("me"); //try to get his name
                return true;
            }
            catch (Exception exception) //if there is an exception, then the user is logged out
            {
                if (exception is FacebookOAuthException && exception.Message.Contains("#2500")) //especially if the exception is an oauthexception
                {
                    return true;
                }
                else return false; //but if there is another exception, then login
            }
        }

        public bool Login(bool forceLogin)
        {
            if (FbLoginForm.IsLoggedIn && forceLogin == false)
                //if the user is logged in and force login = false, then exit
                return true;
            if (forceLogin && FbLoginForm.IsLoggedIn)
                //if the user is logged in, and forcelogin = true, then logout first
                Logout();
            FbLoginForm.Browser.Navigate(GetLoginUri("publish_stream, user_photos"));
            if (FbLoginForm.ShowDialog() == DialogResult.No)
                return false;
            return true;
        }

        public bool Logout()
        {
            LogoutUri =
                    GetLogoutUrl(
                        new
                        {
                            access_token = AccessToken,
                            next = "https://www.facebook.com/connect/login_success.html"
                        });
            FbLoginForm.Browser.Navigate(LogoutUri);
            FbLoginForm.ShowDialog();
            FbLoginForm.IsLoggedIn = false;
            return false;
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

        public bool PostMessage(string username, PostDetails details)
        {
            try
            {
                var args = new Dictionary<string, object>();
                args["name"] = details.Name ?? string.Empty;
                args["name"] = details.Name ?? string.Empty;
                args["link"] = details.Link ?? string.Empty;
                args["caption"] = details.Caption ?? string.Empty;
                args["description"] = details.Description ?? string.Empty;
                args["picture"] = details.PictureUrl ?? string.Empty;
                args["message"] = details.Message ?? string.Empty;
                args["actions"] = "";

                Post("/" + username + "/feed", args);
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
            return false;
        }

        public bool Login(string appId, string appSecret, bool forceLogin)
        {
            try
            {
                AppId = appId;
                AppSecret = appSecret;

                return Login(forceLogin);
            }
            catch
            {
                return false;
            }
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
            var mediaObject = new FacebookMediaObject
            {
                FileName = photoDetails.FacebookName,
                ContentType = "image/jpeg"
            };
            byte[] fileBytes = photoDetails.ImageStream;
            mediaObject.SetValue(fileBytes);
            IDictionary<string, object> upload = new Dictionary<string, object>();
            upload.Add("name", photoDetails.FacebookName);
            upload.Add("message", photoDetails.PhotoDescription);
            upload.Add("@file.jpg", mediaObject);
            Post("/" + photoDetails.AlbumId + "/photos", upload);
            return true;
        }

        public bool CreateAlbum(AlbumDetails album)
        {
            var albumParameters = new Dictionary<string, object>
                                      {
                                          {"message", album.AlbumDescription},
                                          {"name", album.AlbumName}
                                      };
            var result = Post("/me/albums", albumParameters) as JsonObject;

            return true;
        }

        public Image GetUrlImage(string url, string path)
        {
            WebResponse result = null;
            Image rImage = null;
            try
            {
                WebRequest request = WebRequest.Create(url);
                result = request.GetResponse();
                Stream stream = result.GetResponseStream();
                if (stream != null)
                {
                    var br = new BinaryReader(stream);
                    byte[] rBytes = br.ReadBytes(1000000);
                    br.Close();
                    result.Close();
                    var imageStream = new MemoryStream(rBytes, 0, rBytes.Length);
                    imageStream.Write(rBytes, 0, rBytes.Length);
                    rImage = Image.FromStream(imageStream, true);
                    File.WriteAllBytes(path, rBytes);
                    imageStream.Close();
                }
            }
            catch { }
            finally
            {
                if (result != null) result.Close();
            }
            return rImage;

        }

        public List<AlbumDetails> GetAlbums(string username)
        {
            List<AlbumDetails> albumDetails = new List<AlbumDetails>();
            dynamic albums = Get("/" + username + "/albums");
            foreach (dynamic albumInfo in albums.data)
            {
                AlbumDetails album = new AlbumDetails();
                if(albumInfo.name != null)
                    album.AlbumName = albumInfo.name;
                if(albumInfo.description != null)
                    album.AlbumDescription = albumInfo.description;
                if(albumInfo.id != null)
                    album.Id = albumInfo.id;
                if(albumInfo.count != null)
                    album.PhotoCount = albumInfo.count;
                if(albumInfo.can_upload != null)
                    album.CanUpload = albumInfo.can_upload;

                albumDetails.Add(album);
            }
            return albumDetails;
        }

        public UserDetails GetUserDetails(string userName)
        {
            if(FbLoginForm.IsLoggedIn == false)
                throw new FacebookOAuthException("The user is not logged in");

            dynamic user = Get(userName);
            var userDetails = new UserDetails();
            if(user.name != null)
                userDetails.Name = (string) user.name;
            if (user.first_name != null)
                userDetails.FirstName = (string) user.first_name;
            if (user.last_name != null)
                userDetails.LastName = (string) user.last_name;
            if (user.link != null)
                userDetails.ProfileUrl = (string) user.link;
            if (user.username != null)
                userDetails.Username = (string) user.username;
            if (user.gender != null)
                userDetails.Gender = (string) user.gender;
            if (user.id != null)
                userDetails.Id = (string) user.id;
            return userDetails;
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
                FbLoginForm.IsLoggedIn = false;
                FbLoginForm.Close();
            }
        }
    }
}