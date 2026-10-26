using UnityEngine;

/// <summary>
/// A gold coin on the ground. Spins, flies to the player when they get close,
/// and adds itself to their Wallet on contact.
/// </summary>
public class GoldPickup : MonoBehaviour
{
    [SerializeField] private int amount = 1;
    [SerializeField] private float pickupRadius = 0.8f;
    [Tooltip("Coin starts flying toward the player inside this distance.")]
    [SerializeField] private float magnetRadius = 3f;
    [SerializeField] private float magnetSpeed = 12f;
    [SerializeField] private float spinSpeed = 180f;

    private Wallet wallet;
    private bool magnetized;

    public int Amount => amount;

    public void SetAmount(int value) => amount = Mathf.Max(1, value);

    private void Start()
    {
        wallet = FindFirstObjectByType<Wallet>();
    }

    private void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        if (wallet == null) return;

        Vector3 target = wallet.transform.position;
        float distance = Vector3.Distance(transform.position, target);

        if (distance <= magnetRadius) magnetized = true;
        if (magnetized)
            transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * Time.deltaTime);

        if (distance <= pickupRadius)
        {
            wallet.Add(amount);
            Destroy(gameObject);
        }
    }
}
