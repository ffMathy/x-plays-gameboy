using System;
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

        public delegate void MessageReceivedEventHandler(string username, string message);
        public event MessageReceivedEventHandler MessageReceived;

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

            client.Connected += client_Connected;
            client.ChannelListReceived += client_ChannelListReceived;
            client.RawMessageReceived += client_RawMessageReceived;

            client.Connect("irc.twitch.tv", 6667, false, new IrcUserRegistrationInfo() {NickName = twitchUsername, Password = twitchOAuthToken, UserName = twitchUsername, RealName = twitchUsername});

        }

        void client_ChannelListReceived(object sender, IrcChannelListReceivedEventArgs e)
        {
            var client = (IrcClient)sender;
            foreach (var channel in e.Channels)
            {
                client.SendRawMessage("JOIN " + channel.Name);
            }
        }

        void client_Connected(object sender, EventArgs e)
        {
            var client = (IrcClient) sender;
            client.SendRawMessage("join #" + client.LocalUser.UserName.ToLower());
        }

        void client_RawMessageReceived(object sender, IrcRawMessageEventArgs e)
        {
            var client = (IrcClient)sender;

            var message = e.Message;
            if (message.Command == "PRIVMSG" && message.Parameters[0] == "#" + client.LocalUser.UserName.ToLower() && !string.Equals(message.Source.Name, client.LocalUser.UserName, StringComparison.CurrentCultureIgnoreCase))
            {
                if (MessageReceived != null)
                {
                    MessageReceived(message.Source.Name, message.Parameters[1]);
                }
            }
        }

    }
}
