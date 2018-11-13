using uMod.Libraries.Universal;

namespace uMod.HumanFallFlat
{
    /// <summary>
    /// Provides Universal functionality for the game "Human: Fall Flat"
    /// </summary>
    public class HumanFallFlatProvider : IUniversalProvider
    {
        /// <summary>
        /// Gets the name of the game for which this provider provides
        /// </summary>
        public string GameName => "HumanFallFlat";

        /// <summary>
        /// Gets the Steam app ID of the game's client, if available
        /// </summary>
        public uint ClientAppId => 477160;

        /// <summary>
        /// Gets the Steam app ID of the game's server, if available
        /// </summary>
        public uint ServerAppId => 477160;

        /// <summary>
        /// Gets the singleton instance of this provider
        /// </summary>
        internal static HumanFallFlatProvider Instance { get; private set; }

        public HumanFallFlatProvider()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the player manager
        /// </summary>
        public HumanFallFlatPlayerManager PlayerManager { get; private set; }

        /// <summary>
        /// Gets the command system provider
        /// </summary>
        public HumanFallFlatCommands CommandSystem { get; private set; }

        /// <summary>
        /// Creates the game-specific server object
        /// </summary>
        /// <returns></returns>
        public IServer CreateServer() => new HumanFallFlatServer();

        /// <summary>
        /// Creates the game-specific player manager object
        /// </summary>
        /// <returns></returns>
        public IPlayerManager CreatePlayerManager()
        {
            PlayerManager = new HumanFallFlatPlayerManager();
            PlayerManager.Initialize();
            return PlayerManager;
        }

        /// <summary>
        /// Creates the game-specific command system provider object
        /// </summary>
        /// <returns></returns>
        public ICommandSystem CreateCommandSystemProvider() => CommandSystem = new HumanFallFlatCommands();

        /// <summary>
        /// Formats the text with markup as specified in uMod.Libraries.Formatter
        /// into the game-specific markup language
        /// </summary>
        /// <param name="text">text to format</param>
        /// <returns>formatted text</returns>
        public string FormatText(string text) => Formatter.ToUnity(text); // TODO: Check
    }
}
