#region License
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Twitter.cs" company="Faculty of Computer Science A. I. Cuza">
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Twitterizer;
using TwitterUser = Twitterizer.TwitterUser;

namespace CoLiW
{
    public class Twitter : IWebApp
    {
        public Twitter()
        {
            LoginForm = new TwitterLoginForm();
            LoginForm.Browser.Navigated += BrowserNavigated;
            Tokens = new OAuthTokens();
        }

        public Twitter(string consumerKey, string consumerSecret)
        {
            ConsumerKey = consumerKey;
            ConsumerSecret = consumerSecret;
            LoginForm = new TwitterLoginForm();
            LoginForm.Browser.Navigated += BrowserNavigated;
            Tokens = new OAuthTokens {ConsumerKey = consumerKey, ConsumerSecret = consumerSecret};
        }

        public OAuthTokens Tokens { get; set; }

        public string AccessToken { get; set; }

        public string ConsumerKey { get; set; }

        public string ConsumerSecret { get; set; }

        public string ScreenName { get; set; }

        public decimal UserId { get; set; }

        public string Pin { get; set; }

        public TwitterLoginForm LoginForm { get; set; }

        void BrowserNavigated(object sender, WebBrowserNavigatedEventArgs e)
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

        public bool Logout()
        {
            LoginForm.IsLoggedIn = false;
            return true;
        }

        public bool Unfollow(string username)
        {
            var response = TwitterFriendship.Delete(Tokens, username);

            if (response.Result == RequestResult.Success)
                return true;
            throw new TwitterizerException(response.ErrorMessage);
        }

        public bool SetBackgroundImage(string path)
        {
                TwitterResponse<TwitterUser> response = TwitterAccount.UpdateProfileBackgroundImage(Tokens, File.ReadAllBytes(path));

                if (response.Result == RequestResult.Success)
                    return true;
                throw new InvalidCommand(response.ErrorMessage);
        }

        public bool Login(string consumerKey, string consumerSecret, bool forcedLogin)
        {
            ConsumerKey = consumerKey;
            ConsumerSecret = consumerSecret;
            return Login(forcedLogin);
        }

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
                if (Tokens.HasBothTokens)
                {
                    TwitterResponse<TwitterUser> response = TwitterAccount.VerifyCredentials(Tokens);
                    if(response != null && response.ResponseObject != null)
                    {
                        UserId = response.ResponseObject.Id;
                        ScreenName = response.ResponseObject.ScreenName;
                        LoginForm.IsLoggedIn = true;
                        return true;
                    }
                }
                // Step 1 - Retrieve an OAuth Request Token
                string token = OAuthUtility.GetRequestToken(ConsumerKey, ConsumerSecret, "oob").Token;

                // Step 2 - Redirect to the OAuth Authorization URL
                Uri uri = OAuthUtility.BuildAuthorizationUri(token);
                LoginForm.Browser.Navigate(uri);

                DialogResult loginResult = LoginForm.ShowDialog();
                if (loginResult != DialogResult.OK)
                {
                    Console.WriteLine("Login was unsuccesful");
                    return false;
                }
                OAuthTokenResponse accessToken = OAuthUtility.GetAccessToken(ConsumerKey, ConsumerSecret, token, Pin);

                Tokens.AccessToken = accessToken.Token;
                Tokens.AccessTokenSecret = accessToken.TokenSecret;
                AccessToken = accessToken.Token + ":" + accessToken.TokenSecret;
                ScreenName = accessToken.ScreenName;
                UserId = accessToken.UserId;
                LoginForm.IsLoggedIn = true;
                return true;
            }
            catch (Exception exception)
            {
                throw new InvalidCredentialException(exception.ToString());
            }
        }

        public bool Follow(string username)
        {
            var response = Twitterizer.TwitterFriendship.Create(Tokens, username);

            if(response.Result == RequestResult.Success)
                return true;
            throw new TwitterizerException(response.ErrorMessage);
        }

        public List<string> GetLastTweets(int nr)
        {
            UserTimelineOptions options = new UserTimelineOptions();
            options.ScreenName = ScreenName;
            TwitterStatusCollection tweets = TwitterTimeline.UserTimeline(options).ResponseObject;
            List<String> tweetList = new List<string>();
            int i = 0;
            foreach (var tweet in tweets)
            {
                tweetList.Add(tweet.Text);
                if (++i == nr)
                    break;
            }
            return tweetList;
        }

        public TwitterUser GetUserDetails(string username)
        {
            TwitterResponse<TwitterUser> twitterResponse = TwitterUser.Show(username);
            if (twitterResponse.Result == RequestResult.Success)
                return twitterResponse.ResponseObject;
            throw new InvalidCommand(twitterResponse.ErrorMessage);
        }

        public bool SendMessage(string text, string username)
        {
            if(username == null || text == null)
                throw new InvalidCommand("You must specify a username and a text message");
            if (text.Length > 140)
                throw new InvalidCommand("The limit for direct messages is of 140 characters, your message has " + text.Length);
            var response = TwitterDirectMessage.Send(Tokens, username, text);
            if (response.Result == RequestResult.Success)
                return true;
            return false;
        }

        public bool UpdateStatus(string text, string path)
        {
            //IAsyncResult result = TwitterStatusAsync.UpdateWithMedia(Tokens, "Salut", File.ReadAllBytes(path), new TimeSpan(1, 0, 0), delegate(TwitterAsyncResponse<TwitterStatus> asyncResponse) { Console.WriteLine("Status updated"); });
            TwitterResponse<TwitterStatus> response = null;
            if (text.Length > 140)
                throw new InvalidCommand("The limit for tweets is of 140 characters, your tweet has " + text.Length);
            if (path == null)
                response = TwitterStatus.Update(Tokens, text);
            else
                response = TwitterStatus.UpdateWithMedia(Tokens, text, File.ReadAllBytes(path));
            if (response.Result == RequestResult.Success)
                return true;
            return false;
        }

        public bool UpdateProfilePicture(string path)
        {
            var response = TwitterAccount.UpdateProfileImage(Tokens, File.ReadAllBytes(path));
            
            if (response.Result == RequestResult.Success)
                return true;
            return false;
        }
    }
}
