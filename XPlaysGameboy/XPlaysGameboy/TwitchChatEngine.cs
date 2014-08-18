﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using IrcDotNet;

namespace XPlaysGameboy
{
    public class TwitchChatEngine
    {

        private static TwitchChatEngine _engine;

        public static TwitchChatEngine Instance
        {
            get { return _engine ?? (_engine = new TwitchChatEngine()); }
        }

        private TwitchChatEngine() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="twitchUsername"></param>
        /// <param name="twitchOAuthToken">
        /// A chat login token which can be generated here: http://www.twitchapps.com/tmi/
        /// </param>
        public void Start(string twitchUsername, string twitchOAuthToken)
        {
            var client = new IrcClient();
            client.Connect("irc.twitch.tv", 6667, false, new IrcUserRegistrationInfo() {NickName = twitchUsername, Password = twitchOAuthToken, UserName = twitchUsername, RealName = twitchUsername});
            client.Connected += client_Connected;
            client.RawMessageReceived += client_RawMessageReceived;

        }

        void client_Connected(object sender, EventArgs e)
        {
            var client = (IrcClient) sender;
            client.SendRawMessage("JOIN #" + client.LocalUser.UserName);
        }

        void client_RawMessageReceived(object sender, IrcRawMessageEventArgs e)
        {
            Debug.WriteLine(e.RawContent);
        }

    }
}
