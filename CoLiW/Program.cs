using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
                    string[] parameters = ParseArguments(command);
                    switch (parameters[0])
                    {
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

        public static String[] ParseArguments(string args)
        {
            List<String> parameters = new List<string>();
            bool add = false;
            string parameter = null;
            foreach (char c in args)
            {
                if (c == '\"')
                    add = !add;
                if (c == ' ')
                    if (add == false)
                    {
                        parameters.Add(parameter);
                        parameter = string.Empty;
                        continue;
                    }
                parameter += c;
            }
            parameters.Add(parameter);
            return parameters.ToArray();
        }

        private static bool Post(string[] parameters)
        {
            if (parameters.Length < 2)
                throw new InvalidCommand("Not enough parameters");
            switch (parameters[1])
            {
                case "facebook":
                    return PostFacebook(parameters);
                default:
                    throw new InvalidCommand(string.Format("The app {0} doesn't exist", parameters[1]));
            }
        }

        private static bool PostFacebook(string[] parameters)
        {
            try
            {
                string username = null;
                foreach (string parameter in parameters)
                {
                    if (parameter.Contains("-u"))
                    {
                        var p = parameter.Split(':');
                        if (p.Length < 2)
                            username = "me";
                        else
                            username = p[1];
                        break;
                    }
                }


                switch (parameters[2])
                {
                    case "message":
                        return PostFacebookMessage(parameters, username);
                    case "photo":
                        return PostFacebookPhoto(parameters, username);
                    case "album":
                        return PostFacebookAlbum(parameters, username);
                    case "status":
                        return PostFacebookStatus(parameters, username);
                }




            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private static bool PostFacebookMessage(string[] parameters, string username)
        {
            try
            {
                var details = new PostDetails();
                Dictionary<string, string> options = new Dictionary<string, string>();

                for (int i = 3; i < parameters.Length; i++)
                {
                    string[] keyvalue = parameters[i].Split(':');

                    if (keyvalue.Length > 2)
                        keyvalue[1] += ":" + keyvalue[2];
                    if (keyvalue.Length < 2)
                        return false;
                    keyvalue[1] = keyvalue[1].Trim('\"');
                    options[keyvalue[0]] = keyvalue[1];
                }

                foreach (KeyValuePair<string, string> keyValuePair in options)
                {
                    switch (keyValuePair.Key)
                    {
                        case "-m":
                            details.Message = keyValuePair.Value;
                            break;
                        case "-n":
                            details.Name = keyValuePair.Value;
                            break;
                        case "-c":
                            details.Caption = keyValuePair.Value;
                            break;
                        case "-l":
                            details.Link = keyValuePair.Value;
                            break;
                        case "-p":
                            details.PictureUrl = keyValuePair.Value;
                            break;
                        case "-d":
                            details.Description = keyValuePair.Value;
                            break;
                        case "-u":
                            break;
                        default:
                            throw new InvalidCommand("Invalid option: " + keyValuePair.Key);
                    }
                }
                _apiManager.FacebookClient.PostMessage(username, details);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool PostFacebookAlbum(string[] parameters, string username)
        {
            return false;
        }

        private static bool PostFacebookStatus(string[] parameters, string username)
        {
            return false;
        }

        private static bool PostFacebookPhoto(string[] parameters, string username)
        {
            try
            {

                var photoDetails = new PhotoDetails();

                Dictionary<string, string> options = new Dictionary<string, string>();

                for (int i = 3; i < parameters.Length; i++)
                {
                    String[] keyValue = parameters[i].Split(':');
                    if (keyValue.Length > 2)
                        keyValue[1] += ":" + keyValue[2];
                    if (keyValue.Length < 2)
                        return false;
                    keyValue[1] = keyValue[1].Trim('"');

                    options.Add(keyValue[0], keyValue[1]);
                }
                foreach (KeyValuePair<string, string> keyValuePair in options)
                {
                    switch (keyValuePair.Key)
                    {
                        case "-pn":
                            photoDetails.FacebookName = keyValuePair.Value;
                            break;
                        case "-pd":
                            photoDetails.PhotoDescription = keyValuePair.Value;
                            break;
                        case "-aid":
                            photoDetails.AlbumId = keyValuePair.Value;
                            break;
                        case "-ad":
                            photoDetails.AlbumDescription = keyValuePair.Value;
                            break;
                        case "-an":
                            photoDetails.AlbumName = keyValuePair.Value;
                            break;
                        case "-p":
                            photoDetails.ImageStream = File.ReadAllBytes(keyValuePair.Value);
                            break;
                    }

                }
                if (string.IsNullOrEmpty(photoDetails.AlbumId))
                {
                    photoDetails.AlbumId = ChooseAlbumId(username);
                }
                return _apiManager.FacebookClient.AddPhoto(photoDetails);
            }
            catch
            {
                return false;
            }
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
                default:
                    throw new InvalidCommand(string.Format("The app {0} doesn't exist", parameters[1]));
            }
        }

        private static string GetFacebook(string[] parameters)
        {
            try
            {
                string username = "me";
                foreach (string parameter in parameters)
                {
                    if (parameter.Contains("-u"))
                    {
                        var p = parameter.Split(':');
                        if (p.Length < 2)
                            username = "me";
                        else
                            username = p[1].Trim('\"');
                        break;
                    }
                }

                switch (parameters[2])
                {
                    case "albums":
                        return ShowAlbums(username);
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

        private static string ShowAlbums(string username)
        {
            var albums = _apiManager.FacebookClient.GetAlbums(username);
            StringBuilder builder = new StringBuilder();
            foreach (AlbumDetails albumDetails in albums)
            {
                builder.Append(albumDetails.AlbumName);
                builder.Append("\n");
            }
            return builder.ToString();
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