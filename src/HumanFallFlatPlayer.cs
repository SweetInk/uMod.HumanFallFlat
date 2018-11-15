using Multiplayer;
using Steamworks;
using System;
using System.Globalization;
using uMod.Libraries;
using uMod.Libraries.Universal;
using UnityEngine;

namespace uMod.HumanFallFlat
{
    /// <summary>
    /// Represents a player, either connected or not
    /// </summary>
    public class HumanFallFlatPlayer : IPlayer, IEquatable<IPlayer>
    {
        private static Permission libPerms;

        private readonly NetPlayer netPlayer;

        internal HumanFallFlatPlayer(string userId, string name)
        {
            // Get perms library
            if (libPerms == null)
            {
                libPerms = Interface.uMod.GetLibrary<Permission>();
            }

            // Store user details
            Name = name.Sanitize();
            Id = userId;
        }

        internal HumanFallFlatPlayer(NetPlayer netPlayer) : this(netPlayer.host.connection.ToString(), netPlayer.host.name)
        {
            // Store user object
            this.netPlayer = netPlayer;
        }

        #region Objects

        /// <summary>
        /// Gets the object that backs the player
        /// </summary>
        public object Object => netPlayer;

        /// <summary>
        /// Gets the player's last command type
        /// </summary>
        public CommandType LastCommand { get; set; }

        #endregion Objects

        #region Information

        /// <summary>
        /// Gets/sets the name for the player
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the ID for the player (unique within the current game)
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the player's IP address
        /// </summary>
        public string Address
        {
            get
            {
                SteamNetworking.GetP2PSessionState((CSteamID)netPlayer.host.connection, out P2PSessionState_t sessionState);
                uint ip = sessionState.m_nRemoteIP;
                return string.Concat(ip >> 24 & 255, ".", ip >> 16 & 255, ".", ip >> 8 & 255, ".", ip & 255);
            }
        }

        /// <summary>
        /// Gets the player's average network ping
        /// </summary>
        public int Ping => Convert.ToInt32(0); // TODO: Implement when possible

        /// <summary>
        /// Gets the player's language
        /// </summary>
        public CultureInfo Language => CultureInfo.GetCultureInfo("en"); // TODO: Implement when possible

        /// <summary>
        /// Returns if the player is admin
        /// </summary>
        public bool IsAdmin => netPlayer.host.isLocal; // TODO: Implement when possible

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        public bool IsBanned => NetGame.kickedUsers.Contains(netPlayer.host.connection);

        /// <summary>
        /// Returns if the player is connected
        /// </summary>
        public bool IsConnected => NetGame.instance.clients.Contains(netPlayer.host);

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        public bool IsSleeping => netPlayer.human.state == HumanState.Unconscious;

        /// <summary>
        /// Returns if the player is the server
        /// </summary>
        public bool IsServer => netPlayer.host.isLocal;

        #endregion Information

        #region Administration

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string reason, TimeSpan duration = default(TimeSpan))
        {
            // Check if already banned
            if (!IsBanned)
            {
                // Ban and kick user
                NetGame.instance.Kick(netPlayer.host); // TODO: Save these somewhere for persistence
            }
        }

        /// <summary>
        /// Gets the amount of time remaining on the player's ban
        /// </summary>
        public TimeSpan BanTimeRemaining => TimeSpan.MaxValue; // TODO: Implement when possible

        /// <summary>
        /// Heals the player's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Heal(float amount) => throw new NotImplementedException(); // TODO: Implement when possible

        /// <summary>
        /// Gets/sets the player's health
        /// </summary>
        public float Health
        {
            get => 100f; // TODO: Implement when possible
            set => throw new NotImplementedException(); // TODO: Implement when possible
        }

        /// <summary>
        /// Damages the player's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount)
        {
            netPlayer.human.unconsciousTime = amount;
            netPlayer.human.state = HumanState.Unconscious;
        }

        /// <summary>
        /// Kicks the player from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason)
        {
            using (NetStream netStream = NetGame.BeginMessage(NetMsgId.Kick))
            {
                NetGame.instance.SendReliable(netPlayer.host, netStream);
            }
        }

        /// <summary>
        /// Causes the player's character to die
        /// </summary>
        public void Kill()
        {
            netPlayer.human.state = HumanState.Dead;
            Game.instance.Respawn(netPlayer.human, Vector3.zero);
        }

        /// <summary>
        /// Gets/sets the player's maximum health
        /// </summary>
        public float MaxHealth
        {
            get => 100f; // TODO: Implement when possible
            set => throw new NotImplementedException(); // TODO: Implement when possible
        }

