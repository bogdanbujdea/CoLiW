using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CoLiW
{
    public partial class FacebookLoginForm : Form
    {
        public FacebookLoginForm()
        {
            InitializeComponent();
            Browser = webBrowser;
        }

        

        public WebBrowser Browser { get; set; }


    }
}
