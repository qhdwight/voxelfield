using System.Linq;
using Input;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Interfaces
{
    public class ScoreboardInterface : SessionInterfaceBehavior
    {
        [SerializeField] private GameObject m_EntryPrefab = default;
        [SerializeField] private Transform m_EntryHolder = default;

        private ScoreboardEntryInterface[] m_Entries;

        protected override void Awake()
        {
            base.Awake();
            m_Entries = null;
        }

        private void Update()
        {
            // if (ConsoleInterface.Singleton.IsActive || !GameManager.Singleton.IsInGame) return;
            InputProvider inputs = InputProvider.Singleton;
            SetInterfaceActive(inputs.GetInput(InputType.OpenScoreboard));
        }

        public void SortEntries(PlayerContainerArrayProperty players)
        {
            var max = int.MinValue;
            for (var i = 0; i < players.Length; i++)
            {
                Container player = players[i];
                if (player.Has(out StatsComponent stats) && stats.kills.HasValue)
                {
                    if (stats.kills > max)
                    {
                        m_Entries[i].transform.SetAsFirstSibling();
                        max = stats.kills;
                    }
                }
            }
        }

        public override void Render(Container session)
        {
            if (session.Without(out PlayerContainerArrayProperty players)) return;

            if (m_Entries == null)
                m_Entries = Enumerable.Range(0, players.Length).Select(i =>
                {
                    GameObject instance = Instantiate(m_EntryPrefab, m_EntryHolder);
                    var entry = instance.GetComponent<ScoreboardEntryInterface>();
                    return entry;
                }).ToArray();

            for (var i = 0; i < players.Length; i++)
            {
                Container player = players[i];
                ScoreboardEntryInterface entry = m_Entries[i];
                entry.Render(player);
            }

            SortEntries(players);
        }
    }
}