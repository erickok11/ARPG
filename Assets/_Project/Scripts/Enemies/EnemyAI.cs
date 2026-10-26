using System.Collections;
using UnityEngine;

/// <summary>
/// Simple melee monster: waits until the player comes close (or hits it),
/// chases, attacks when in range, and gives up if the player runs far away.
/// </summary>
[RequireComponent(typeof(CharacterMover), typeof(MeleeAttack), typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    [Tooltip("Starts chasing when the player is this close.")]
    [SerializeField] private float aggroRange = 10f;
    [Tooltip("Stops chasing if the player gets this far away.")]
    [SerializeField] private float leashRange = 20f;
    [SerializeField] private float repathInterval = 0.2f;

    [Header("Death")]
    [Tooltip("Seconds the corpse takes to sink into the ground before being removed.")]
    [SerializeField] private float corpseSinkDuration = 1.5f;

    private CharacterMover mover;
    private MeleeAttack attack;
    private Health health;
    private Transform player;
    private Health playerHealth;
    private bool aggroed;
    private float repathTimer;

    private void Awake()
    {
        mover = GetComponent<CharacterMover>();
        attack = GetComponent<MeleeAttack>();
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    private void Start()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            player = pc.transform;
            playerHealth = pc.GetComponent<Health>();
        }
    }

    private void Update()
    {
        if (health.IsDead || player == null) return;

        if (playerHealth.IsDead)
        {
            if (aggroed) { aggroed = false; mover.Stop(); }
            return;
        }

        float distance = FlatDistance(player.position);
        if (!aggroed && distance <= aggroRange) aggroed = true;
        if (aggroed && distance > leashRange) { aggroed = false; mover.Stop(); }
        if (!aggroed) return;

        if (attack.InRange(player))
        {
            mover.Stop();
            mover.FaceTowards(player.position);
            attack.TryAttack(playerHealth);
            repathTimer = 0f;
        }
        else
        {
            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                mover.MoveTo(player.position);
            }
        }
    }

    private void OnDamaged(float amount) => aggroed = true;

    private void OnDied()
    {
        mover.DisableMovement();
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;
        StartCoroutine(SinkAndDestroy());
    }

    private IEnumerator SinkAndDestroy()
    {
        yield return new WaitForSeconds(0.4f);

        Vector3 start = transform.position;
        float t = 0f;
        while (t < corpseSinkDuration)
        {
            t += Time.deltaTime;
            transform.position = start + Vector3.down * (t / corpseSinkDuration) * 2f;
            yield return null;
        }
        Destroy(gameObject);
    }

    private float FlatDistance(Vector3 point)
    {
        Vector3 offset = point - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }
}
