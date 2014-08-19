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

        public bool IsOperator(string username)
        {
            return _operatorUsernames.Contains(username.ToLower());
        }

        private IrcClient _client;
        private IrcUserRegistrationInfo _registrationInformation;

        private readonly HashSet<string> _operatorUsernames;

        private TwitchChatEngine()
        {
            _operatorUsernames = new HashSet<string>();
        }

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
            client.RawMessageReceived += client_RawMessageReceived;
            client.Disconnected += client_Disconnected;
            client.Error += client_Error;

            _client = client;
            _registrationInformation = new IrcUserRegistrationInfo()
            {
                NickName = twitchUsername,
                Password = twitchOAuthToken,
                UserName = twitchUsername,
                RealName = twitchUsername
            };

            Connect();
        }

        void client_Error(object sender, IrcErrorEventArgs e)
        {
            if (!_client.IsConnected)
            {
                Connect();
            }
        }

        private void Connect()
        {
            _client.Connect("irc.twitch.tv", 6667, false, _registrationInformation);
        }

        void client_Disconnected(object sender, EventArgs e)
        {
            Connect();
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
            if (message.Command == "PRIVMSG" && message.Parameters[0] == "#" + client.LocalUser.UserName.ToLower() &&
                !string.Equals(message.Source.Name, client.LocalUser.UserName, StringComparison.CurrentCultureIgnoreCase))
            {
                if (MessageReceived != null)
                {
                    MessageReceived(message.Source.Name, message.Parameters[1]);
                }
            }
            else if (message.Command == "JOIN")
            {
                SendMessage("Welcome to the stream, @" +
                                      message.Source +
                                      "!");
                SendMessage("Be sure to read the description for a full list of commands and more details.");
            }
            else if(message.Command == "MODE" && message.Parameters[1] == "+o")
            {
                var operatorUsername = message.Parameters[2];
                _operatorUsernames.Add(operatorUsername.ToLower());
            }
        }

        public void SendMessage(string message)
        {
            _client.SendRawMessage("PRIVMSG #" + _client.LocalUser.UserName.ToLower() + " " + message);
        }

    }
}
