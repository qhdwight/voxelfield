using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Steamworks;
using Swihoni.Sessions;
using Swihoni.Sessions.Interfaces;
using UnityEngine;
using Voxelfield.Integration;
using SteamImage = Steamworks.Data.Image;

namespace Voxelfield.Interface
{
    public struct Entry
    {
        public StringBuilder text;
        public Texture2D texture;
    }

    public class SteamFriendsInterface : SessionInterfaceBehavior
    {
        private class FriendComparer : IEqualityComparer<Friend>
        {
            public bool Equals(Friend f1, Friend f2) => f1.Id.Value.Equals(f2.Id.Value);
            public int GetHashCode(Friend friend) => friend.Id.Value.GetHashCode();
        }

        private static readonly Comparer<Friend> StateSorter = Comparer<Friend>.Create((f1, f2) => StateOrder[f1.State].CompareTo(StateOrder[f2.State]));
        private static readonly Comparer<Friend> InGameSorter = Comparer<Friend>.Create((f1, f2) => f2.GameInfo.HasValue.CompareTo(f1.GameInfo.HasValue));
        private static readonly Dictionary<FriendState, int> StateOrder = new List<FriendState>
        {
            FriendState.Online, FriendState.LookingToPlay, FriendState.LookingToTrade,
            FriendState.Away, FriendState.Snooze, FriendState.Busy, FriendState.Invisible, FriendState.Offline
        }.Select((friend, i) => new {friend, i}).ToDictionary(pair => pair.friend, pair => pair.i);

        [SerializeField] private SteamFriendEntryInterface m_EntryPrefab = default;
        [SerializeField] private Transform m_EntryParent = default;

        private readonly Dictionary<Friend, Entry> m_Entries = new Dictionary<Friend, Entry>(new FriendComparer());
        private readonly List<SteamFriendEntryInterface> m_InterfaceEntries = new List<SteamFriendEntryInterface>();
        private readonly Mutex m_FriendMutex = new Mutex();

        protected override void OnSetInterfaceActive(bool isActive)
        {
            if (isActive)
            {
                SteamClientBehavior.RunOrWait(() =>
                {
                    SteamFriends.OnPersonaStateChange += OnPersonaStateChange;
                    foreach (Friend friend in SteamFriends.GetFriends()) UpdateFriend(friend);
                    Render();
                });
            }
            else
            {
                if (SteamClient.IsValid) SteamFriends.OnPersonaStateChange -= OnPersonaStateChange;
            }
        }

        private void OnDestroy() => m_FriendMutex.Dispose();

        public override void Render(in SessionContext context) { }

        private void OnPersonaStateChange(Friend friend)
        {
            UpdateFriend(friend);
            Render();
        }

        private async void UpdateFriend(Friend friend)
        {
            try
            {
                m_FriendMutex.WaitOne();

                Entry entry = default;
                
                SteamImage? avatarNullable = await SteamFriends.GetSmallAvatarAsync(friend.Id);
                Texture2D texture = null;
                if (avatarNullable is { } avatar)
                {
                    texture = new Texture2D((int) avatar.Width, (int) avatar.Height, TextureFormat.RGBA32, false, true);
                    texture.LoadRawTextureData(avatar.Data);
                    texture.Apply();
                }
                entry.texture = texture;

                var builder = new StringBuilder("<color=#");
                if (friend.GameInfo != null) builder.Append("84c782");
                else
                    switch (friend.State)
                    {
                        case FriendState.Online:
                            builder.Append("66c0f4");
                            break;
                        case FriendState.Snooze:
                        case FriendState.Away:
                            builder.Append("376a87");
                            break;
                        default:
                            builder.Append("4a4a4a");
                            break;
                    }
                builder.Append(">").Append("<noparse>").Append(friend.Name).Append("</noparse>").Append("</color>");
                entry.text = builder;

                m_Entries[friend] = entry;
            }
            finally
            {
                m_FriendMutex.ReleaseMutex();
            }
        }

        private void Render()
        {
            try
            {
                m_FriendMutex.WaitOne();

                while (m_InterfaceEntries.Count > m_Entries.Count)
                    Destroy(m_InterfaceEntries[m_InterfaceEntries.Count - 1]);
                while (m_InterfaceEntries.Count < m_Entries.Count)
                    m_InterfaceEntries.Add(Instantiate(m_EntryPrefab, m_EntryParent));

                IEnumerable<Entry> ordered = m_Entries.OrderBy(p => p.Key, StateSorter)
                                                      .ThenBy(p => p.Key, InGameSorter)
                                                      .ThenBy(p => p.Key.Name)
                                                      .Select(pair => pair.Value);
                var i = 0;
                foreach (Entry e in ordered)
                    m_InterfaceEntries[i++].Render(e);
            }
            finally
            {
                m_FriendMutex.ReleaseMutex();
            }
        }
    }
}