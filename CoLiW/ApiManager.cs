using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Facebook;

namespace CoLiW
{
    public class ApiManager
    {
        public ApiManager()
        {
            BloggerClient = new Blogger();
            AliasesPath = "aliases.xml";
            SettingsPath = "settings.xml";
        }

        public Facebook FacebookClient { get; set; }

        public string AliasesPath { get; private set; }

        public string SettingsPath { get; private set; }

        public Dictionary<String, HashSet<String>> Aliases { get; set; }

        public Twitter TwitterClient { get; set; }

        public Blogger BloggerClient { get; set; }

        public void ProcessMessage(string message)
        {
            
        }

        public void InitializeFacebook(string appId, string appSecret)
        {
            FacebookClient = new Facebook(appId, appSecret);
        }

        public void InitializeTwitter(string consumerId, string consumerSecret)
        {
            TwitterClient = new Twitter(consumerId, consumerSecret);
        }
    }
}