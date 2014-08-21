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

        public delegate void UserJoinedEventHandler(string username);
        public event UserJoinedEventHandler UserJoined;

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

        private DateTime _lastPongTime;

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
            client.Error += client_Error;
            client.ConnectFailed += client_ConnectFailed;
            client.PongReceived += client_PongReceived;
            client.ServerBounce += client_ServerBounce;
            client.ProtocolError += client_ProtocolError;
            client.ErrorMessageReceived += client_ErrorMessageReceived;

            _client = client;
            _registrationInformation = new IrcUserRegistrationInfo()
            {
                NickName = twitchUsername,
                Password = twitchOAuthToken,
                UserName = twitchUsername,
                RealName = twitchUsername
            };

            Connect();

            _lastPongTime = DateTime.UtcNow;

            StartPingLoop();
        }

        void client_ErrorMessageReceived(object sender, IrcErrorMessageEventArgs e)
        {
            Reconnect();
        }

        private async void Reconnect(int delay = 1)
        {
            Disconnect();
            await Task.Delay(delay);
            Connect();
        }

        private void Disconnect()
        {
            _client.Disconnect();
        }

        void client_ProtocolError(object sender, IrcProtocolErrorEventArgs e)
        {
            Reconnect();
        }

        void client_ServerBounce(object sender, IrcServerInfoEventArgs e)
        {
            Reconnect();
        }

        void client_PongReceived(object sender, IrcPingOrPongReceivedEventArgs e)
        {
            _lastPongTime = DateTime.UtcNow;
        }

        private async void StartPingLoop()
        {
            while (!_client.IsConnected)
            {
                await Task.Delay(100);
            }

            while (true)
            {

                var failed = false;
                try
                {
                    _client.Ping();
                }
                catch (Exception)
                {
                    failed = true;
                }

                await Task.Delay(10000);

                if (failed || !_client.IsConnected || (DateTime.UtcNow - _lastPongTime).TotalSeconds > 60000)
                {
                    _client.Disconnect();
                    Connect();
                }
            }
        }

        async void client_ConnectFailed(object sender, IrcErrorEventArgs e)
        {
            await Task.Delay(1000);
            Connect();
        }

        void client_Error(object sender, IrcErrorEventArgs e)
        {
            Reconnect();
        }

        private void Connect()
        {
            if (!_client.IsConnected)
            {
                _operatorUsernames.Clear();
                _client.Connect("irc.twitch.tv", 6667, false, _registrationInformation);
            }
        }

        void client_Connected(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;
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
                if (UserJoined != null)
                {
                    UserJoined(message.Source.Name);
                }
            }
            else if (message.Command == "MODE" && message.Parameters[1] == "+o")
            {
                var operatorUsername = message.Parameters[2].ToLower();
                if (!_operatorUsernames.Contains(operatorUsername))
                {
                    _operatorUsernames.Add(operatorUsername);
                }
            }
        }

        public void SendMessage(string message)
        {
            if (_client.Channels.Any())
            {
                _client.LocalUser.SendMessage(_client.Channels[0], message);
            }
        }

    }
}
