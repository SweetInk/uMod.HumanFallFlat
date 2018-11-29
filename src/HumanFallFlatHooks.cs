using Multiplayer;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using uMod.Configuration;
using uMod.Libraries.Universal;
using uMod.Plugins;

namespace uMod.HumanFallFlat
{
    /// <summary>
    /// Game hooks and wrappers for the core Human: Fall Flat plugin
    /// </summary>
    public partial class HumanFallFlat
    {
        #region Player Hooks

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="netHost"></param>
        /// <param name="name"></param>
        /// <param name="message"></param>
        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(NetHost netHost, string name, string message)
        {
            if (message.Trim().Length <= 1)
            {
                return true;
            }

            // TODO: Handle split screen players (same NetHost, different NetPlayer)

            List<NetPlayer> netPlayers = netHost.players.ToList();
            NetPlayer netPlayer = netPlayers.FirstOrDefault();
            IPlayer player = netPlayer?.IPlayer;
            if (netPlayer == null || player == null)
            {
                return null;
            }

            // Update player's stored username
            if (!player.Name.Equals(name))
            {
                player.Rename(name);
            }

            // Is it a chat command?
            string str = message.Substring(0, 1);
            if (!str.Equals("/") && !str.Equals("!"))
            {
                // Call the hooks for plugins
                object chatUniversal = Interface.Call("OnPlayerChat", player, message);
                object chatSpecific = Interface.Call("OnPlayerChat", netPlayer, message);
                if (chatUniversal != null || chatSpecific != null)
                {
                    return true;
                }

                Interface.uMod.LogInfo($"[Chat] {name}: {message}");
                return null;
            }

            // Get the command and parse it
            string cmd;
            string[] args;
            Universal.CommandSystem.ParseCommand(message.Substring(1), out cmd, out args);
            if (cmd == null)
            {
                return null;
            }

            // Call the hooks for plugins
            object commandUniversal = Interface.Call("OnPlayerCommand", player, cmd, args);
            object commandSpecific = Interface.Call("OnPlayerCommand", netPlayer, cmd, args);
            if (commandUniversal != null || commandSpecific != null)
            {
                return true;
            }

            // Is this a covalence command?
            message = "/" + message.Substring(1);
            if (Universal.CommandSystem.HandleChatMessage(player, message))
            {
                return true;
            }

            // TODO: Handle non-universal commands

            player.Reply(string.Format(lang.GetMessage("UnknownCommand", this, player.Id), cmd));

            return true;
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="netPlayer"></param>
        [HookMethod("IOnPlayerConnected")]
        private void IOnPlayerConnected(NetPlayer netPlayer)
        {
            /*if (App.state != AppSate.Startup)
            {
                return;
            }*/

            // Check if server is intended to be dedicated
            if (netPlayer.isLocalPlayer && HumanFallFlatExtension.Dedicated)
            {
                // Remove server player from client list
                NetGame.instance.clients.Remove(netPlayer.host);

                // Ignore server player
                return;
            }

            // TODO: Add command-line option/argument to allow/disallow split screen players
            /*if (netPlayer.host.players.Count > 1)
            {
                netPlayer.host.players.Remove(netPlayer);
                return;
            }*/

            // TODO: Kick duplicate players (already connected once)

            string userId = netPlayer.host.connection.ToString();

            if (permission.IsLoaded)
            {
                // Update player's stored username
                permission.UpdateNickname(userId, netPlayer.host.name);

                // Set default groups, if necessary
                uModConfig.DefaultGroups defaultGroups = Interface.uMod.Config.Options.DefaultGroups;
                if (!permission.UserHasGroup(userId, defaultGroups.Players))
                {
                    permission.AddUserGroup(userId, defaultGroups.Players);
                }
            }

            // Let universal know
            Universal.PlayerManager.PlayerJoin(userId, netPlayer.host.name); // TODO: Move to OnPlayerApproved hook once available
            Universal.PlayerManager.PlayerConnected(netPlayer);

            IPlayer player = Universal.PlayerManager.FindPlayerById(userId);
            if (player != null)
            {
                // Set IPlayer object on NetPlayer
                netPlayer.IPlayer = player;

                // Call game-specific hook
                Interface.Call("OnPlayerConnected", netPlayer);

                // Call universal hook
                Interface.Call("OnPlayerConnected", player);
            }

            // Override/set server hostname
            string serverName = $"{Server.Name} | {Server.Players}/{Server.MaxPlayers}";
            SteamMatchmaking.SetLobbyData((NetGame.instance.transport as NetTransportSteam).lobbyID, "name", serverName);
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="netHost"></param>
        [HookMethod("IOnPlayerDisconnected")]
        private void IOnPlayerDisconnected(NetHost netHost)
        {
            /*if (App.state == AppSate.Startup)
            {
                return;
            }*/

            List<NetPlayer> netPlayers = netHost.players.ToList();
            NetPlayer netPlayer = netPlayers.FirstOrDefault();
            if (netPlayer == null)
            {
                return;
            }

            // Check if server is intended to be dedicated
            if (netPlayer.isLocalPlayer && HumanFallFlatExtension.Dedicated)
            {
                // Ignore server player
                return;
            }

            // Let universal know
            Universal.PlayerManager.PlayerDisconnected(netPlayer);

            // Call game-specific hook
            Interface.Call("OnPlayerDisconnected", netPlayer, "Unknown");

            IPlayer player = netPlayer.IPlayer;
            if (player != null)
            {
                // Call universal hook
                Interface.Call("OnPlayerDisconnected", player, "Unknown");
            }

            // Override/set server hostname
            string serverName = $"{Server.Name} | {Server.Players}/{Server.MaxPlayers}";
            SteamMatchmaking.SetLobbyData((NetGame.instance.transport as NetTransportSteam).lobbyID, "name", serverName);
        }

        #endregion Player Hooks
    }
}
