using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Facebook;

namespace CoLiW
{
    internal class Program
    {
        private static ApiManager _apiManager;

        [STAThread]
        private static void Main(string[] args)
        {
            _apiManager = new ApiManager();
            _apiManager.InitializeFacebook("372354616159806", "5ccc6315874961c13249003ef9ed279f");
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
                        case "test":
                            _apiManager.FacebookClient.GetAlbums("bogdanbujdea");
                            break;
                        case "login":
                            Login(parameters);
                            break;
                        case "logout":
                            Logout(parameters);
                            break;
                        case "get":
                            Console.WriteLine(Get(parameters));
                            break;
                        case "post":
                            Console.WriteLine(Post(parameters));
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

        private static string Post(string[] parameters)
        {
            if (parameters.Length < 2)
                throw new InvalidCommand("Not enough parameters");
            switch (parameters[1])
            {
                case "facebook":
                    return PostFacebook(parameters);
                default:
                    throw new InvalidCommand(string.Format("The app {0} doesn't exist", parameters[1]));
                    break;
            }
            return "";
        }

        private static string PostFacebook(string[] parameters)
        {
            try
            {
                string username;
                if (parameters.Contains("-u") == false)
                {
                    username = "me";
                }
                else
                {
                    var param = new List<string>(parameters);
                    int index = param.IndexOf("-u") + 1;
                    if (index < 0 || index > param.Count)
                        throw new InvalidCommand(
                            "Command not formated properly. Example: 'post facebook name -u johnsmith'"); //needs to be changed
                    username = param.ElementAt(index);
                }
                var photoDetails = new PhotoDetails();

                for (int i = 2; i < parameters.Length; i++)
                {
                    switch (parameters[i])
                    {
                        case "fn":
                            photoDetails.FacebookName = parameters[i + 1];
                            break;
                        case "pd":
                            photoDetails.PhotoDescription = parameters[i + 1];
                            break;
                        case "aid":
                            photoDetails.AlbumId = parameters[i + 1];
                            break;
                        case "ad":
                            photoDetails.AlbumDescription = parameters[i + 1];
                            break;
                        case "an":
                            photoDetails.AlbumName = parameters[i + 1];
                            break;
                        case "path":
                            photoDetails.ImageStream = File.ReadAllBytes(parameters[i + 1]);
                            break;
                    }
                   
                }
                if (string.IsNullOrEmpty(photoDetails.AlbumId))
                {
                    photoDetails.AlbumId = ChooseAlbumId(username);
                }
                _apiManager.FacebookClient.AddPhoto(photoDetails);
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
            return "";
        }

        private static string ChooseAlbumId(string username)
        {
            var albums = _apiManager.FacebookClient.GetAlbums(username);

            int i = 1;
            foreach (AlbumDetails album in albums)
            {
                Console.WriteLine(String.Format("{0}. {1}", i, album.AlbumName));
                i++;
            }

            Console.WriteLine("Type a number:");
            string selectedAlbum = Console.ReadLine();

            Int32.TryParse(selectedAlbum, out i);
            return albums[i - 1].Id;
        }

        private static string Get(string[] parameters)
        {
            if (parameters.Length < 2)
                throw new InvalidCommand("Not enough parameters");
            switch (parameters[1])
            {
                case "facebook":
                    return GetFacebook(parameters);
                    break;
                default:
                    throw new InvalidCommand(string.Format("The app {0} doesn't exist", parameters[1]));
            }
        }

        private static string GetFacebook(string[] parameters)
        {
            try
            {
                string username = "me";
                if (parameters.Contains("-u") == false)
                {
                    username = "me";
                }
                else
                {
                    var param = new List<string>(parameters);
                    int index = param.IndexOf("-u") + 1;
                    if (index < 0 || index > param.Count)
                        throw new InvalidCommand(
                            "Command not formated properly. Example: 'get facebook name -u johnsmith'");
                    username = param.ElementAt(index);
                }

                switch (parameters[2])
                {
                    case "name":
                        return _apiManager.FacebookClient.GetUserDetails(username).Name;
                    case "first_name":
                        return _apiManager.FacebookClient.GetUserDetails(username).FirstName;
                    case "last_name":
                        return _apiManager.FacebookClient.GetUserDetails(username).LastName;
                    case "gender":
                        return _apiManager.FacebookClient.GetUserDetails(username).Gender;
                    case "id":
                        return _apiManager.FacebookClient.GetUserDetails(username).Id;
                    case "profile_url":
                        return _apiManager.FacebookClient.GetUserDetails(username).ProfileUrl;
                    default:
                        throw new InvalidCommand(
                            "Invalid request. You can get name, first name, last name, gender, id and profile Url");
                }
            }
            catch (Exception exception)
            {
                if (exception is FacebookOAuthException && exception.Message.Contains("#2500"))
                {
                    _apiManager.FacebookClient.Login(true);
                    return GetFacebook(parameters);
                }
                throw;
            }
        }

        private static void Logout(string[] parameters)
        {
            if (parameters.Length < 2)
                throw new InvalidCommand("You forgot to specify the app");
            try
            {
                switch (parameters[1])
                {
                    case "facebook":
                        _apiManager.FacebookClient.Logout();
                        break;
                    default:
                        throw new InvalidCommand("Invalid app selected for logout.");
                        break;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        private static void Login(string[] parameters)
        {
            if (parameters.Length < 2)
                throw new InvalidCommand("You forgot to specify the app");
            switch (parameters[1])
            {
                case "facebook":
                    if (parameters.Length == 4)
                        _apiManager.FacebookClient.Login(parameters[2], parameters[3], false);
                    else if (parameters.Length == 2)
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
            for (int i = 0; i < command.Length; i++)
            {
                newCommand += Char.ToLower(command[i]);
            }
            return newCommand;
        }
    }
}