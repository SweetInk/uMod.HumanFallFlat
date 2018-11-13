extern alias References;

using Multiplayer;
using References::ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using uMod.Libraries.Universal;

namespace uMod.HumanFallFlat
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    public class HumanFallFlatPlayerManager : IPlayerManager
    {
        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        private struct PlayerRecord
        {
            public string Name;
            public string Id;
        }

        private IDictionary<string, PlayerRecord> playerData;
        private IDictionary<string, HumanFallFlatPlayer> allPlayers;
        private IDictionary<string, HumanFallFlatPlayer> connectedPlayers;
        private const string dataFileName = "umod";

        internal void Initialize()
        {
            playerData = ProtoStorage.Load<Dictionary<string, PlayerRecord>>(dataFileName) ?? new Dictionary<string, PlayerRecord>();
            allPlayers = new Dictionary<string, HumanFallFlatPlayer>();
            connectedPlayers = new Dictionary<string, HumanFallFlatPlayer>();

            foreach (KeyValuePair<string, PlayerRecord> pair in playerData)
            {
                allPlayers.Add(pair.Key, new HumanFallFlatPlayer(pair.Value.Id, pair.Value.Name));
            }
        }

        internal void PlayerJoin(string userId, string name)
        {
            if (playerData.TryGetValue(userId, out PlayerRecord record))
            {
                record.Name = name;
                playerData[userId] = record;
                allPlayers.Remove(userId);
                allPlayers.Add(userId, new HumanFallFlatPlayer(userId, name));
            }
            else
            {
                record = new PlayerRecord { Id = userId, Name = name };
                playerData.Add(userId, record);
                allPlayers.Add(userId, new HumanFallFlatPlayer(userId, name));
            }
        }

        internal void PlayerConnected(NetPlayer netPlayer)
        {
            string id = netPlayer.host.connection.ToString();
            allPlayers[id] = new HumanFallFlatPlayer(netPlayer);
            connectedPlayers[id] = new HumanFallFlatPlayer(netPlayer);
        }

        internal void PlayerDisconnected(NetPlayer netPlayer) => connectedPlayers.Remove(netPlayer.host.connection.ToString());

        internal void SavePlayerData() => ProtoStorage.Save(playerData, dataFileName);

        #region Player Finding

        /// <summary>
        /// Gets all players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> All => allPlayers.Values.Cast<IPlayer>();

        /// <summary>
        /// Gets all connected players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> Connected => connectedPlayers.Values.Cast<IPlayer>();

        /// <summary>
        /// Gets all sleeping players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> Sleeping => null; // TODO: Implement if/when possible

        /// <summary>
        /// Finds a single player given unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IPlayer FindPlayerById(string id)
        {
            HumanFallFlatPlayer player;
            return allPlayers.TryGetValue(id, out player) ? player : null;
        }

        /// <summary>
        /// Finds a single connected player given game object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public IPlayer FindPlayerByObj(object obj) => connectedPlayers.Values.FirstOrDefault(p => p.Object == obj);

        /// <summary>
        /// Finds a single player given a partial name or unique ID (case-insensitive, wildcards accepted, multiple matches returns null)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        public IPlayer FindPlayer(string partialNameOrId)
        {
            IPlayer[] players = FindPlayers(partialNameOrId).ToArray();
            return players.Length == 1 ? players[0] : null;
        }

        /// <summary>
        /// Finds any number of players given a partial name or unique ID (case-insensitive, wildcards accepted)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        public IEnumerable<IPlayer> FindPlayers(string partialNameOrId)
        {
            foreach (HumanFallFlatPlayer player in allPlayers.Values)
            {
                if (player.Name != null && player.Name.IndexOf(partialNameOrId, StringComparison.OrdinalIgnoreCase) >= 0 || player.Id == partialNameOrId)
                {
                    yield return player;
                }
            }
        }

        #endregion Player Finding
    }
}
