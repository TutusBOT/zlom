using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum CellType {
  Empty,
  Room
}

public class Room {
  public int xOrigin, zOrigin;
  public int width, length;
  public List<Room> connections;

  public Room(int x, int z, int w, int l) {
    xOrigin = x;
    zOrigin = z;
    width = w;
    length = l;
    connections = new List<Room>();
  }
}

public class DungeonGenerator : MonoBehaviour {
  public const int DEFAULT_GRID_SIZE_X = 10;
  public const int DEFAULT_GRID_SIZE_Z = 10;
  public const int DEFAULT_ROOM_COUNT = 8;

  public int gridSizeX = DEFAULT_GRID_SIZE_X;
  public int gridSizeZ = DEFAULT_GRID_SIZE_Z;
  public int roomCount = DEFAULT_ROOM_COUNT;
  public GameObject startingRoomPrefab;
  

  [Serializable]
  public struct RoomSize {
    public int width;
    public int length;

    public override bool Equals(object obj) {
      if (obj == null || GetType() != obj.GetType()) {
        return false;
      }

      RoomSize other = (RoomSize)obj;
      return (width == other.width) && (length == other.length);
    }

    public override int GetHashCode() {
      return HashCode.Combine(width, length);
    }
  }

  public RoomVariantsWrapper roomVariantsWrapper = new RoomVariantsWrapper();

  private CellType[,] grid;
  private List<Room> rooms = new List<Room>();

  void Start() {
    GenerateDungeon();
  }

  void GenerateDungeon() {
    grid = new CellType[gridSizeX, gridSizeZ];
    rooms.Clear();

    // Clear any existing dungeon objects. Important to do this when regenerating.
    foreach (Transform child in transform) {
      Destroy(child.gameObject);
    }

    PlaceRooms();
    ConnectRooms();
    RenderDungeon();
  }

  void PlaceRooms() {
  if (roomCount <= 0) return;

  // Get available room sizes from variants
  List<RoomSize> availableSizes = new List<RoomSize>(roomVariantsWrapper.ToDictionary().Keys);
  if (availableSizes.Count == 0) {
    Debug.LogWarning("No room variants defined. Using 1x1 rooms as fallback.");
    availableSizes.Add(new RoomSize { width = 1, length = 1 });
  }

  int startX = Random.Range(0, gridSizeX - 1);
  int startZ = Random.Range(0, gridSizeZ - 1);
  
  Room firstRoom = new Room(startX, startZ, 1, 1); // Always 1x1
  rooms.Add(firstRoom);
  MarkRoomInGrid(firstRoom);

  int placedRooms = 1;
  int attempts = 0;
  int maxAttempts = roomCount * 10;

  while (placedRooms < roomCount && attempts < maxAttempts) {
      attempts++;

      Room currentRoom = rooms[Random.Range(0, rooms.Count)];

      List<PossibleRoomPlacement> possiblePlacements = GetPossibleRoomPlacements(currentRoom, availableSizes);

      if (possiblePlacements.Count > 0) {
        PossibleRoomPlacement placement = possiblePlacements[Random.Range(0, possiblePlacements.Count)];
        
        Room newRoom = new Room(placement.x, placement.z, placement.size.width, placement.size.length);
        rooms.Add(newRoom);
        MarkRoomInGrid(newRoom);
        placedRooms++;

        currentRoom.connections.Add(newRoom);
        newRoom.connections.Add(currentRoom);
      } else if (attempts % rooms.Count == 0) {
        Debug.LogWarning($"Struggling to place more rooms. Placed {placedRooms} of {roomCount} after {attempts} attempts.");
      }
    }

    Debug.Log($"Room placement complete. Placed {placedRooms} rooms out of {roomCount} after {attempts} attempts.");
  }

  struct PossibleRoomPlacement {
    public int x, z;
    public RoomSize size;
    public Direction direction;
  }

  enum Direction {
    North, South, East, West
  }

  // Mark all cells for a room as Room type in the grid
  void MarkRoomInGrid(Room room) {
    for (int x = room.xOrigin; x < room.xOrigin + room.width; x++) {
      for (int z = room.zOrigin; z < room.zOrigin + room.length; z++) {
        grid[x, z] = CellType.Room;
      }
    }
  }

  // Check if a room can be placed at the specified location
  bool CanPlaceRoom(int x, int z, int width, int length) {
    // Check boundaries
    if (x < 0 || z < 0 || x + width > gridSizeX || z + length > gridSizeZ) {
      return false;
    }

    // Check if all cells are empty
    for (int i = x; i < x + width; i++) {
      for (int j = z; j < z + length; j++) {
        if (grid[i, j] != CellType.Empty) {
          return false;
        }
      }
    }

    return true;
  }

