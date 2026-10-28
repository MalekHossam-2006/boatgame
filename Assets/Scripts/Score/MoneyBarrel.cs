using UnityEngine;

/// <summary>
/// A floating barrel with money inside. When the player ship sails close enough,
/// the money goes to the <see cref="PlayerWallet"/> and the barrel disappears.
/// Floating on the waves is handled by the <see cref="Bobber"/> on the same object.
/// </summary>
public class MoneyBarrel : MonoBehaviour
{
    public int value = 10;
    [Tooltip("Horizontal distance from the ship's center that counts as a pickup.")]
    public float pickupRadius = 3.2f;

    /// <summary>Barrels still floating in the scene.</summary>
    public static int Remaining { get; private set; }

    void OnEnable() => Remaining++;
    void OnDisable() => Remaining--;

    void Update()
    {
        var wallet = PlayerWallet.Instance;
        if (wallet == null || !GameState.IsPlaying) return;

        var d = wallet.transform.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > pickupRadius * pickupRadius) return;

        wallet.Add(value, transform.position);
        gameObject.SetActive(false);
    }
}
