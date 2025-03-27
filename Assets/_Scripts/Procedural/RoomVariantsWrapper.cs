using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoomVariantsEntry {
  public DungeonGenerator.RoomSize size;
  public List<GameObject> variants;
}

[Serializable]
public class RoomVariantData {
  public List<GameObject> normalVariants;
}

[Serializable]
public class RoomVariantsWrapper {
  public List<RoomVariantsEntry> roomVariantsList = new List<RoomVariantsEntry>();

  public Dictionary<DungeonGenerator.RoomSize, RoomVariantData> ToDictionary() {
    Dictionary<DungeonGenerator.RoomSize, RoomVariantData> dictionary =
        new Dictionary<DungeonGenerator.RoomSize, RoomVariantData>();

    foreach (RoomVariantsEntry entry in roomVariantsList) {
      RoomVariantData data = new RoomVariantData {
        normalVariants = entry.variants
      };
      dictionary[entry.size] = data;
    }

    return dictionary;
  }

  public void FromDictionary(Dictionary<DungeonGenerator.RoomSize, RoomVariantData> dictionary) {
    roomVariantsList = new List<RoomVariantsEntry>();
    foreach (var pair in dictionary) {
      RoomVariantsEntry entry = new RoomVariantsEntry {
        size = pair.Key,
        variants = pair.Value.normalVariants
      };
      roomVariantsList.Add(entry);
    }
  }
}