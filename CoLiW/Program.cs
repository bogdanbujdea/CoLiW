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
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using Facebook;
using Google.GData.Client;

namespace CoLiW
{
    internal class Program
    {
        private static ApiManager _apiManager;

        [STAThread]
        private static void Main(string[] args)
        {
            string command = string.Empty;
            foreach (string s in args)
            {
                Console.WriteLine(s);
            }
            bool parseFile = false;
            if (args.Length > 0)
            {
                foreach (string s in args)
                {
                    command += s + " ";
                }
                if (File.Exists(command))
                {
                    parseFile = true;
                }
            }

            _apiManager = new ApiManager();
            if (ParseSettingsXml() == false)
            {
                _apiManager.InitializeFacebook("372354616159806", "5ccc6315874961c13249003ef9ed279f");
                _apiManager.InitializeTwitter("LxfSyKV8laf2HuWdzblXw", "PpoiRPC4i9sJBbM14PHRNE7GDfqrZSBdlRALASEBak");
                _apiManager.BloggerClient.AppName = "coliwbbe";
            }
            ParseAliasesXml();

            if (command.Length > 0)
            {
                command = command.Trim();
                if (parseFile)
                {
                    IEnumerable<string> lines = File.ReadLines(command);
                    foreach (string line in lines)
                    {
                        Console.WriteLine(ProcessCommand(line));
                    }
                }
                else
                    Console.WriteLine(ProcessCommand(command));
                SaveSettings();
                SaveAliases();
                Console.WriteLine("Done!");
                Environment.Exit(0);
            }


            while (true)
            {
                try
                {
                    Console.WriteLine("Type a command:");
                    command = Console.ReadLine();
                    Console.WriteLine(ProcessCommand(command));
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
            }
            // ReSharper disable FunctionNeverReturns
        }

        #region Utils

        public static string ReadPassword()
        {
            var pass = new Stack<String>();

            for (ConsoleKeyInfo consKeyInfo = Console.ReadKey(true);
                 consKeyInfo.Key != ConsoleKey.Enter;
                 consKeyInfo = Console.ReadKey(true))
            {
                if (consKeyInfo.Key == ConsoleKey.Backspace)
                {
                    try
                    {
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.Write(" ");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        pass.Pop();
                    }
                    catch (InvalidOperationException)
                    {
                        Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
                    }
                }
                else
                {
                    Console.Write("*");
                    pass.Push(consKeyInfo.KeyChar.ToString());
                }
            }
            string[] password = pass.ToArray();
            Array.Reverse(password);
            return string.Join(string.Empty, password);
        }

        private static Dictionary<string, string> GetParameters(string[] parameters, int indexStart)
        {
            var dictionary = new Dictionary<string, string>();

            for (int i = indexStart; i < parameters.Length; i++)
            {
                string[] keyValue = parameters[i].Split(':');

                if (keyValue.Length > 2)
                {
                    keyValue[1] = keyValue[1] + ":" + keyValue[2];
                }
                if (keyValue.Length < 2)
                    return null;
                keyValue[0] = keyValue[0].Trim('\"');
                keyValue[1] = keyValue[1].Trim('\"');
                dictionary[keyValue[0]] = keyValue[1];
            }
            return dictionary;
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

        private static string ProcessCommand(string command)
        {
            try
            {
                command = GetCommand(command);
                if (CheckForMashups(command))
                    return "";
                string[] parameters = ParseArguments(command);

                switch (parameters[0].ToLower())
                {
                    case "test":
                        ParseRssFeed("http://news.softpedia.com/newsRSS/Global-0.xml", 30);
                        break;
                    case "define":
                        try
                        {
                            if (AddAlias(parameters[1], parameters[2]))
                                return "Alias added";
                        }
                        catch (Exception ex)
                        {
                            return ex.Message;
                        }
                        break;
                    case "login":
                        Login(parameters);
                        SaveSettings();
                        break;
                    case "logout":
                        Logout(parameters);
                        SaveSettings();
                        break;
                    case "get":
                        return Get(parameters);
                    case "post":
                        Post(parameters);
                        break;
                    case "delete":
                        Delete(parameters).ToString(CultureInfo.InvariantCulture);
                        break;
                    case "exit":
                        Exit();
                        break;
                    default:
                        Console.WriteLine("Wrong command. Type 'help' for a list of available commands");
                        break;
                }
                return "Success";
            }
            catch (Exception exception)
            {
                if (exception is NullReferenceException)
                    return "You must login first";
                return exception.Message;
            }
        }

        private static void Exit()
        {
            Console.WriteLine("Saving settings...");
            Console.WriteLine(SaveSettings() ? "Settings saved!" : "Settings were not saved");
            Console.WriteLine(SaveAliases() ? "Aliases saved!" : "Aliases were not saved");
            Console.WriteLine("Bye....");
            Environment.Exit(0);
        }

        private static bool CheckForMashups(string command)
        {
            if (!command.Contains("|"))
            {
                return false;
            }

            string[] commands = null;
            commands = command.Split('|');

            var getcommands = new List<string>();
            var postcommands = new List<string>();
            var results = new List<string>();
            for (int i = 0; i < commands.Length; i++)
            {
                commands[i] = commands[i].Trim();
                if (IsGetCommand(commands[i]))
                {
                    getcommands.Add(commands[i]);
                }
                else
                {
                    postcommands.Add(commands[i]);
                }
            }

            if (getcommands.Count == 0 || postcommands.Count == 0)
            {
                throw new InvalidCommand(
                    "For a mashup, you must have at least one get command and one post/delete command");
            }

            foreach (string cmd in getcommands)
            {
                results.Add(ProcessCommand(cmd));
            }

            string args = string.Empty;

            for (int i = 0; i < postcommands.Count; i++)
            {
                for (int j = 0; j < results.Count; j++)
                {
                    postcommands[i] = postcommands[i].Replace("{" + j + "}", results[j]);
                    args += results[j] + "+";
                }
                args = args.Remove(args.LastIndexOf('+'));
                args = args.Trim();
                if (postcommands[i].Contains(" blogger ") && postcommands[i].Contains("-p"))
                    postcommands[i] = postcommands[i].Replace("-p", "-p?\"" + args + "\"");
                ProcessCommand(postcommands[i]);
            }
            return true;
        }

        private static bool IsGetCommand(string cmd)
        {
            if (cmd.StartsWith("get"))
                return true;
            return false;
        }

        private static bool ParseSettingsXml()
        {
            try
            {
                using (XmlReader reader = new XmlTextReader(_apiManager.SettingsPath))
                {
                    var stack = new Stack<IWebApp>();
                    IWebApp app = null;
                    while (reader.Read())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                switch (reader.Name)
                                {
                                    case "type":
                                        string value = reader.ReadElementContentAsString();
                                        switch (value)
                                        {
                                            case "Twitter":
                                                stack.Push(new Twitter());
                                                break;
                                            case "Facebook":
                                                stack.Push(new Facebook());
                                                break;
                                            case "Blogger":
                                                stack.Push(new Blogger());
                                                break;
                                        }
                                        break;
                                    case "key":
                                        {
                                            app = stack.Peek();
                                            if (app.GetType().Name == "Twitter")
                                            {
                                                ((Twitter) app).ConsumerKey = reader.ReadElementContentAsString();
                                            }
                                            else if (app.GetType().Name == "Facebook")
                                            {
                                                ((Facebook) app).AppId = reader.ReadElementContentAsString();
                                            }
                                            else
                                            {
                                                ((Blogger) app).AppName = reader.ReadElementContentAsString();
                                            }
                                        }
                                        break;
                                    case "secret":
                                        app = stack.Peek();
                                        if (app.GetType().Name == "Twitter")
                                        {
                                            ((Twitter) app).ConsumerSecret = reader.ReadElementContentAsString();
                                        }
                                        else if (app.GetType().Name == "Facebook")
                                        {
                                            ((Facebook) app).AppSecret = reader.ReadElementContentAsString();
                                        }
                                        break;
                                    case "accesstoken":
                                        app = stack.Peek();
                                        if (app.GetType().Name == "Twitter")
                                        {
                                            string[] tokens = reader.ReadElementContentAsString().Split(':');
                                            _apiManager.InitializeTwitter(((Twitter) app).ConsumerKey,
                                                                          ((Twitter) app).ConsumerSecret);
                                            if (tokens.Length != 2)
                                            {
                                                stack.Pop();
                                                break;
                                            }
                                            _apiManager.TwitterClient.Tokens.AccessToken = tokens[0];
                                            _apiManager.TwitterClient.Tokens.AccessTokenSecret = tokens[1];
                                            _apiManager.TwitterClient.AccessToken = tokens[0] + ":" + tokens[1];
                                            _apiManager.TwitterClient.Login(true);
                                        }
                                        else if (app.GetType().Name == "Facebook")
                                        {
                                            ((Facebook) app).AccessToken = reader.ReadElementContentAsString();
                                            _apiManager.InitializeFacebook((app as Facebook).AppId,
                                                                           (app as Facebook).AppSecret);
                                            _apiManager.FacebookClient.AccessToken = (app as Facebook).AccessToken;
                                            _apiManager.FacebookClient.Login(false);
                                        }
                                        else
                                        {
                                            _apiManager.BloggerClient = app as Blogger;
                                        }
                                        stack.Pop();
                                        break;
                                }
                                break;
                        }
                    }
                }
                if (_apiManager.FacebookClient == null || _apiManager.TwitterClient == null ||
                    _apiManager.BloggerClient == null)
                    return false;
                return true;
            }
            catch (Exception exception)
            {
                if (exception is NullReferenceException)
                    Console.WriteLine(exception.ToString());
                if (exception is XmlException)
                    return false;
                if (exception is FileNotFoundException)
                {
                    FileStream fileStream = File.Create(_apiManager.SettingsPath);
                    fileStream.Close();
                    return false;
                }
                Console.WriteLine(exception.Message);
                return false;
            }
        }