  List<PossibleRoomPlacement> GetPossibleRoomPlacements(Room room, List<RoomSize> availableSizes) {
    List<PossibleRoomPlacement> placements = new List<PossibleRoomPlacement>();

    foreach (RoomSize size in availableSizes) {
      // Try placing room to the North
      for (int x = room.xOrigin; x < room.xOrigin + room.width; x++) {
        int z = room.zOrigin + room.length;
        if (CanPlaceRoom(x - (size.width - 1) / 2, z, size.width, size.length)) {
          placements.Add(new PossibleRoomPlacement { 
            x = x - (size.width - 1) / 2, 
            z = z, 
            size = size, 
            direction = Direction.North 
          });
        }
      }
      
      // Try placing room to the South
      for (int x = room.xOrigin; x < room.xOrigin + room.width; x++) {
        int z = room.zOrigin - size.length;
        if (CanPlaceRoom(x - (size.width - 1) / 2, z, size.width, size.length)) {
          placements.Add(new PossibleRoomPlacement { 
            x = x - (size.width - 1) / 2, 
            z = z, 
            size = size, 
            direction = Direction.South 
          });
        }
      }
      
      // Try placing room to the East
      for (int z = room.zOrigin; z < room.zOrigin + room.length; z++) {
        int x = room.xOrigin + room.width;
        if (CanPlaceRoom(x, z - (size.length - 1) / 2, size.width, size.length)) {
          placements.Add(new PossibleRoomPlacement { 
            x = x, 
            z = z - (size.length - 1) / 2, 
            size = size, 
            direction = Direction.East 
          });
        }
      }
      
      // Try placing room to the West
      for (int z = room.zOrigin; z < room.zOrigin + room.length; z++) {
        int x = room.xOrigin - size.width;
        if (CanPlaceRoom(x, z - (size.length - 1) / 2, size.width, size.length)) {
          placements.Add(new PossibleRoomPlacement { 
            x = x, 
            z = z - (size.length - 1) / 2, 
            size = size, 
            direction = Direction.West 
          });
        }
      }
    }

    return placements;
  }

  void ConnectRooms() {
    foreach (Room roomA in rooms) {
      foreach (Room roomB in rooms) {
        if (roomA != roomB && !roomA.connections.Contains(roomB)) {
          if (AreRoomsAdjacent(roomA, roomB)) {
            roomA.connections.Add(roomB);
            roomB.connections.Add(roomA);
          }
        }
      }
    }
  }

  bool AreRoomsAdjacent(Room roomA, Room roomB) {
    // Check if roomB is to the right of roomA
    if (roomA.xOrigin + roomA.width == roomB.xOrigin &&
        !(roomA.zOrigin + roomA.length <= roomB.zOrigin ||
          roomB.zOrigin + roomB.length <= roomA.zOrigin)) {
      return true;
    }

    // Check if roomB is to the left of roomA
    if (roomB.xOrigin + roomB.width == roomA.xOrigin &&
        !(roomA.zOrigin + roomA.length <= roomB.zOrigin ||
          roomB.zOrigin + roomB.length <= roomA.zOrigin)) {
      return true;
    }

    // Check if roomB is in front of roomA
    if (roomA.zOrigin + roomA.length == roomB.zOrigin &&
        !(roomA.xOrigin + roomA.width <= roomB.xOrigin ||
          roomB.xOrigin + roomB.width <= roomA.xOrigin)) {
      return true;
    }

    // Check if roomB is behind roomA
    if (roomB.zOrigin + roomB.length == roomA.zOrigin &&
        !(roomA.xOrigin + roomA.width <= roomB.xOrigin ||
          roomB.xOrigin + roomB.width <= roomA.xOrigin)) {
      return true;
    }

    return false; // Not adjacent
  }

 void RenderDungeon() {
  Dictionary<RoomSize, RoomVariantData> roomVariants = 
      roomVariantsWrapper.ToDictionary();

  for (int i = 0; i < rooms.Count; i++) {
    Room room = rooms[i];
    bool isStartingRoom = (i == 0);
    
    if (isStartingRoom && startingRoomPrefab != null) {
      Vector3 roomCenter = new Vector3(
        room.xOrigin + 0.5f,
        0,
        room.zOrigin + 0.5f
      );
      
      GameObject instance = Instantiate(startingRoomPrefab, roomCenter, Quaternion.identity, transform);
      instance.name = "StartingRoom";
    } else {
      // For non-starting rooms, use the normal variants
      RoomSize size = new RoomSize {
        width = room.width,
        length = room.length
      };
      
      if (roomVariants.ContainsKey(size)) {
        RoomVariantData variantData = roomVariants[size];
        
        if (variantData.normalVariants != null && variantData.normalVariants.Count > 0) {
          GameObject prefab = variantData.normalVariants[Random.Range(0, variantData.normalVariants.Count)];
          
          Vector3 roomCenter = new Vector3(
            room.xOrigin + (room.width / 2f),
            0,
            room.zOrigin + (room.length / 2f)
          );
          
          Instantiate(prefab, roomCenter, Quaternion.identity, transform);
        } else {
          Debug.LogWarning($"No room variants found for size: Width={size.width}, Length={size.length}");
        }
      } else {
        Debug.LogWarning($"No room variants defined for size: Width={room.width}, Length={room.length}");
      }
    }
  }
}
}