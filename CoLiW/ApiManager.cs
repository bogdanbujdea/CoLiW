using System.Windows.Forms;
using Facebook;

namespace CoLiW
{
    public class ApiManager
    {
        public ApiManager()
        {
           
        }

        

        public Facebook FacebookClient { get; set; }

        public Twitter TwitterClient { get; set; }

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