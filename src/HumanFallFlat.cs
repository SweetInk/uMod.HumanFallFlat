using Multiplayer;
using Steamworks;
using System.Collections.Generic;
using uMod.Libraries;
using uMod.Libraries.Universal;
using uMod.Logging;
using uMod.Plugins;

namespace uMod.HumanFallFlat
{
    /// <summary>
    /// The core Human: Fall Flat plugin
    /// </summary>
    public partial class HumanFallFlat : CSPlugin
    {
        #region Initialization

        /// <summary>
        /// Initializes a new instance of the HumanFallFlat class
        /// </summary>
        public HumanFallFlat()
        {
            // Set plugin info attributes
            Title = "HumanFallFlat";
            Author = HumanFallFlatExtension.AssemblyAuthors;
            Version = HumanFallFlatExtension.AssemblyVersion;
        }

        // Instances
        internal static readonly HumanFallFlatProvider Universal = HumanFallFlatProvider.Instance;
        internal readonly IServer Server = Universal.CreateServer();

        // Libraries
        internal readonly Lang lang = Interface.uMod.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.uMod.GetLibrary<Permission>();

        private bool serverInitialized;

        #endregion Initialization

        #region Core Hooks

        /// <summary>
        /// Called when the plugin is initializing
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote error logging
            RemoteLogger.SetTag("game", Title.ToLower());
            RemoteLogger.SetTag("game version", Server.Version);

            // Register messages for localization
            foreach (KeyValuePair<string, Dictionary<string, string>> language in Localization.languages)
            {
                lang.RegisterMessages(language.Value, this, language.Key);
            }
        }

        /// <summary>
        /// Called when another plugin has been loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (serverInitialized)
            {
                // Call OnServerInitialized for hotloaded plugins
                plugin.CallHook("OnServerInitialized", false);
            }
        }

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("IOnServerInitialized")]
        private void IOnServerInitialized()
        {
            if (!serverInitialized)
            {
                Analytics.Collect();

                serverInitialized = true;

                // Override/set server hostname
                string serverName = $"{SteamFriends.GetPersonaName()}'s uMod Server | {Server.Players}/{Server.MaxPlayers}";
                NetGame.instance.server.name = $"{SteamFriends.GetPersonaName()}'s uMod Server"; // TODO: Get name from command-line +hostname argument
                SteamMatchmaking.SetLobbyData((NetGame.instance.transport as NetTransportSteam).lobbyID, "name", serverName);

                // Let plugins know server startup is complete
                Interface.CallHook("OnServerInitialized", serverInitialized);

                Interface.Oxide.LogInfo($"Server version is: {Server.Version}");

                if (Interface.uMod.ServerConsole != null)
                {
                    Interface.uMod.ServerConsole.Title = () => $"{NetGame.instance.players.Count} | {NetGame.instance.server.name}";
                }
            }
        }

        /// <summary>
        /// Called when the server is saved
        /// </summary>
        [HookMethod("OnServerSave")]
        private void OnServerSave()
        {
            Interface.uMod.OnSave();

            // Save groups, users, and other data
            Universal.PlayerManager.SavePlayerData();
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown()
        {
            Interface.uMod.OnShutdown();

            // Save groups, users, and other data
            Universal.PlayerManager.SavePlayerData();
        }

        #endregion Core Hooks
    }
}
