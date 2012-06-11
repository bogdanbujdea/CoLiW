using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoLiW
{
    
    class Program
    {
        private static ApiManager _apiManager;

        [STAThread]
        static void Main(string[] args)
        {
            _apiManager = new ApiManager();
            _apiManager.FacebookClient.AppId = "372354616159806";
            _apiManager.FacebookClient.AppSecret = "5ccc6315874961c13249003ef9ed279f";
            while (true)
            {
                try
                {
                    Console.WriteLine("Type a command:");
                    string command = Console.ReadLine();
                    command = ToLower(command);
                    string[] parameters = command.Split(' ');
                    switch (parameters[0])
                    {
                        case "login":
                            Login(parameters);
                            break;
                        default:
                            Console.WriteLine("Wrong command. Type 'help' for a list of available commands");
                            break;
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
            }
        }

        private static void Login(string[] parameters)
        {
            if(parameters.Length < 2)
                throw new InvalidCommand("You forgot to specify the app");
            switch (parameters[1])
            {
                case "facebook":
                    if (parameters.Length == 4)
                        _apiManager.FacebookClient.Login(parameters[2], parameters[3], false);
                    else if(parameters.Length == 2)
                    {
                        _apiManager.FacebookClient.Login(false);
                    }
                    else throw new InvalidCommand("Wrong number of parameters");
                    break;
                default:
                    throw new InvalidCommand("Wrong command");
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
