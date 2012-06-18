using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CoLiW
{
    [TestFixture]
    class FacebookTests
    {
        [Test]
        [STAThread]
        public void TestLoginParametersWithCorrectValues()
        {
            Facebook fb = new Facebook();
            bool success = fb.Login("372354616159806", "5ccc6315874961c13249003ef9ed279f", false);
            Assert.That(success == true);
        }

        [Test]
        [STAThread]
        public void TestLoginParametersWithIncorrectValues()
        {
            Facebook fb = new Facebook();
            try
            {

                bool success = fb.Login("37235461H159806", "5ccc6315874961c13249003ef9ed279f", false);
                Assert.That(success == true);
            }
            catch
            {
            }
        }
    }
}
