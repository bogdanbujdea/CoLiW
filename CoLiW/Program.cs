using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoLiW
{
    class Program
    {

        static void Main(string[] args)
        {
            var apiManager = new ApiManager();
            while (true)
            {
                Console.WriteLine("Type a command:");
                string command = Console.ReadLine();
                command = ToLower(command);
                switch (command)
                {
                    case "login Facebook":
                        apiManager.FacebookClient.Login();
                        break;

                }
            }
        }

        private static string ToLower(string command)
        {
            string newCommand = null;
            for(int i = 0; i < command.Length; i++)
            {
                newCommand += Char.ToLower(command[i]);
            }
            return newCommand;
        }
    }
}
