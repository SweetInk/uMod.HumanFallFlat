using Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using uMod.Extensions;
using uMod.Plugins;
using uMod.Unity;
using UnityEngine;

namespace uMod.HumanFallFlat
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class HumanFallFlatExtension : Extension
    {
        // Get assembly info
        internal static Assembly Assembly = Assembly.GetExecutingAssembly();
        internal static AssemblyName AssemblyName = Assembly.GetName();
        internal static VersionNumber AssemblyVersion = new VersionNumber(AssemblyName.Version.Major, AssemblyName.Version.Minor, AssemblyName.Version.Build);
        internal static string AssemblyAuthors = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(Assembly, typeof(AssemblyCompanyAttribute), false)).Company;

        /// <summary>
        /// Gets whether this extension is for a specific game
        /// </summary>
        public override bool IsGameExtension => true;

        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "HumanFallFlat";

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => AssemblyAuthors;

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => AssemblyVersion;

        /// <summary>
        /// Gets the branch of this extension
        /// </summary>
        public override string Branch => "public"; // TODO: Handle this programmatically

        // Commands that a plugin can't override
        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            ""
        };

        /// <summary>
        /// Default game-specific references for use in plugins
        /// </summary>
        public override string[] DefaultReferences => new[]
        {
            "HumanAPI", "System.Drawing", "UnityEngine.AIModule", "UnityEngine.AssetBundleModule", "UnityEngine.CoreModule", "UnityEngine.GridModule",
            "UnityEngine.ImageConversionModule", "UnityEngine.Networking", "UnityEngine.PhysicsModule", "UnityEngine.TerrainModule", "UnityEngine.TerrainPhysicsModule",
            "UnityEngine.UI", "UnityEngine.UIModule", "UnityEngine.UIElementsModule", "UnityEngine.UnityWebRequestAudioModule", "UnityEngine.UnityWebRequestModule",
            "UnityEngine.UnityWebRequestTextureModule", "UnityEngine.UnityWebRequestWWWModule", "UnityEngine.VehiclesModule", "UnityEngine.WebModule"
        };

        /// <summary>
        /// List of assemblies allowed for use in plugins
        /// </summary>
        public override string[] WhitelistAssemblies => new[]
        {
            "Assembly-CSharp", "HumanAPI", "mscorlib", "uMod", "System", "System.Core", "UnityEngine", "UnityEngine.CoreModule"
        };

        /// <summary>
        /// List of namespaces allowed for use in plugins
        /// </summary>
        public override string[] WhitelistNamespaces => new[]
        {
            "ProtoBuf", "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine"
        };

        /// <summary>
        /// List of filter matches to apply to console output
        /// </summary>
        public static string[] Filter =
        {
            "Error: Global Illumination requires a graphics device to render",
            "OnLobbyEnter",
            "OnSessionConnectFail",
            "Wrong state",
            "invalid torque"
        };

        /// <summary>
        /// Initializes a new instance of the HumanFallFlatExtension class
        /// </summary>
        /// <param name="manager"></param>
        public HumanFallFlatExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            Manager.RegisterPluginLoader(new HumanFallFlatPluginLoader());
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="directory"></param>
        public override void LoadPluginWatchers(string directory)
        {
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            CSharpPluginLoader.PluginReferences.UnionWith(DefaultReferences);

            // Override log message handling
            Application.logMessageReceived += HandleLog;

            // Liten for server console input, if enabled
            if (Interface.uMod.EnableConsole())
            {
                Interface.uMod.ServerConsole.Input += ServerConsoleOnInput;
                Interface.uMod.ServerConsole.Completion = input =>
                {
                    input = input.Trim().TrimStart('/');
                    if (!string.IsNullOrEmpty(input))
                    {
                        // TODO: Handle other 3 dictionaries where commands may be stored
                        //Shell.commands.commands
                        //NetChat.clientCommands.commands
                        //NetChat.serverCommands.commandsStr
                        return NetChat.serverCommands.commands.Where(c => c.Key.Contains(input.ToLower())).ToList().ConvertAll(c => c.Key).OrderBy(c => c).ToArray();
                    }

                    return null;
                };
            }

            // Remove startup experience UI
            if (StartupExperienceUI.instance != null)
            {
                UnityEngine.Object.Destroy(StartupExperienceUI.instance.gameObject);
            }

            // Disable game audio for server
            GameAudio.instance.SetMasterLevel(0f);

            // Limit FPS to reduce CPU usage
            Application.targetFrameRate = 256;// TODO: Make command-line argument - fpslimit

            // Make server public/open
            NetGame.friendly = false; // TODO: Make command-line argument - friendsonly
            Options.lobbyInviteOnly = 0; // TODO: Make command-line argument - inviteonly
            (NetGame.instance.transport as NetTransportSteam).UpdateLobbyType();

            // Allow join in progress
            Options.lobbyJoinInProgress = 1; // TODO: Make command-line argument - joininprogress

            // Set/override max players
            Options.lobbyMaxPlayers = 10; // TODO: Make command-line argument - maxplayers
            (NetGame.instance.transport as NetTransportSteam).UpdateLobbyPlayers();
            App.instance.OnClientCountChanged();

            // Use cheat mode to enable/disable some stuff
            CheatCodes.cheatMode = true;

            // Forcefully host a game server
            App.state = AppSate.ServerHost; // ServerLobby?
            NetGame.instance.HostGame();
        }

        private static void ServerConsoleOnInput(string input)
        {
            input = input.Trim().TrimStart('/');
            if (!string.IsNullOrEmpty(input))
            {
                // TODO: Fix handling of non-native/universal commands
                NetChat.serverCommands.Execute(input);
            }
        }

        private static void HandleLog(string message, string stackTrace, LogType logType)
        {
            if (!string.IsNullOrEmpty(message) && !Filter.Any(message.StartsWith))
            {
                Interface.uMod.RootLogger.HandleMessage(message, stackTrace, logType.ToLogType());
            }
        }
    }
}
