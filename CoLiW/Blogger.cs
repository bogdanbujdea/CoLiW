using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.GData.Blogger;
using Google.GData.Client;

namespace CoLiW
{
    public class Blogger : IWebApp
    {
        private Service service;
        private string _email;
        private string _password;

        public bool IsLoggedIn { get; set; }

        public Blogger(string email, string password)
        {
            _email = email;
            _password = password;
            IsLoggedIn = false;
        }

        public Blogger()
        {
            IsLoggedIn = false;
        }

        public Blog Blog { get; set; }

        public string AppName { get; set; }

        public string Email { get; set; }

        public bool Login(string email, string password)
        {
            _email = email;
            _password = password;
            return Login();
        }

        public bool Login()
        {
            service = new Service("blogger", "coliwbbe");
            service.Credentials = new GDataCredentials(_email, _password);
            var factory = (GDataGAuthRequestFactory)service.RequestFactory;
            factory.AccountType = "GOOGLE";
            IsLoggedIn = true;
            return true;
        }

        public AtomEntryCollection GetListOfBlogs()
        {
            var query = new FeedQuery();
            query.Uri = new Uri("http://www.blogger.com/feeds/default/blogs");
            AtomFeed feed = null;

            feed = service.Query(query);
            return feed.Entries;
        }

        public bool CreatePost(PostInfo postInfo)
        {
            var newPost = new AtomEntry();
            newPost.Title.Text = postInfo.Title;
            newPost.Content = new AtomContent();
            newPost.Content.Content = postInfo.Content;
            newPost.Content.Type = "xhtml";
            newPost.IsDraft = postInfo.IsDraft;

            var blogFeedUri = new Uri("http://www.blogger.com/feeds/" + postInfo.BlogId + "/posts/default");
            service.Insert(blogFeedUri, newPost);
            
            return true;
        }
    }
}
