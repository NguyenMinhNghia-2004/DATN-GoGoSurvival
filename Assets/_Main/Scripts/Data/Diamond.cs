using System.Collections;
using UnityEngine;

/// <summary>
/// XP gem (Biofuel) pickup. Phase B8 migrated: XP grant goes ONLY through framework
/// <see cref="Luzart.PlayerCharacter.Stats"/> (the source of truth for XP). The legacy
/// GameManager.ValureLevel bar is no longer driven from here; SV_GameplayHud's XP bar
/// subscribes to Stats.Runtime_XP directly.
///
/// Behavior:
///   - Spawned by enemy death (see EnemyManager.DropDiamond) and by BoltSHooter scatter.
///   - When player's DetecteurRoad trigger enters this gem, magnetize toward player
///     (BackPoint coroutine flips StartMove → false → Update lerps toward player).
///   - On collision with Player tag: grant XP + audio + destroy.
/// </summary>
public class Diamond : MonoBehaviour
{
    public GameObject Player;
    public GameObject Flasher;

    [Tooltip("XP granted when player walks over this gem. Match GDD Small Biofuel = 10.")]
    public float xpReward = 10f;

    private AudioSource _audio;
    private BooleanManager _boolM;
    internal bool FollowPlayer = false;
    internal bool StartMove = true;
    internal bool AddOnce = true;

    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        var controller = GameObject.Find("Controller");
        if (controller != null) _boolM = controller.GetComponent<BooleanManager>();
        _audio = GetComponent<AudioSource>();
        if (_audio != null) _audio.volume = 0.5f;
    }

    private bool IsGameRunning()
    {
        return _boolM != null && _boolM.GameStart;
    }

    void FixedUpdate()
    {
        if (!IsGameRunning() || !FollowPlayer || !StartMove) return;
        if (AddOnce)
        {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            AddOnce = false;
        }
        gameObject.GetComponent<Rigidbody2D>().AddForce(transform.up * 15);
        StartCoroutine(BackPoint());
    }

    void Update()
    {
        if (!IsGameRunning() || StartMove || Player == null) return;
        transform.position = Vector2.MoveTowards(transform.position, Player.transform.position, 15f * Time.deltaTime);
    }

    IEnumerator BackPoint()
    {
        yield return new WaitForSeconds(0.4f);
        StartMove = false;
    }

    IEnumerator Destroy()
    {
        yield return new WaitForSeconds(0.8f);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Framework Stats.AddXP is the single source of truth — fires Runtime_XP.Changed
            // → GameController.OnXPChange → SV_GameplayHud XP bar + SV_LevelUpPopup trigger.
            var sr = Luzart.SceneRootManager.Instance;
            if (sr != null && sr.Domain != null)
            {
                var player = sr.Domain.Get<Luzart.PlayerCharacter>();
                if (player != null && player.Stats != null) player.Stats.AddXP(xpReward);
            }

            if (Flasher != null) Instantiate(Flasher, transform.position, transform.rotation);
            if (_boolM != null && _boolM.Sound && _audio != null) _audio.Play();
            StartCoroutine(Destroy());
        }
        else if (other.CompareTag("RoadDetections"))
        {
            FollowPlayer = true;
        }
    }
}
