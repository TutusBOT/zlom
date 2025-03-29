using System.Collections.Generic;
using Alteruna;
using Alteruna.Trinity;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : AttributesSync
{
    [SynchronizableMethod]
    public void StartGame()
    {
        Multiplayer.LoadScene("Dungeon3D", true);
    }

    void OnMouseDown()
    {
        StartGame();
    }
}
