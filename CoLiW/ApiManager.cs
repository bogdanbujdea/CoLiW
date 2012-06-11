using System.Windows.Forms;
using Facebook;

namespace CoLiW
{
    public class ApiManager
    {
        public ApiManager()
        {
            FacebookClient = new Facebook();
        }

        

        public Facebook FacebookClient { get; set; }

        public void ProcessMessage(string message)
        {
            
        }
    }
}