using Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using uMod.Libraries;
using uMod.Libraries.Universal;
using uMod.Plugins;

namespace uMod.HumanFallFlat
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public class HumanFallFlatCommands : ICommandSystem
    {
        // The universal provider
        private readonly HumanFallFlatProvider provider = HumanFallFlatProvider.Instance;

        // The console player
        //private readonly HumanFallFlatConsolePlayer consolePlayer;

        // All registered commands
        private readonly IDictionary<string, RegisteredCommand> registeredCommands;

        // Command handler
        private readonly CommandHandler commandHandler;

        // Registered commands
        internal class RegisteredCommand
        {
            /// <summary>
            /// The plugin that handles the command
            /// </summary>
            public readonly Plugin Source;

            /// <summary>
            /// The name of the command
            /// </summary>
            public readonly string Command;

            /// <summary>
            /// The callback
            /// </summary>
            public readonly CommandCallback Callback;

            /// <summary>
            /// The callback
            /// </summary>
            public Action<string> OriginalCallback;

            /// <summary>
            /// Initializes a new instance of the RegisteredCommand class
            /// </summary>
            /// <param name="source"></param>
            /// <param name="command"></param>
            /// <param name="callback"></param>
            public RegisteredCommand(Plugin source, string command, CommandCallback callback)
            {
                Source = source;
                Command = command;
                Callback = callback;
            }
        }

        /// <summary>
        /// Initializes the command system provider
        /// </summary>
        public HumanFallFlatCommands()
        {
            registeredCommands = new Dictionary<string, RegisteredCommand>();
            commandHandler = new CommandHandler(ChatCommandCallback, registeredCommands.ContainsKey);
        }

        private bool ChatCommandCallback(IPlayer caller, string cmd, string[] args)
        {
            return registeredCommands.TryGetValue(cmd, out RegisteredCommand command) && command.Callback(caller, cmd, args);
        }

        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
        {
            // Remove whitespace and convert command to lowercase
            command = command.Trim().ToLowerInvariant();

            // Setup a new universal command
            RegisteredCommand newCommand = new RegisteredCommand(plugin, command, callback);

            // Check if the command can be overridden
            if (!CanOverrideCommand(command))
            {
                throw new CommandAlreadyExistsException(command);
            }

            // Check if command already exists in another plugin
            if (registeredCommands.TryGetValue(command, out RegisteredCommand cmd))
            {
                if (cmd.OriginalCallback != null)
                {
                    newCommand.OriginalCallback = cmd.OriginalCallback;
                }

                string previousPluginName = cmd.Source?.Name ?? "an unknown plugin";
                string newPluginName = plugin?.Name ?? "An unknown plugin";
                string message = $"{newPluginName} has replaced the '{command}' command previously registered by {previousPluginName}";
                Interface.uMod.LogWarning(message);
            }

            // Check if command already exists as a vanilla command
            if (NetChat.serverCommands.commandsStr.ContainsKey(command))
            {
                if (newCommand.OriginalCallback == null)
                {
                    newCommand.OriginalCallback = NetChat.serverCommands.commandsStr[command];
                }

                NetChat.serverCommands.commandsStr.Remove(command);
                if (cmd == null)
                {
                    string newPluginName = plugin?.Name ?? "An unknown plugin";
                    string message = $"{newPluginName} has replaced the '{command}' command previously registered by {provider.GameName.Humanize()}";
                    Interface.uMod.LogWarning(message);
                }
            }

            // Register the command as a chat command
            registeredCommands[command] = newCommand;
            NetChat.serverCommands.RegisterCommand(command, () => { }, null); // TODO: Handle actual callback and set command help
        }

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        public void UnregisterCommand(string command, Plugin plugin)
        {
            if (registeredCommands.TryGetValue(command, out RegisteredCommand cmd))
            {
                // Check if the command belongs to the plugin
                if (plugin == cmd.Source)
                {
                    // Remove the chat command
                    registeredCommands.Remove(command);

                    // If this was originally a vanilla command then restore it, otherwise remove it
                    if (cmd.OriginalCallback != null)
                    {
                        NetChat.serverCommands.commandsStr[cmd.Command] = cmd.OriginalCallback;
                    }
                    else
                    {
                        NetChat.serverCommands.commandsStr.Remove(cmd.Command);
                    }
                }
            }
        }

        /// <summary>
        /// Handles a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool HandleChatMessage(IPlayer player, string message) => commandHandler.HandleChatMessage(player, message);

        #region Command Overriding

        /// <summary>
        /// Checks if a command can be overridden
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private bool CanOverrideCommand(string command)
        {
            if (registeredCommands.TryGetValue(command, out RegisteredCommand cmd))
            {
                if (cmd.Source.IsCorePlugin)
                {
                    return false;
                }
            }

            return !HumanFallFlatExtension.RestrictedCommands.Contains(command);
        }

        #endregion Command Overriding

        #region Command Handling

        /// <summary>
        /// Parses the specified chat command
        /// </summary>
        /// <param name="argstr"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        public void ParseCommand(string argstr, out string cmd, out string[] args)
        {
            List<string> arglist = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool inlongarg = false;

            foreach (char c in argstr)
            {
                if (c == '"')
                {
                    if (inlongarg)
                    {
                        string arg = sb.ToString().Trim();

                        if (!string.IsNullOrEmpty(arg))
                        {
                            arglist.Add(arg);
                        }

                        sb = new StringBuilder();
                        inlongarg = false;
                    }
                    else
                    {
                        inlongarg = true;
                    }
                }
                else if (char.IsWhiteSpace(c) && !inlongarg)
                {
                    string arg = sb.ToString().Trim();

                    if (!string.IsNullOrEmpty(arg))
                    {
                        arglist.Add(arg);
                    }

                    sb = new StringBuilder();
                }
                else
                {
                    sb.Append(c);
                }
            }

            if (sb.Length > 0)
            {
                string arg = sb.ToString().Trim();

                if (!string.IsNullOrEmpty(arg))
                {
                    arglist.Add(arg);
                }
            }

            if (arglist.Count == 0)
            {
                cmd = null;
                args = null;
                return;
            }

            cmd = arglist[0];
            arglist.RemoveAt(0);
            args = arglist.ToArray();
        }

        #endregion Command Handling
    }
}
