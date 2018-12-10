using Multiplayer;
using Steamworks;
using System;
using System.Globalization;
using System.Net;
using uMod.Libraries.Universal;
using uMod.Logging;

namespace uMod.HumanFallFlat
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class HumanFallFlatServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get => HumanFallFlatExtension.ServerName; // NetGame.instance.server.name
            set => HumanFallFlatExtension.ServerName = value; // NetGame.instance.server.name
        }

        private static IPAddress address;
        private static IPAddress localAddress;

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address
        {
            get
            {
                try
                {
                    if (address == null)
                    {
                        uint publicIp = SteamGameServer.GetPublicIP();
                        if (publicIp > 0)
                        {
                            string ip = string.Concat(publicIp >> 24 & 255, ".", publicIp >> 16 & 255, ".", publicIp >> 8 & 255, ".", publicIp & 255); // TODO: Create as utility method
                            IPAddress.TryParse(ip, out address);
                            Interface.uMod.LogInfo($"IP address from Steam query: {address}");
                        }
                        else
                        {
                            WebClient webClient = new WebClient();
                            IPAddress.TryParse(webClient.DownloadString("http://api.ipify.org"), out address);
                            Interface.uMod.LogInfo($"IP address from external API: {address}");
                        }
                    }

                    return address;
                }
                catch (Exception ex)
                {
                    RemoteLogger.Exception("Couldn't get server's public IP address", ex);
                    return IPAddress.Any;
                }
            }
        }

        /// <summary>
        /// Gets the local IP address of the server, if known
        /// </summary>
        public IPAddress LocalAddress
        {
            get
            {
                try
                {
                    return localAddress ?? (localAddress = Utility.GetLocalIP());
                }
                catch (Exception ex)
                {
                    RemoteLogger.Exception("Couldn't get server's local IP address", ex);
                    return IPAddress.Any;
                }
            }
        }

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => 0; // TODO: Implement when possible

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => VersionDisplay.fullVersion;

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => VersionDisplay.netCode.ToString();

        /// <summary>
        /// Gets the language set by the server
        /// </summary>
        public CultureInfo Language => CultureInfo.InstalledUICulture;

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => NetGame.instance.players.Count;

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get => Options.lobbyMaxPlayers; // TODO: Test
            set => Options.lobbyMaxPlayers = (byte)value;
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get => throw new NotImplementedException(); // TODO: Implement when possible
            set => throw new NotImplementedException(); // TODO: Implement when possible
        }

        /// <summary>
        /// Gets information on the currently loaded save file
        /// </summary>
        public SaveInfo SaveInfo => null;

        #endregion Information

        #region Administration

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string id, string reason, TimeSpan duration = default(TimeSpan))
        {
            // Check if already banned
            if (!IsBanned(id))
            {
                // Ban and kick user
                // TODO: Implement when possible
            }
        }

        /// <summary>
        /// Gets the amount of time remaining on the player's ban
        /// </summary>
        /// <param name="id"></param>
        public TimeSpan BanTimeRemaining(string id) => TimeSpan.MaxValue; // TODO: Implement when possible

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        /// <param name="id"></param>
        public bool IsBanned(string id) => false; // TODO: Implement when possible

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save() => throw new NotImplementedException(); // TODO: Implement when possible

        /// <summary>
        /// Unbans the player
        /// </summary>
        /// <param name="id"></param>
        public void Unban(string id)
        {
            // Check if unbanned already
            if (IsBanned(id))
            {
                // Set to unbanned
                // TODO: Implement when possible
            }
        }

        #endregion Administration

        #region Chat and Commands

        /// <summary>
        /// Broadcasts the specified chat message and prefix to all players
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Broadcast(string message, string prefix, params object[] args)
        {
            message = args.Length > 0 ? string.Format(Formatter.ToUnity(message), args) : Formatter.ToUnity(message);
            if (!HumanFallFlatExtension.Dedicated && NetGame.instance.server.isLocal)
            {
                NetChat.OnReceive(NetGame.instance.local.hostId, prefix, message);
            }
            using (NetStream netStream = NetGame.BeginMessage(NetMsgId.Chat))
            {
                netStream.WriteNetId(NetGame.instance.local.hostId);
                netStream.Write(prefix ?? string.Empty);
                netStream.Write(message);
                for (int i = 0; i < NetGame.instance.allclients.Count; i++)
                {
                    NetGame.instance.SendReliable(NetGame.instance.allclients[i], netStream);
                }
            }
        }

        /// <summary>
        /// Broadcasts the specified chat message to all players
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message) => Broadcast(message, null);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            NetChat.serverCommands.Execute($"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}");
        }

        #endregion Chat and Commands
    }
}
