using System;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public static event Action OnSellKeyPressed;
    public KeyCode sellKey = KeyCode.E;

    private void Update()
    {
        if (Input.GetKeyDown(sellKey))
        {
            OnSellKeyPressed?.Invoke();
        }
    }
}
