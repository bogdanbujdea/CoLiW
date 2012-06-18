using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using TweetSharp;

namespace CoLiW
{
    public class Twitter : TwitterService
    {
        public Twitter()
        {

        }

        public Twitter(string consumerKey, string consumerSecret)
            : base(consumerKey, consumerSecret)
        {
            ConsumerKey = consumerKey;
            ConsumerSecret = consumerSecret;
            LoginForm = new TwitterLoginForm();
            LoginForm.Browser.Navigated += BrowserNavigated;

        }

        void BrowserNavigated(object sender, System.Windows.Forms.WebBrowserNavigatedEventArgs e)
        {
            var htmlString = LoginForm.Browser.DocumentText;
            Regex expression = new Regex(@"<code>(?<word>\w+)</code>");
            Match match = expression.Match(htmlString);
            Pin = match.Groups["word"].Value;
            if (!string.IsNullOrEmpty(Pin) && Pin.Length == 7)
            {
                LoginForm.DialogResult = DialogResult.OK;
                LoginForm.Close();

            }
        }

        public TwitterLoginForm LoginForm { get; set; }

        public string ConsumerKey { get; set; }

        public string ConsumerSecret { get; set; }

        public string AccessToken { get; set; }

        public string Pin { get; set; }

        public bool Login(bool forcedLogin)
        {
            try
            {
                if (LoginForm.IsLoggedIn && forcedLogin == false)
                    //if the user is logged in and force login = false, then exit
                    return true;
                if (forcedLogin && LoginForm.IsLoggedIn)
                    //if the user is logged in, and forcelogin = true, then logout first
                    Logout();

                if (Token != null && TokenSecret != null)
                {
                    AuthenticateWith(Token, TokenSecret);
                    return true;
                }
                // Step 1 - Retrieve an OAuth Request Token
                OAuthRequestToken requestToken = GetRequestToken("oob");
                
                // Step 2 - Redirect to the OAuth Authorization URL
                Uri uri = GetAuthorizationUri(requestToken);
                LoginForm.Browser.Navigate(uri);
                //Console.WriteLine("A 7-digit code will appear in the window after you authorize the app.\nPlease copy that code, close the window, and insert it in this console and hit <Enter> key:");
                DialogResult loginResult = LoginForm.ShowDialog();
                if (loginResult != DialogResult.OK)
                {
                    Console.WriteLine("Login was unsuccesful");
                    return false;
                }
                OAuthAccessToken access = GetAccessToken(requestToken, Pin);
                AuthenticateWith(access.Token, access.TokenSecret); Token = access.Token;
                TokenSecret = access.TokenSecret;
                Token = access.Token;
                return true;
            }
            catch (Exception exception)
            {
                throw new InvalidCredentialException(exception.ToString());
            }
        }

        private void Logout()
        {

        }

        public string TokenSecret { get; set; }

        public string Token { get; set; }

        public bool Login(string consumerId, string consumerSecret, bool forcedLogin)
        {
            return false;
        }

        public TwitterStatus GetRetweet(int index)
        {
            IEnumerable<TwitterStatus> listRetweetsByMe = ListRetweetsByMe();
            if(listRetweetsByMe == null)
                throw new NullReferenceException("There are no retweets");
            int count = listRetweetsByMe.Count();
            if(index < 0 || index > count)
                throw new IndexOutOfRangeException("There are only " + listRetweetsByMe.Count());
            return listRetweetsByMe.ElementAt(index);
        }
    
        public bool Follow(string username)
        {
            string url = "https://api.twitter.com/1/friendships/create.json";
            return true;
        }
    }
}