        /// <summary>
        /// Renames the player to specified name
        /// <param name="name"></param>
        /// </summary>
        public void Rename(string name)
        {
            netPlayer.host.name = name;
            netPlayer.nametag.textMesh.text = name;

            netPlayer.IPlayer.Name = name;
            libPerms.UpdateNickname(Id, name);
        }

        /// <summary>
        /// Teleports the player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z)
        {
            netPlayer.human.state = HumanState.Spawning;
            netPlayer.human.SetPosition(new Vector3(x, y, z));
            netPlayer.human.Reset();
            netPlayer.human.state = HumanState.Idle;
        }

        /// <summary>
        /// Teleports the player's character to the specified generic position
        /// </summary>
        /// <param name="pos"></param>
        public void Teleport(GenericPosition pos) => Teleport(pos.X, pos.Y, pos.Z);

        /// <summary>
        /// Unbans the player
        /// </summary>
        public void Unban()
        {
            // Check if unbanned already
            if (IsBanned)
            {
                // Set to unbanned
                NetGame.kickedUsers.Remove(netPlayer.host.connection);
            }
        }

        #endregion Administration

        #region Location

        /// <summary>
        /// Gets the position of the player
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Position(out float x, out float y, out float z)
        {
            Vector3 pos = netPlayer.human.transform.position;
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>
        /// Gets the position of the player
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position()
        {
            Vector3 pos = netPlayer.human.transform.position;
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #endregion Location

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message and prefix to the player
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Message(string message, string prefix, params object[] args)
        {
            message = args.Length > 0 ? string.Format(Formatter.ToUnity(message), args) : Formatter.ToUnity(message);
            using (NetStream netStream = NetGame.BeginMessage(NetMsgId.Chat))
            {
                netStream.WriteNetId(NetGame.instance.local.hostId);
                netStream.Write(prefix ?? string.Empty);
                netStream.Write(message);
                NetGame.instance.SendReliable(netPlayer.host, netStream);
            }
        }

        /// <summary>
        /// Sends the specified message to the player
        /// </summary>
        /// <param name="message"></param>
        public void Message(string message) => Message(message, null);

        /// <summary>
        /// Replies to the player with the specified message and prefix
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Reply(string message, string prefix, params object[] args) => Message(message, prefix, args);

        /// <summary>
        /// Replies to the player with the specified message
        /// </summary>
        /// <param name="message"></param>
        public void Reply(string message) => Message(message, null);

        /// <summary>
        /// Runs the specified console command on the player
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            throw new NotImplementedException(); // TODO: Implement when possible
        }

        #endregion Chat and Commands

        #region Permissions

        /// <summary>
        /// Gets if the player has the specified permission
        /// </summary>
        /// <param name="perm"></param>
        /// <returns></returns>
        public bool HasPermission(string perm) => libPerms.UserHasPermission(Id, perm);

        /// <summary>
        /// Grants the specified permission on this player
        /// </summary>
        /// <param name="perm"></param>
        public void GrantPermission(string perm) => libPerms.GrantUserPermission(Id, perm, null);

        /// <summary>
        /// Strips the specified permission from this player
        /// </summary>
        /// <param name="perm"></param>
        public void RevokePermission(string perm) => libPerms.RevokeUserPermission(Id, perm);

        /// <summary>
        /// Gets if the player belongs to the specified group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool BelongsToGroup(string group) => libPerms.UserHasGroup(Id, group);

        /// <summary>
        /// Adds the player to the specified group
        /// </summary>
        /// <param name="group"></param>
        public void AddToGroup(string group) => libPerms.AddUserGroup(Id, group);

        /// <summary>
        /// Removes the player from the specified group
        /// </summary>
        /// <param name="group"></param>
        public void RemoveFromGroup(string group) => libPerms.RemoveUserGroup(Id, group);

        #endregion Permissions

        #region Operator Overloads

        /// <summary>
        /// Returns if player's unique ID is equal to another player's unique ID
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IPlayer other) => Id == other?.Id;

        /// <summary>
        /// Returns if player's object is equal to another player's object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) => obj is IPlayer && Id == ((IPlayer)obj).Id;

        /// <summary>
        /// Gets the hash code of the player's unique ID
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>
        /// Returns a human readable string representation of this IPlayer
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"HumanFallFlatPlayer[{Id}, {Name}]";

        #endregion Operator Overloads
    }
}
