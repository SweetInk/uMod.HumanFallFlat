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

            // Add core plugin commands
            AddUniversalCommand(new[] { "umod.plugins", "u.plugins", "oxide.plugins", "o.plugins", "plugins" }, nameof(Commands.PluginsCommand), "umod.plugins");
            AddUniversalCommand(new[] { "umod.load", "u.load", "oxide.load", "o.load", "plugin.load" }, nameof(Commands.LoadCommand), "umod.load");
            AddUniversalCommand(new[] { "umod.reload", "u.reload", "oxide.reload", "o.reload", "plugin.reload" }, nameof(Commands.ReloadCommand), "umod.reload");
            AddUniversalCommand(new[] { "umod.unload", "u.unload", "oxide.unload", "o.unload", "plugin.unload" }, nameof(Commands.UnloadCommand), "umod.unload");

            // Add core permission commands
            AddUniversalCommand(new[] { "umod.grant", "u.grant", "oxide.grant", "o.grant", "perm.grant" }, nameof(Commands.GrantCommand), "umod.grant");
            AddUniversalCommand(new[] { "umod.group", "u.group", "oxide.group", "o.group", "perm.group" }, nameof(Commands.GroupCommand), "umod.group");
            AddUniversalCommand(new[] { "umod.revoke", "u.revoke", "oxide.revoke", "o.revoke", "perm.revoke" }, nameof(Commands.RevokeCommand), "umod.revoke");
            AddUniversalCommand(new[] { "umod.show", "u.show", "oxide.show", "o.show", "perm.show" }, nameof(Commands.ShowCommand), "umod.show");
            AddUniversalCommand(new[] { "umod.usergroup", "u.usergroup", "oxide.usergroup", "o.usergroup", "perm.usergroup" }, nameof(Commands.UserGroupCommand), "umod.usergroup");

            // Add core misc commands
            AddUniversalCommand(new[] { "umod.lang", "u.lang", "oxide.lang", "o.lang", "lang" }, nameof(Commands.LangCommand));
            AddUniversalCommand(new[] { "umod.save", "u.save", "oxide.save", "o.save" }, nameof(Commands.SaveCommand));
            AddUniversalCommand(new[] { "umod.version", "u.version", "oxide.version", "o.version" }, nameof(Commands.VersionCommand));

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

                NetTransportSteam transport = NetGame.instance.transport as NetTransportSteam;

                // Check if server is intended to be dedicated
                if (HumanFallFlatExtension.Dedicated)
                {
                    // Make server public/open
                    NetGame.friendly = HumanFallFlatExtension.FriendsOnly;
                    Options.lobbyInviteOnly = HumanFallFlatExtension.InviteOnly ? 1 : 0;
                    transport?.SetJoinable(HumanFallFlatExtension.InviteOnly);
                    transport?.UpdateLobbyType();

                    // Allow join in progress
                    Options.lobbyJoinInProgress = HumanFallFlatExtension.JoinInProgress ? 1 : 0;

                    // Set/override max players
                    Options.lobbyMaxPlayers = HumanFallFlatExtension.MaxPlayers;
                    transport?.UpdateLobbyPlayers();
                    App.instance.OnClientCountChanged();

                    // Use cheat mode to enable/disable some stuff
                    CheatCodes.cheatMode = true;
                }

                if (transport != null)
                {
                    // Override/set server hostname
                    string serverName = $"{Server.Name} | {Server.Players}/{Server.MaxPlayers}";
                    SteamMatchmaking.SetLobbyData(transport.lobbyID, "name", serverName);
                }

                // Let plugins know server startup is complete
                Interface.CallHook("OnServerInitialized", serverInitialized);

                Interface.Oxide.LogInfo($"Server version is: {Server.Version}"); // TODO: Localization

                if (Interface.uMod.ServerConsole != null)
                {
                    Interface.uMod.ServerConsole.Title = () => $"{NetGame.instance.players.Count} | {HumanFallFlatExtension.ServerName}";
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
