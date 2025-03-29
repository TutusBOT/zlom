using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoomVariantsEntry
{
    public DungeonGenerator.RoomSize size;

    public RoomVariantData variantData = new RoomVariantData();
}

[Serializable]
public class RoomVariantData
{
    public List<GameObject> normalVariants;
    public bool allowDoorsAnywhere = true;
    public List<Vector2Int> allowedNorthDoors;
    public List<Vector2Int> allowedSouthDoors;
    public List<Vector2Int> allowedEastDoors;
    public List<Vector2Int> allowedWestDoors;
}

[Serializable]
public class RoomVariantsWrapper
{
    public List<RoomVariantsEntry> roomVariantsList = new List<RoomVariantsEntry>();

    public Dictionary<DungeonGenerator.RoomSize, RoomVariantData> ToDictionary()
    {
        Dictionary<DungeonGenerator.RoomSize, RoomVariantData> dictionary =
            new Dictionary<DungeonGenerator.RoomSize, RoomVariantData>();

        foreach (RoomVariantsEntry entry in roomVariantsList)
        {
            dictionary[entry.size] = entry.variantData;
        }

        return dictionary;
    }

    public void FromDictionary(Dictionary<DungeonGenerator.RoomSize, RoomVariantData> dictionary)
    {
        roomVariantsList = new List<RoomVariantsEntry>();
        foreach (var pair in dictionary)
        {
            RoomVariantsEntry entry = new RoomVariantsEntry
            {
                size = pair.Key,
                variantData = pair.Value,
            };
            roomVariantsList.Add(entry);
        }
    }
}
