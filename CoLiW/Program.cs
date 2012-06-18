using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
                    Console.WriteLine(ProcessCommand(command));
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
            }
        }

        private static string ProcessCommand(string command)
        {
            try
            {
                command = ToLower(command);
                string[] parameters = null;
                string retVal = CheckForMashups(command);
                if (retVal != "")
                    return retVal;
                parameters = ParseArguments(command);

                switch (parameters[0])
                {
                    case "login":
                        return Login(parameters);
                    case "logout":
                        return Logout(parameters);
                    case "get":
                        return Get(parameters);
                    case "post":
                        return Post(parameters).ToString(CultureInfo.InvariantCulture);
                    default:
                        Console.WriteLine("Wrong command. Type 'help' for a list of available commands");
                        break;
                }
                return "Success";
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return exception.Message;
            }
        }

        public static string CheckForMashups(string command)
        {
            if (command.Contains("|"))
            {
                string[] parameters = null;
                string[] commands = null;
                commands = command.Split('|');
                if (commands.Length > 2) return "Too many commands";

                commands[0] = commands[0].Trim();
                commands[1] = commands[1].Trim();

                if (commands[0].StartsWith("get") && commands[1].StartsWith("post"))
                {
                    string tmp = commands[0];
                    commands[0] = commands[1];
                    commands[1] = tmp;
                }
                else if (commands[0].StartsWith("post") == false || commands[1].StartsWith("get") == false)
                    throw new InvalidCommand("For a mashup, you must type a post command followed by pipe and get command");

                string retVal;
                try
                {
                    retVal = ProcessCommand(commands[1]);
                }
                catch (Exception exception)
                {
                    return "Get error: " + exception.Message;
                }
                
                parameters = ParseArguments(commands[0]);
                commands[0] = string.Empty;
                for (int i = 0; i < parameters.Length; i++)
                {
                    var opts = parameters[i].Split(':');
                    if (opts.Length == 2)
                        if (opts[1].Length == 0)
                        {
                            parameters[i] += "\"" + retVal + "\"";
                        }
                        else if (opts[1].Contains("{@}"))
                            parameters[i] = opts[0] + ":" + opts[1].Replace("{@}", retVal);
                    commands[0] += " " + parameters[i]; //rebuild the command
                }
                commands[0] = commands[0].Trim(); //remove spaces
                return ProcessCommand(commands[0]); //execute the command

            }
            return "";
        }

        #region Main commands

        private static string Login(string[] parameters)
        {
            if (parameters.Length < 2)
                throw new InvalidCommand("You forgot to specify the app");
            switch (parameters[1])
            {
                case "facebook":
                    if (parameters.Length == 4)
                        return _apiManager.FacebookClient.Login(parameters[2], parameters[3], false).ToString();
                    if (parameters.Length == 2)
                    {
                        return _apiManager.FacebookClient.Login(false).ToString();
                    }
                    throw new InvalidCommand("Wrong number of parameters");
                default:
                    throw new InvalidCommand("Wrong command");
            }
        }

        private static string Logout(string[] parameters)
        {
            if (parameters.Length < 2)
                throw new InvalidCommand("You forgot to specify the app");
            try
            {
                switch (parameters[1])
                {
                    case "facebook":
                        return _apiManager.FacebookClient.Logout().ToString();
                    default:
                        throw new InvalidCommand("Invalid app selected for logout.");
                }
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
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

        #endregion

        #region Facebook

        private static bool PostFacebook(string[] parameters)
        {
            try
            {
                string username = null;
                foreach (string parameter in parameters)
                {
                    if (parameter.Contains("-u"))
                    {
                        string[] p = parameter.Split(':');
                        username = p.Length < 2 ? "me" : p[1].Trim('\"');
                        break;
                    }
                }

                if (username == null)
                    username = "me";


                switch (parameters[2])
                {
                    case "message":
                        return PostFacebookMessage(parameters, username);
                    case "photo":
                        return PostFacebookPhoto(parameters, username);
                    case "album":
                        return PostFacebookAlbum(parameters, username);
                    case "status":
                        return PostFacebookMessage(parameters, "me");
                }
            }
            catch (Exception exception)
            {
                if (exception is FacebookOAuthException)
                    _apiManager.FacebookClient.FbLoginForm.IsLoggedIn = false;
                return false;
            }
            return true;
        }

        private static bool PostFacebookMessage(string[] parameters, string username)
        {
            try
            {
                var details = new PostDetails();
                var options = new Dictionary<string, string>();

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

                foreach (var keyValuePair in options)
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


        private static bool PostFacebookPhoto(string[] parameters, string username)
        {
            try
            {
                var photoDetails = new PhotoDetails();

                var options = new Dictionary<string, string>();

                for (int i = 3; i < parameters.Length; i++)
                {
                    String[] keyValue = parameters[i].Split(':');
                    if (keyValue.Length > 2)
                        keyValue[1] += ":" + keyValue[2];
                    if (keyValue.Length < 2)
                        return false;
                    keyValue[1] = keyValue[1].Trim('\"');

                    options.Add(keyValue[0], keyValue[1]);
                }

                foreach (var keyValuePair in options)
                {
                    switch (keyValuePair.Key)
                    {
                        case "-u":
                            username = keyValuePair.Value;
                            break;
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
                if (string.IsNullOrEmpty(photoDetails.AlbumId) && username.CompareTo("me") == 0)
                {
                    photoDetails.AlbumId = ChooseAlbumId(username);
                }
                else photoDetails.AlbumId = username;
                return _apiManager.FacebookClient.AddPhoto(photoDetails);
            }
            catch
            {
                return false;
            }
        }

        private static string ChooseAlbumId(string username)
        {
            List<AlbumDetails> albums = _apiManager.FacebookClient.GetAlbums(username);

            int i = 1;
            foreach (AlbumDetails album in albums)
            {
                Console.WriteLine(String.Format("{0}. {1}", i, album.AlbumName));
                i++;
            }
            if (albums.Count == 0)
                throw new Exception("No album returned. You may not have the permission to see the albums of " +
                                    username);
            Console.WriteLine("Type a number:");
            string selectedAlbum = Console.ReadLine();

            Int32.TryParse(selectedAlbum, out i);
            return albums[i - 1].Id;
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
                        string[] p = parameter.Split(':');
                        username = p.Length < 2 ? "me" : p[1].Trim('\"');
                        break;
                    }
                }

                switch (parameters[2])
                {
                    case "profilepic":
                        return GetFacebookProfilePic(parameters, username);
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

        private static string GetFacebookProfilePic(string[] parameters, string username)
        {
            try
            {
                if (username.CompareTo("me") == 0)
                    username = _apiManager.FacebookClient.GetUserDetails(username).Username;

                var options = new Dictionary<string, string>();

                for (int i = 3; i < parameters.Length; i++)
                {
                    string[] values = parameters[i].Split(':');

                    if (values.Length > 2)
                        values[1] += ":" + values[2];
                    if (values.Length < 2)
                        throw new InvalidCommand("Wrong options");
                    values[1] = values[1].Trim('\"');

                    options[values[0]] = values[1];
                }
                string path = null;
                string size = null;
                foreach (var keyValuePair in options)
                {
                    switch (keyValuePair.Key)
                    {
                        case "-p":
                            path = keyValuePair.Value;
                            break;
                        case "-s":
                            size = keyValuePair.Value;
                            break;
                        case "-u":
                            break;
                        default:
                            throw new InvalidCommand("Wrong option: " + keyValuePair.Key);
                    }
                }
                if (size == null)
                    size = "large";
                else if (size.Length < 2)
                {
                    string[] sizes = { "small", "large", "square" };
                    int s = 0;
                    Int32.TryParse(options["-s"], out s);
                    if (s < 0 || s > 2)
                        throw new InvalidCommand(
                            "You must provide a size value between 0 and 2, or one of the following values:small, large and square");
                    size = sizes[s];
                }

                if (path == null)
                {
                    Console.WriteLine("Type the path for the photo:(ex: c:\\myprofilepic.jpg");
                    path = Console.ReadLine();
                }


                _apiManager.FacebookClient.GetUrlImage(
                    "https://graph.facebook.com/" + username + "/picture?type=" + size, path);
                return path;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static string ShowAlbums(string username)
        {
            List<AlbumDetails> albums = _apiManager.FacebookClient.GetAlbums(username);
            var builder = new StringBuilder();
            foreach (AlbumDetails albumDetails in albums)
            {
                builder.Append(albumDetails.AlbumName);
                builder.Append("\n");
            }
            return builder.ToString();
        }

        #endregion

        #region Utils

        private static string ToLower(string command)
        {
            string newCommand = null;
            for (int i = 0; i < command.Length; i++)
            {
                newCommand += Char.ToLower(command[i]);
            }
            return newCommand;
        }

        public static String[] ParseArguments(string args)
        {
            var parameters = new List<string>();
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

        #endregion
    }
}