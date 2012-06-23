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
using System;
using System.Collections.Generic;
using System.IO;
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
            try
            {
                var newPost = new AtomEntry();
                newPost.Title.Text = postInfo.Title;
                newPost.Content = new AtomContent();
                newPost.Content.Content = postInfo.Content;
                newPost.Content.Type = "xhtml";
                newPost.IsDraft = postInfo.IsDraft;
                if(newPost.Content.Content.StartsWith("<div xmlns='http://www.w3.org/1999/xhtml'>") == false)
                {
                    newPost.Content.Content = newPost.Content.Content.Insert(0, "<div xmlns='http://www.w3.org/1999/xhtml'>");
                    newPost.Content.Content = newPost.Content.Content += "</div>";
                }
                newPost.Content.Content = newPost.Content.Content.Replace("&rsquo;", "'");
                File.WriteAllText("c:\\tmp.html", newPost.Content.Content);
                var blogFeedUri = new Uri("http://www.blogger.com/feeds/" + postInfo.BlogId + "/posts/default");
                service.Insert(blogFeedUri, newPost);

                return true;
            }
            catch (Exception exception)
            {
                if(exception is GDataRequestException)
                {
                    Console.WriteLine("The html is not valid: " + (exception as GDataRequestException).ResponseString);
                }
                return false;
            }
        }
    }
}
