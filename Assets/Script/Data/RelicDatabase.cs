using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RelicDatabase", menuName = "AetherRift/Data/Relic Database")]
public class RelicDatabase : ScriptableObject
{
    [SerializeField] private List<RelicData> relics = new List<RelicData>();

    public IReadOnlyList<RelicData> Relics => relics;

    public RelicData FindRelic(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return null;

        relics ??= new List<RelicData>();

        for (int i = 0; i < relics.Count; i++)
        {
            RelicData relicData = relics[i];

            if (relicData != null && string.Equals(relicData.RelicId, relicId, StringComparison.Ordinal))
                return relicData;
        }

        return null;
    }

    public IReadOnlyList<RelicData> GetRandomRelics(int count)
    {
        if (count <= 0)
            return Array.Empty<RelicData>();

        relics ??= new List<RelicData>();
        List<RelicData> candidates = new List<RelicData>();
        HashSet<string> addedRelicIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < relics.Count; i++)
        {
            RelicData relicData = relics[i];

            if (relicData == null || string.IsNullOrWhiteSpace(relicData.RelicId) || !addedRelicIds.Add(relicData.RelicId))
                continue;

            candidates.Add(relicData);
        }

        int resultCount = Mathf.Min(count, candidates.Count);

        for (int i = 0; i < resultCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, candidates.Count);
            (candidates[i], candidates[randomIndex]) = (candidates[randomIndex], candidates[i]);
        }

        if (resultCount < candidates.Count)
            candidates.RemoveRange(resultCount, candidates.Count - resultCount);

        return candidates;
    }

    private void OnValidate()
    {
        relics ??= new List<RelicData>();
    }
}
