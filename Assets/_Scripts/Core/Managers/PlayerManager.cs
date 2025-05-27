using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    private readonly List<Player> _activePlayers = new();

    [SerializeField]
    private bool _debug = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterPlayer(Player player)
    {
        if (!_activePlayers.Contains(player))
        {
            _activePlayers.Add(player);
            if (_debug)
                Debug.Log($"Player registered. Total players: {_activePlayers.Count}");
        }
    }

    public void UnregisterPlayer(Player player)
    {
        if (_activePlayers.Contains(player))
        {
            _activePlayers.Remove(player);
            if (_debug)
                Debug.Log($"Player unregistered. Total players: {_activePlayers.Count}");
        }
    }

    public List<Player> GetPlayersInRange(
        Vector3 position,
        float distance,
        Player excludeSelf = null
    )
    {
        List<Player> nearbyPlayers = new List<Player>();
        float sqrDistance = distance * distance;

        foreach (var player in _activePlayers)
        {
            if (player == excludeSelf)
                continue;

            if ((player.transform.position - position).sqrMagnitude <= sqrDistance)
            {
                nearbyPlayers.Add(player);
            }
        }

        return nearbyPlayers;
    }

    public bool IsAnyPlayerInRange(Vector3 position, float distance, Player excludeSelf = null)
    {
        float sqrDistance = distance * distance;

        foreach (var player in _activePlayers)
        {
            if (player == excludeSelf)
                continue;

            if ((player.transform.position - position).sqrMagnitude <= sqrDistance)
            {
                return true;
            }
        }

        return false;
    }

    public List<Player> GetAllPlayers()
    {
        return new List<Player>(_activePlayers);
    }

    public List<Player> GetAllAlivePlayers()
    {
        return _activePlayers.Where(player => !player.IsDead()).ToList();
    }
    public Player GetLocalPlayer()
    {
        foreach (var player in _activePlayers)
        {
            if (player.IsOwner)
            {
                return player;
            }
        }

        return null;
    }
}
