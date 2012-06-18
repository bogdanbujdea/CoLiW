using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TweetSharp;

namespace CoLiW
{
    public partial class TwitterLoginForm : Form
    {
        public TwitterLoginForm()
        {
            InitializeComponent();

            Browser = browser;
        }

        public WebBrowser Browser { get; set; }

        public bool IsLoggedIn { get; set; }

        

    }
}
