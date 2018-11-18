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

        // The command line
        public CommandLine CommandLine;

        // The configuration
        public static bool Dedicated { get; set; } = false;
        public static bool FriendsOnly { get; set; } = false;
        public static bool InviteOnly { get; set; } = false;
        public static bool JoinInProgress { get; set; } = true;
        public static int FpsLimit { get; set; } = 256;
        public static int MaxPlayers { get; set; } = 10;
        public static string ServerName { get; set; } = "My uMod Server";

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
            "DontDestroyOnLoad only work for root GameObjects or components on root GameObjects",
            "Error: Global Illumination requires a graphics device to render",
            "HDR Render Texture not supported, disabling HDR on reflection probe",
            "OnLobbyEnter",
            "OnSessionConnectFail",
            "Trim",
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
            // Parse command-line to set instance directory
            CommandLine = new CommandLine(Environment.GetCommandLineArgs());
            Dedicated = CommandLine.HasVariable("dedicated");
            FriendsOnly = CommandLine.HasVariable("friendsonly");
            InviteOnly = CommandLine.HasVariable("inviteonly");
            JoinInProgress = CommandLine.HasVariable("joininprogress");
            if (CommandLine.HasVariable("servername"))
            {
                CommandLine.GetArgument("servername", out _, out string serverName);
                ServerName = serverName;
            }

            CSharpPluginLoader.PluginReferences.UnionWith(DefaultReferences);

            // Override log message handling
            Application.logMessageReceived += HandleLog;

            // Liten for server console input, if enabled
            if (Interface.uMod.EnableConsole())
            {
                Interface.uMod.ServerConsole.Title = () => ServerName;
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

            // Check if server is intended to be dedicated
            if (Dedicated)
            {
                if (StartupExperienceUI.instance != null)
                {
                    // Remove startup experience UI
                    UnityEngine.Object.Destroy(StartupExperienceUI.instance.gameObject);
                }

                // Limit FPS to reduce CPU usage
                Application.targetFrameRate = FpsLimit;

                // Disable game audio for server
                GameAudio.instance.SetMasterLevel(0f);

                // Forcefully host a game server
                App.state = AppSate.Menu;
                App.instance.HostGame();
            }
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
