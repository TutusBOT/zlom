using System;
using UnityEngine;

public interface IPickable
{
    void OnPickedUp(Player player);
    void OnDropped();
    GameObject GetGameObject();
    bool CanBePickedUp();
    public static event Action<GameObject> OnItemDestroyed;
    public static void RaiseItemDestroyed(GameObject item)
    {
        OnItemDestroyed?.Invoke(item);
    }
}
