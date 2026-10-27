using System;
using UnityEngine;

/// <summary>
/// Holds the player's money (the score). Lives on the player ship.
/// <see cref="MoneyBarrel"/> calls <see cref="Add"/> when the ship picks a barrel up.
/// </summary>
public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    public int Money { get; private set; }
    public int BarrelsCollected { get; private set; }

    /// <summary>Raised after every pickup with (amount, world position of the barrel).</summary>
    public event Action<int, Vector3> Collected;

    void OnEnable() => Instance = this;

    void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    public void Add(int amount, Vector3 from)
    {
        Money += amount;
        BarrelsCollected++;
        Collected?.Invoke(amount, from);
    }
}