        private static bool ParseAliasesXml()
        {
            try
            {
                _apiManager.Aliases = new Dictionary<string, HashSet<string>>();

                using (XmlReader reader = new XmlTextReader(_apiManager.AliasesPath))
                {
                    string command = string.Empty;
                    while (reader.Read())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                switch (reader.Name.ToLower())
                                {
                                    case "command":
                                        command = reader.GetAttribute("value");
                                        break;
                                    case "alias":
                                        if (_apiManager.Aliases.ContainsKey(command) == false)
                                        {
                                            _apiManager.Aliases[command] = new HashSet<string>();
                                        }
                                        _apiManager.Aliases[command].Add(reader.GetAttribute("value"));
                                        break;
                                }
                                break;
                        }
                    }
                }
                return true;
            }
            catch (Exception exception)
            {
                if (exception is FileNotFoundException)
                {
                    FileStream fileStream = File.Create(_apiManager.AliasesPath);
                    fileStream.Close();
                    return false;
                }
                if (exception is NullReferenceException)
                    return false;
                Console.WriteLine("Aliases.xml : " + exception.Message);
                return false;
            }
        }

        private static bool SaveAliases()
        {
            try
            {
                using (XmlWriter writer = new XmlTextWriter(_apiManager.AliasesPath, Encoding.UTF8))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Aliases");
                    foreach (var kvp in _apiManager.Aliases)
                    {
                        writer.WriteStartElement("Command");
                        writer.WriteAttributeString("value", kvp.Key);
                        foreach (string command in kvp.Value)
                        {
                            writer.WriteStartElement("Alias");
                            writer.WriteAttributeString("value", command);
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    return true;
                }
            }
            catch (Exception exception)
            {
                if (exception is FileNotFoundException)
                {
                    FileStream fileStream = File.Create(_apiManager.AliasesPath);
                    fileStream.Close();
                    return SaveAliases();
                }
                if (exception is NullReferenceException)
                    return false;
                Console.WriteLine("Save aliases exception: " + exception.Message);
                return false;
            }
        }

        private static bool SaveSettings()
        {
            try
            {
                using (XmlWriter writer = new XmlTextWriter("settings.xml", Encoding.UTF8))
                {
                    #region document

                    writer.WriteStartDocument();

                    #region settings

                    writer.WriteStartElement("Settings");

                    #region apps

                    writer.WriteStartElement("Apps");
                    var apps = new List<IWebApp>
                                   {_apiManager.FacebookClient, _apiManager.TwitterClient, _apiManager.BloggerClient};
                    foreach (IWebApp webApp in apps)
                    {
                        #region app

                        writer.WriteStartElement("App");

                        #region type

                        writer.WriteStartElement("type");
                        writer.WriteValue(webApp.GetType().Name);
                        writer.WriteEndElement();

                        #endregion

                        #region key

                        writer.WriteStartElement("key");
                        switch (webApp.GetType().Name)
                        {
                            case "Facebook":
                                writer.WriteValue((webApp as Facebook).AppId);
                                break;
                            case "Twitter":
                                writer.WriteValue((webApp as Twitter).ConsumerKey);
                                break;
                            case "Blogger":
                                writer.WriteValue((webApp as Blogger).AppName);
                                break;
                        }
                        writer.WriteEndElement();

                        #endregion

                        #region secret

                        writer.WriteStartElement("secret");
                        switch (webApp.GetType().Name)
                        {
                            case "Facebook":
                                writer.WriteValue((webApp as Facebook).AppSecret);
                                break;
                            case "Twitter":
                                writer.WriteValue((webApp as Twitter).ConsumerSecret);
                                break;
                        }
                        writer.WriteEndElement();

                        #endregion

                        #region accesstoken

                        writer.WriteStartElement("accesstoken");
                        switch (webApp.GetType().Name)
                        {
                            case "Facebook":
                                writer.WriteValue((webApp as Facebook).AccessToken);
                                break;
                            case "Twitter":
                                writer.WriteValue((webApp as Twitter).AccessToken);
                                break;
                        }
                        writer.WriteEndElement();

                        #endregion

                        writer.WriteEndElement();

                        #endregion
                    }
                    writer.WriteEndElement();

                    #endregion

                    writer.WriteEndElement();

                    #endregion

                    writer.WriteEndDocument();

                    #endregion

                    return true;
                }
            }
            catch (Exception exception)
            {
                if (exception is FileNotFoundException)
                {
                    FileStream fileStream = File.Create(_apiManager.SettingsPath);
                    fileStream.Close();
                    return SaveSettings();
                }
                if (exception is XmlException)
                    return false;
                if (exception is NullReferenceException)
                    return false;
                Console.WriteLine("SaveSettings exception: " + exception.Message);
                return false;
            }
        }

        private static string GetCommand(string alias)
        {
            alias = alias.Trim().Trim('\"');
            foreach (var keyValuePair in _apiManager.Aliases)
            {
                foreach (string str in keyValuePair.Value)
                {
                    if (alias.StartsWith(str))
                    {
                        alias = alias.Replace(str, keyValuePair.Key);
                    }
                }
            }
            return alias;
        }

        private static bool AddAlias(string cmd, string cmdAlias)
        {
            cmd = cmd.Trim().Trim('\"');
            cmdAlias = cmdAlias.Trim().Trim('\"');
            foreach (var keyValuePair in _apiManager.Aliases)
            {
                if (String.CompareOrdinal(keyValuePair.Key, cmd) == 0)
                {
                    if (keyValuePair.Value.Contains(cmdAlias) == false)
                    {
                        _apiManager.Aliases[cmd].Add(cmdAlias);
                        return true;
                    }
                    throw new InvalidCommand("The alias \"" + cmdAlias + "\" for the command \"" + cmd +
                                             "\" already exists");
                }
            }
            _apiManager.Aliases[cmd] = new HashSet<string> {cmdAlias};
            SaveAliases();
            return true;
        }

        #endregion

        #region Main commands

        private static string Login(string[] parameters)
        {
            if (parameters.Length < 2)
                throw new InvalidCommand("You forgot to specify the app");
            switch (parameters[1])
            {
                case "facebook":
                    if (parameters.Length == 4)
                        return
                            _apiManager.FacebookClient.Login(parameters[2], parameters[3], false).ToString(
                                CultureInfo.InvariantCulture);
                    if (parameters.Length == 2)
                    {
                        return _apiManager.FacebookClient.Login(false).ToString(CultureInfo.InvariantCulture);
                    }
                    if (parameters.Length == 3 && parameters[2] == "-f")
                        return _apiManager.FacebookClient.Login(true).ToString(CultureInfo.InvariantCulture);
                    throw new InvalidCommand("Wrong number of parameters");
                case "twitter":
                    if (parameters.Length == 4)
                        return
                            _apiManager.TwitterClient.Login(parameters[2], parameters[3], false).ToString(
                                CultureInfo.InvariantCulture);
                    if (parameters.Length == 2)
                    {
                        return _apiManager.TwitterClient.Login(false).ToString(CultureInfo.InvariantCulture);
                    }
                    if (parameters.Length == 3 && parameters[2] == "-f")
                        return _apiManager.TwitterClient.Login(true).ToString(CultureInfo.InvariantCulture);
                    throw new InvalidCommand("Wrong number of parameters");
                case "blogger":
                    if (parameters.Length == 2)
                    {
                        string[] creds = RequestCredentials();
                        return _apiManager.BloggerClient.Login(creds[0], creds[1]).ToString();
                    }
                    if (parameters.Length == 4)
                    {
                        return _apiManager.BloggerClient.Login(parameters[2], parameters[3]).ToString();
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
                        return _apiManager.FacebookClient.Logout().ToString(CultureInfo.InvariantCulture);
                    case "twitter":
                        return _apiManager.TwitterClient.Logout().ToString(CultureInfo.InvariantCulture);
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
                    if (_apiManager.FacebookClient.FbLoginForm.IsLoggedIn == false &&
                        String.CompareOrdinal(parameters[0], "login") != 0)
                        throw new InvalidCommand("You are not logged in. Use the command \"login facebook\" first");
                    return GetFacebook(parameters);
                case "twitter":
                    if (_apiManager.TwitterClient.LoginForm.IsLoggedIn == false &&
                        String.CompareOrdinal(parameters[0], "login") != 0)
                        throw new InvalidCommand("You are not logged in. Use the command \"login twitter\" first");
                    return GetTwitter(parameters);
                case "blogger":
                    if (_apiManager.BloggerClient.IsLoggedIn == false &&
                        String.CompareOrdinal(parameters[0], "login") != 0)
                        throw new InvalidCommand("You are not logged in. Use the command \"login blogger\" first");
                    return GetBlogger(parameters);
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
                    if (_apiManager.FacebookClient.FbLoginForm.IsLoggedIn == false &&
                        parameters[0].CompareTo("login") != 0)
                        throw new InvalidCommand("You are not logged in. Use the command \"login facebook\" first");
                    return PostFacebook(parameters);
                case "twitter":
                    if (_apiManager.TwitterClient.LoginForm.IsLoggedIn == false && parameters[0].CompareTo("login") != 0)
                        throw new InvalidCommand("You are not logged in. Use the command \"login twitter\" first");
                    return PostTwitter(parameters);
                case "blogger":
                    if (_apiManager.BloggerClient.IsLoggedIn == false && parameters[0].CompareTo("login") != 0)
                        throw new InvalidCommand("You are not logged in. Use the command \"login blogger\" first");
                    return PostBlogger(parameters);
                default:
                    throw new InvalidCommand(string.Format("The app {0} doesn't exist", parameters[1]));
            }
        }

        private static bool Delete(string[] parameters)
        {
            if (parameters.Length < 2)
                throw new InvalidCommand("Not enough parameters");

            switch (parameters[1])
            {
                case "facebook":
                    if (_apiManager.FacebookClient.FbLoginForm.IsLoggedIn == false &&
                        parameters[0].CompareTo("login") != 0)
                        throw new InvalidCommand("You are not logged in. Use the command \"login facebook\" first");
                    return DeleteFacebook(parameters);
                case "twitter":
                    if (_apiManager.TwitterClient.LoginForm.IsLoggedIn == false && parameters[0].CompareTo("login") != 0)
                        throw new InvalidCommand("You are not logged in. Use the command \"login twitter\" first");
                    return DeleteTwitter(parameters);
                default:
                    throw new InvalidCommand(string.Format("The app {0} doesn't exist", parameters[1]));
            }
        }

        #endregion

        #region Twitter

        private static bool PostTwitter(string[] parameters)
        {
            switch (parameters[2])
            {
                case "follow":
                    return FollowUser(parameters);
                case "profilepic":
                    return SetTwitterProfilePicture(parameters);
                case "message":
                    return PostTwitterMessage(parameters);
                case "backgroundimage":
                    return PostTwitterBkgImage(parameters);
                case "unfollow":
                    return UnfollowUser(parameters);
                default:
                    throw new InvalidCommand("Unknown argument \"" + parameters[2] + "\"");
            }
        }

        private static bool DeleteTwitter(string[] parameters)
        {
            switch (parameters[2])
            {
                case "user":
                    return UnfollowUser(parameters);
                default:
                    return false;
            }
        }

        private static bool PostTwitterBkgImage(string[] parameters)
        {
            Dictionary<string, string> options = GetParameters(parameters, 3);
            string path = null;
            foreach (var keyValuePair in options)
            {
                switch (keyValuePair.Key)
                {
                    case "-p":
                        path = keyValuePair.Value;
                        break;
                    default:
                        throw new InvalidCommand("Undefined option: " + keyValuePair.Key);
                }
            }
            return _apiManager.TwitterClient.SetBackgroundImage(path);
        }

        private static bool UnfollowUser(string[] parameters)
        {
            try
            {
                Dictionary<string, string> options = GetParameters(parameters, 3);

                string username = null;
                foreach (var keyValuePair in options)
                {
                    switch (keyValuePair.Key)
                    {
                        case "-u":
                            username = keyValuePair.Value;
                            break;
                        default:
                            throw new InvalidCommand("Undefined option:" + keyValuePair.Value);
                    }
                }
                if (_apiManager.TwitterClient.Unfollow(username))
                {
                    Console.WriteLine("You're not following " + username + " anymore");
                    return true;
                }
                Console.WriteLine("You're already not following " + username);
                return false;
            }
            catch (Exception exception)
            {
                if (exception is NullReferenceException)
                    return false;
                Console.WriteLine(exception.Message);
                return false;
            }
        }

        private static bool PostTwitterMessage(string[] parameters)
        {
            Dictionary<string, string> options = GetParameters(parameters, 3);
            string username = null, message = null, path = null;
            foreach (var keyValuePair in options)
            {
                switch (keyValuePair.Key)
                {
                    case "-u":
                        username = keyValuePair.Value;
                        break;
                    case "-m":
                        message = keyValuePair.Value;
                        break;
                    case "-p":
                        path = keyValuePair.Value;
                        break;
                    default:
                        throw new InvalidCommand("Unknown argument \"" + keyValuePair.Key + "\"");
                }
            }
            if (string.IsNullOrEmpty(username)) //if update status
            {
                return _apiManager.TwitterClient.UpdateStatus(message, path);
            }
            return _apiManager.TwitterClient.SendMessage(message, username);
        }

        private static bool SetTwitterProfilePicture(string[] parameters)
        {
            Dictionary<string, string> options = GetParameters(parameters, 3);
            string path = null;

            foreach (var keyValuePair in options)
            {
                switch (keyValuePair.Key)
                {
                    case "-p":
                        path = keyValuePair.Value;
                        break;
                    default:
                        throw new InvalidCommand("Unknown argument \"" + keyValuePair.Key + "\"");
                }
            }
            if (path != null)
                return _apiManager.TwitterClient.UpdateProfilePicture(path);
            throw new InvalidCommand("You must specify the path for the image");
        }

        private static bool FollowUser(string[] parameters)
        {
            Dictionary<string, string> options = GetParameters(parameters, 3);
            string username = null;

            foreach (var keyValuePair in options)
            {
                switch (keyValuePair.Key)
                {
                    case "-u":
                        username = keyValuePair.Value;
                        break;
                    default:
                        throw new InvalidCommand("Unknown argument \"" + keyValuePair.Key + "\"");
                }
            }

            return _apiManager.TwitterClient.Follow(username);
        }

        private static string GetTwitter(string[] parameters)
        {
            if (parameters.Length < 3)
                throw new InvalidCommand("You need at least 3 arguments for a GET command");
            string username = _apiManager.TwitterClient.ScreenName;
            foreach (string parameter in parameters)
            {
                if (parameter.Contains("-u"))
                {
                    string[] p = parameter.Split(':');
                    username = p.Length < 2 ? _apiManager.TwitterClient.ScreenName : p[1].Trim('\"');
                    break;
                }
            }


            switch (parameters[2])
            {
                case "last_tweets":
                    int nr = 0;
                    Int32.TryParse(parameters[3], out nr);
                    if (nr <= 0)
                        return "";
                    foreach (string text in _apiManager.TwitterClient.GetLastTweets(nr))
                    {
                        Console.WriteLine(text);
                    }
                    return "Done!";
                case "name":
                    return _apiManager.TwitterClient.GetUserDetails(username).Name;
                case "description":
                    return _apiManager.TwitterClient.GetUserDetails(username).Description;
                case "location":
                    return _apiManager.TwitterClient.GetUserDetails(username).Location;
                case "favorites":
                    return
                        _apiManager.TwitterClient.GetUserDetails(username).NumberOfFavorites.ToString(
                            CultureInfo.InvariantCulture);
                case "id":
                    return
                        _apiManager.TwitterClient.GetUserDetails(username).Id.ToString(
                            CultureInfo.InvariantCulture);
                case "followers":
                    return _apiManager.TwitterClient.GetUserDetails(username).NumberOfFollowers.ToString();
                case "friends":
                    return
                        _apiManager.TwitterClient.GetUserDetails(username).NumberOfFriends.ToString(
                            CultureInfo.InvariantCulture);
                case "website":
                    return _apiManager.TwitterClient.GetUserDetails(username).Website;
                case "tweets":
                    return
                        _apiManager.TwitterClient.GetUserDetails(username).NumberOfStatuses.ToString(
                            CultureInfo.InvariantCulture);
                case "backgroundimage":
                    return _apiManager.TwitterClient.GetUserDetails(username).ProfileBackgroundImageLocation;
                case "profilepic":
                    return _apiManager.TwitterClient.GetUserDetails(username).ProfileImageLocation;
            }
            return "";
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

        private static bool DeleteFacebook(string[] parameters)
        {
            return false;
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
            try
            {
                var album = new AlbumDetails();
                Dictionary<string, string> options = GetParameters(parameters, 3);
                foreach (var option in options)
                {
                    switch (option.Key)
                    {
                        case "-an":
                            album.AlbumName = option.Value;
                            break;
                        case "-ad":
                            album.AlbumDescription = option.Value;
                            break;
                        default:
                            throw new InvalidCommand("Unknown parameter: " + option.Key);
                    }
                }
                if (album.AlbumName == null)
                    throw new InvalidCommand("Album name missing. Use the -an parameter to specify it");

                return _apiManager.FacebookClient.CreateAlbum(album);
            }
            catch (Exception exception)
            {
                if (exception is InvalidCommand)
                    Console.WriteLine(exception.Message);
                return false;
            }
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
                if (String.CompareOrdinal(username, "me") == 0)
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
                    string[] sizes = {"small", "large", "square"};
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

        #region Blogger

        private static bool PostBlogger(string[] parameters)
        {
            var options = new Dictionary<string, string>();
            //-p , [-t], -id, -h or -hp
            for (int i = 2; i < parameters.Length; i++)
            {
                if (parameters[i].Contains("-p?") == false)
                {
                    string[] keyValue = parameters[i].Split(':');
                    if (keyValue.Length > 2)
                    {
                        keyValue[1] = keyValue[1] + ":" + keyValue[2];
                    }
                    if (keyValue.Length < 2)
                        return false;
                    keyValue[0] = keyValue[0].Trim('\"');
                    keyValue[1] = keyValue[1].Trim('\"');
                    options[keyValue[0]] = keyValue[1];
                }
                else
                {
                    options["-p"] = parameters[i].Split('?')[1];
                }
            }

            if (options == null)
                throw new InvalidCommand("You must specify parameters like html content/path, blog id and post title");

            var postInfo = new PostInfo();
            string rssUrl = null;
            int count = 1;
            foreach (var keyValuePair in options)
            {
                switch (keyValuePair.Key)
                {
                    case "-hp":
                        postInfo.Content = File.ReadAllText(keyValuePair.Value);
                        break;
                    case "-h":
                        postInfo.Content = keyValuePair.Value;
                        break;
                    case "-id":
                        postInfo.BlogId = keyValuePair.Value;
                        break;
                    case "-rc":
                        Int32.TryParse(keyValuePair.Value, out count);
                        break;
                    case "-t":
                        postInfo.Title = keyValuePair.Value;
                        break;
                    case "-rss":
                        rssUrl = keyValuePair.Value;
                        if (rssUrl == "")
                            break;
                        break;
                    case "-d":
                        postInfo.IsDraft = true;
                        break;
                    case "-p":
                        postInfo.Parse = true;
                        postInfo.Args = keyValuePair.Value.Trim('\"').Split('+');
                        break;
                    default:
                        throw new InvalidCommand("Unknown parameter: " + keyValuePair.Key);
                }
            }
            if (postInfo.BlogId == null)
            {
                Console.WriteLine("Choose a blog:");
                postInfo.BlogId = GetBlogs(true);
            }
            if (postInfo.Content == null || postInfo.BlogId == null)
                throw new InvalidCommand("You must specify all the parameters: content, blog id and post title");
            string htmlContent = string.Empty;
            if (string.IsNullOrEmpty(rssUrl) == false)
            {
                string template = postInfo.Content;
                foreach (Item item in ParseRssFeed(rssUrl, count))
                {
                    htmlContent += ParseHtml(template, new[] {item.Title, item.Description, item.Link});
                }
                postInfo.Content = htmlContent;
                return _apiManager.BloggerClient.CreatePost(postInfo);
            }
            if (postInfo.Parse)
            {
                postInfo.Content = ParseHtml(postInfo.Content, postInfo.Args);
            }
            return _apiManager.BloggerClient.CreatePost(postInfo);
        }

        private static string ParseHtml(string text, string[] args)
        {
            int argId = 0;
            foreach (string arg in args)
            {
                text = text.Replace("{" + argId + "}", arg);
                argId++;
            }
            return text;
        }

        private static List<Item> ParseRssFeed(string url, int count)
        {
            var document = new XmlDocument();
            var client = new WebClient();
            client.DownloadFile(url, "feed.xml");
            document.Load("feed.xml");
            //Console.WriteLine(document.InnerText);
            int i = 0;
            var items = new List<Item>();
            XmlNodeList rssItems = document.GetElementsByTagName("item");
            if (count >= rssItems.Count)
                count = rssItems.Count - 1;
            foreach (XmlElement xmlItem in rssItems)
            {
                try
                {
                    i++;
                    var item = new Item();
                    item.Title = xmlItem.GetElementsByTagName("title")[0].InnerText;
                        //item.GetElementsByTagName("title").Item(0).Value;
                    item.Description = xmlItem.GetElementsByTagName("description")[0].InnerText;
                    item.Link = xmlItem.GetElementsByTagName("link")[0].InnerText;
                    items.Add(item);
                    if (i == count)
                        break;
                }
                catch (Exception exception)
                {
                    if (exception is NullReferenceException)
                        i--;
                    else
                    {
                        throw;
                    }
                }
            }
            if (i == 0 || count < 0 || items.Count == 0)
                throw new InvalidCommand("No rss items");
            return items;
        }

        private static string[] RequestCredentials()
        {
            var creds = new List<string>();

            Console.WriteLine("Enter your google username(ex. user@gmail.com): ");
            creds.Add(Console.ReadLine());
            Console.WriteLine("Enter your password:");
            creds.Add(ReadPassword());
            return creds.ToArray();
        }

        private static string GetBlogger(string[] parameters)
        {
            switch (parameters[2])
            {
                case "blogs":
                    return GetBlogs(false);
                default:
                    throw new InvalidCommand("Wrong option: " + parameters[2]);
            }
        }

        private static string GetBlogs(bool chooseBlog)
        {
            AtomEntryCollection blogs = _apiManager.BloggerClient.GetListOfBlogs();
            Console.WriteLine("Title\tId\n");
            int i = 1;
            var nblogs = new List<Blog>();
            foreach (AtomEntry atomEntry in blogs)
            {
                string id = atomEntry.Id.Uri.ToString().Substring(atomEntry.Id.Uri.ToString().LastIndexOf('-') + 1);
                Console.WriteLine(i + ". " + atomEntry.Title.Text + "\t" + id);
                nblogs.Add(new Blog {Id = id, Name = atomEntry.Title.Text});
            }
            if (chooseBlog)
            {
                string idStr = Console.ReadLine();
                int id;
                bool tryParse = Int32.TryParse(idStr, out id);
                if (tryParse == false)
                    throw new InvalidCommand("You must type a number");
                return nblogs[id - 1].Id;
            }
            return "Success";
        }

        #endregion
    }
}