using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using DATN.Legacy;

/// <summary>
/// Quản lý 1 enemy instance. Refactored:
/// - Thêm HP system (scale theo wave)
/// - TakeDamage() API cho skill behaviors mới
/// - Cache tất cả GetComponent/Find calls
/// - Gộp 4 OnTrigger blocks trùng lặp thành 1
/// - Giữ backward-compatible với tag cũ (Bolt, ball, Fire, Spiner)
/// </summary>
public class EnemyManager : MonoBehaviour
{
    [Header("Enemy Data (optional - nếu có SO)")]
    [SerializeField] private EnemyData enemyData;

    [Header("Manager Enemy")]
    public GameObject Manager;
    public GameObject HitEffect;
    public GameObject BloodLocalisation;
    public GameObject UImanager;
    public GameObject Bolt; // Damage text popup

    [Header("Diamond Drops")]
    public GameObject BlueDiamond;
    public GameObject RedDiamond;
    public GameObject GreenDiamond;

    [Header("HP Settings (dùng nếu không có EnemyData SO)")]
    [SerializeField] private float defaultHP = 100f;

    // ---- Cached References ----
    private AudioSource audioSource;
    private BooleanManager boolM;
    private GameManager gameManager;
    private UIManager uiManager;
    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private TextMeshProUGUI damageText;

    // ---- Runtime State ----
    private float maxHP;
    private float currentHP;
    private int diamondType;
    private bool followPlayer = true;
    private bool isDead;

    // ---- Public Properties ----
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public bool IsDead => isDead;

    // ============================================================
    // Lifecycle
    // ============================================================

    void Start()
    {
        // Cache references (1 lần duy nhất, không gọi lại trong Update)
        Manager = GameObject.Find("GameManager");
        // After NinjaUI migration, the legacy UIManager GameObject was renamed/reparented.
        // Find by component type instead of relying on the old root name "UI".
        var legacyUI = UnityEngine.Object.FindFirstObjectByType<DATN.Legacy.UIManager>();
        UImanager = legacyUI != null ? legacyUI.gameObject : null;
        BloodLocalisation = GameObject.Find("BloodManager");
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (Bolt != null)
            damageText = Bolt.GetComponent<TextMeshProUGUI>();

        if (Manager != null)
            gameManager = Manager.GetComponent<GameManager>();

        if (UImanager != null)
            uiManager = UImanager.GetComponent<UIManager>();

        var controller = GameObject.Find("Controller");
        if (controller != null)
            boolM = controller.GetComponent<BooleanManager>();

        // Khởi tạo HP
        InitializeHP();

        diamondType = Random.Range(1, 4); // 1-3
    }

    void Update()
    {
        if (isDead) return;

        // Follow player
        if (followPlayer && playerTransform != null && gameManager != null)
        {
            float speed = enemyData != null
                ? enemyData.GetMoveSpeed(GetCurrentWave())
                : gameManager.SpeedEnemy;

            transform.position = Vector2.MoveTowards(
                transform.position, playerTransform.position, speed * Time.deltaTime);

            if (spriteRenderer != null)
                spriteRenderer.flipX = playerTransform.position.x < transform.position.x;
        }

        // Check destroy signal
        if (uiManager != null && uiManager.DestroyEnemys)
        {
            Destroy(gameObject);
        }
    }

    // ============================================================
    // HP System (MỚI)
    // ============================================================

    private void InitializeHP()
    {
        int wave = GetCurrentWave();

        if (enemyData != null)
        {
            maxHP = enemyData.GetHP(wave);
        }
        else
        {
            // Fallback: dùng defaultHP + scale đơn giản
            maxHP = defaultHP * (1f + 0.15f * (wave - 1));
        }

        currentHP = maxHP;
    }

    /// <summary>
    /// Gây damage cho enemy. Gọi bởi skill behaviors mới.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHP -= damage;

        // Hiển thị damage text
        ShowDamageText(damage);

        // Play hit SFX
        if (boolM != null && boolM.Sound && audioSource != null)
            audioSource.Play();

        // Spawn hit effect
        if (HitEffect != null && BloodLocalisation != null)
        {
            var fx = Instantiate(HitEffect, transform.position, transform.rotation);
            fx.transform.SetParent(BloodLocalisation.transform);
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Play death animation
        if (animator != null)
            animator.Play("ZombieDeath");

        // Update kill count
        if (gameManager != null)
            gameManager.CurrentKilled += 1;

        // Drop diamond + destroy sau animation
        StartCoroutine(DeathSequence());
    }

    private void ShowDamageText(float damage)
    {
        if (Bolt == null || damageText == null) return;

        Bolt.SetActive(true);
        damageText.color = Color.red;
        damageText.text = Mathf.RoundToInt(damage).ToString();
    }

    // ============================================================
    // Death & Drops
    // ============================================================

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(0.5f);

        // Drop diamond — XP comes only when player walks over the diamond (Survivor.io
        // style). Auto-grant on death used to live here but bypassed the pickup mechanic;
        // see Diamond.OnTriggerEnter2D for the actual XP grant path.
        DropDiamond();

        Destroy(gameObject);
    }

    private void DropDiamond()
    {
        if (BloodLocalisation == null) return;

        GameObject diamondPrefab = diamondType switch
        {
            1 => BlueDiamond,
            2 => RedDiamond,
            3 => GreenDiamond,
            _ => BlueDiamond
        };

        if (diamondPrefab != null)
        {
            var diamond = Instantiate(diamondPrefab, transform.position, transform.rotation);
            diamond.transform.SetParent(BloodLocalisation.transform);
        }
    }

    // ============================================================
    // Collision (backward compatible với tags cũ)
    // ============================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        // Gộp 4 blocks trùng lặp cũ (Bolt, ball, Fire, Spiner) thành 1
        if (other.CompareTag("Bolt") || other.CompareTag("ball") ||
            other.CompareTag("Fire") || other.CompareTag("Spiner"))
        {
            // Dùng damage cố định cho hệ thống cũ (backward compatible)
            float dmg = maxHP; // 1-hit kill giống cũ cho đến khi full migrate
            TakeDamage(dmg);
        }

        if (other.CompareTag("Player"))
        {
            if (boolM != null && boolM.Sound && audioSource != null)
                audioSource.Play();
            followPlayer = false;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            followPlayer = true;
        }
    }

    // ============================================================
    // Helpers
    // ============================================================

    private int GetCurrentWave()
    {
        if (gameManager != null)
            return Mathf.Max(1, gameManager.CurrentReload + 1);
        return 1;
    }
}
