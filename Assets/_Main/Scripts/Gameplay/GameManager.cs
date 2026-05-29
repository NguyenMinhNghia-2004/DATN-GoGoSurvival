using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System.Threading;
using Unity.VisualScripting;
using static UnityEngine.GraphicsBuffer;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using System.Numerics;
using DATN.Legacy;

public class GameManager : MonoBehaviour
{
    [Header("Manager Controller")]
    public BooleanManager Boolean;
    public PlayerManager PlayerPerfb;
    public AudioManager Sounds;
    public movementJoystick JoystManager;
    public SpawenManager Spawner;
    // Phase B1 migrated: legacy TimerManager removed. Game time owned by Luzart.GameController.
    public ManagerFloatingBtn ManagerDownBtn;
    public ManagerWeapons WeaponSpawn;
    public UIManager ManagerUI;

    [Header("GameObject ScreenMainMenu")]
    public GameObject ScreenHome;
    public GameObject ScreenVolve;
    public GameObject ScreenShop;
    public GameObject ScreenDeath;
    public GameObject ScreenEquipement;

    [Header("Perfabes Controller")]
    public GameObject UIWeapon;
    public GameObject FillingFlash;
    public GameObject ScreenAddonWap;
    public GameObject[] Enemys;

    [Header("Levels World")]
    public GameObject ContainerWorld;

    [Header("UI Manager")]
    public Image HealthBar;
    public Image ReloadWeapon;
    public Image ScoringLevel;
    public Image FillingReweapon;
    public Color[] myColors;

    [Header("UI Text Manager")]
    public TextMeshProUGUI ScoringValue;
    public TextMeshProUGUI ScoringValueDeux;
    public TextMeshProUGUI ValueKilled;
    public Text ValueKilledScreenFinish;
    public TextMeshProUGUI CurrentCoins;

    [Header("Float Manager")]
    public float SpeedEnemy;
    internal float Valeur;
    internal float ValureLevel;
    public float LerpTime = 0.1f;
    internal float t = 0;
    internal float Health;

    [Header("Integer Manager")]
    internal int len;
    internal int colorIndex = 0;
    internal int CurrentKilled = 0;
    internal int CurrentCurrency = 0;
    internal int CurrentReload;

    [Header("Boolean Manager")]
    internal bool EnemyAvailable = true;
    internal bool SpawnObject = true;
    internal bool RightFill = true;
    internal bool LeftFill = true;

    internal bool NormalBolt = true;
    internal bool DiamondBolt = false;
    internal bool CheckFinish = true;
    internal bool StartFlashing = false;
    internal bool startmove = false;
    internal bool AvailabelWeapon = true;
    internal bool GameStart = false;
    internal bool SetActiveAll = true;
    internal bool PlayerDeath = false;
    internal bool Checkenemys = false;

    void Start()
    {
        len = myColors.Length;
        Health = 100f;
        if (ManagerDownBtn != null)
        {
            ManagerDownBtn.Shop = true;
            ManagerDownBtn.Equipement = true;
            ManagerDownBtn.Death = true;
        }

        // Legacy HUD panels were already deleted in Phase A migration; no need to hide them.
    }
    // Legacy HUD writes (HealthBar/ScoringLevel/ScreenAddonWap/FillingReweapon) are owned
    // by SV_GameplayHudUI subscribed to framework Runtime_HP/Runtime_XP/CurrentLevel events.
    // Death detection still lives here (legacy Health field is the source of truth for HP
    // changes from EnemyManager collisions). LegacyHudUpdate path removed in Phase B cleanup.

    void Update()
    {
        if(GameStart == true && SetActiveAll == true)
        {
            Boolean.GameStart = true;
            // Phase F defensive null-guards — these Inspector refs were unwired by
            // earlier migration phases (legacy world container + weapon UI panel).
            if (ContainerWorld != null) ContainerWorld.SetActive(true);
            if (UIWeapon != null) UIWeapon.SetActive(true);
            SetActiveAll = false;
        }
        if(Boolean.GameStart == false)
        {
            Valeur = 0;
            ValureLevel = 0;
        }
        if(Boolean.GameStart == true)
        {
            CheckEnemy();
            ReloadingWapeons();

            // Death detection — Health is mutated by EnemyManager collisions. Framework
            // SV_GameplayHud reads via DATNGameplayBridge.Health → Stats.Runtime_HP.
            //
            // F.G.4: When LuzartPlayerController owns damage (flag ON), legacy Health
            // never drops (PlayerManager.OnTriggerEnter2D early-returns), but defense in
            // depth — skip the BtnPause freeze entirely. Framework owns death via
            // GameController.OnHPChange → Data_ClassicEndGame → SV_LoseScreen.
            if (Health <= 0f && PlayerDeath == false && !LuzartOwnsDeath())
            {
                BtnPause();
                PlayerDeath = true;
            }

            if (startmove == true)
            {
                foreach (GameObject joint in Enemys)
                {
                    joint.GetComponent<EnemyManager>().enabled = false;
                    joint.GetComponent<Rigidbody2D>().simulated = false;
                }
            }
            CheckKilledAndCoins();
        }
    }
    public void BtnPause()
    {
        AvailabelWeapon = false;
        /* Phase B1: Times.timerIsRunning = false → Luzart.GameController paused via Time.timeScale */
        PlayerPerfb.GetComponent<PlayerManager>().enabled = false;
        PlayerPerfb.GetComponent<Rigidbody2D>().simulated = false;
        if (EnemyAvailable == true)
        {
            Spawner.enabled = false;
            startmove = true;

        }
    }
    public void ResumeBtn()
    {
        AvailabelWeapon = true;
        Checkenemys = true;
        /* Phase B1: Times.timerIsRunning = true → Luzart.GameController resumed via Time.timeScale */
        PlayerPerfb.GetComponent<PlayerManager>().enabled = true;
        PlayerPerfb.GetComponent<Rigidbody2D>().simulated = true;
        if (EnemyAvailable == true)
        {
            startmove = false;
            Enemys = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject joint in Enemys)
            {
                joint.GetComponent<EnemyManager>().enabled = true;
                joint.GetComponent<Rigidbody2D>().simulated = true;
            }
        }
    }
    void CheckKilledAndCoins()
    {
        // Legacy text refs lived on /UI/GamePlay/TopUI and FinishScreen, both deleted in the
        // NinjaUI migration. SV_GameplayHud + SV_WinScreen now show these values. Null-guard.
        if (ValueKilled != null) ValueKilled.text = "" + CurrentKilled;
        if (ValueKilledScreenFinish != null) ValueKilledScreenFinish.text = "" + CurrentKilled;
        if (CurrentCoins != null) CurrentCoins.text = "" + CurrentCurrency;
        // Skip save when DataManager singleton isn't bootstrapped (e.g. running GamePlay
        // scene directly without going through Splash/MainMenu). Avoids per-frame NRE.
        if (DataManager.Instance != null)
        {
            DataManager.Instance.SetCurrentKilled(CurrentKilled);
            DataManager.Instance.SetCurrentCurrency(CurrentCurrency);
        }

    }
    void ReloadingWapeons()
    {
        // Legacy reload UI removed in Phase A; ReloadWeapon Image ref is null. The Valeur
        // counter still ticks for any legacy code that reads it but doesn't update visual.
        Valeur += 1f * Time.deltaTime;
        if (ReloadWeapon == null) return;
        if(ReloadWeapon.fillAmount < 1 && RightFill == true)
        {
            SpawnObject = false;
            ReloadWeapon.fillAmount = Valeur;
            LeftFill = true;
        }
        if(ReloadWeapon.fillAmount == 1 && LeftFill == true)
        {
            SpawnObject = true;
            ReloadWeapon.fillAmount = 0;
            Valeur = 0;
            LeftFill = false;
        }
    }
    void CheckEnemy()
    {
        Enemys = GameObject.FindGameObjectsWithTag("Enemy");
        if (Enemys.Length == 0)
          {
            EnemyAvailable = false;
            NormalBolt = false;
            DiamondBolt = true;
          }
        else
          {
            EnemyAvailable = true;
            NormalBolt = true;
            DiamondBolt = false;
        }
    }
    public void FirstCont()
    {
        ScreenAddonWap.SetActive(false);
        /* Phase B1: Times.timerIsRunning = true → Luzart.GameController resumed via Time.timeScale */
        PlayerPerfb.GetComponent<PlayerManager>().enabled = true;
        PlayerPerfb.GetComponent<Rigidbody2D>().simulated = true;
        if (EnemyAvailable == true)
        {
            startmove = false;
            foreach (GameObject joint in Enemys)
            {
                joint.GetComponent<EnemyManager>().enabled = true;
                joint.GetComponent<Rigidbody2D>().simulated = true;
            }
        }
        ScoringLevel.fillAmount = 0;
        StartCoroutine(CheckingFinishBtn());
    }
    public void SecondCont()
    {
        ScreenAddonWap.SetActive(false);
        /* Phase B1: Times.timerIsRunning = true → Luzart.GameController resumed via Time.timeScale */
        PlayerPerfb.GetComponent<PlayerManager>().enabled = true;
        PlayerPerfb.GetComponent<Rigidbody2D>().simulated = true;
        if (EnemyAvailable == true)
        {
            startmove = false;
            foreach (GameObject joint in Enemys)
            {
                joint.GetComponent<EnemyManager>().enabled = true;
                joint.GetComponent<Rigidbody2D>().simulated = true;
            }
        }
        ScoringLevel.fillAmount = 0;
        StartCoroutine(CheckingFinishBtn());
    }
    public void ThirdCont()
    {
        ScreenAddonWap.SetActive(false);
        /* Phase B1: Times.timerIsRunning = true → Luzart.GameController resumed via Time.timeScale */
        PlayerPerfb.GetComponent<PlayerManager>().enabled = true;
        PlayerPerfb.GetComponent<Rigidbody2D>().simulated = true;
        //Spawner.enabled = true;
        if (EnemyAvailable == true)
        {
            startmove = false;
            foreach (GameObject joint in Enemys)
            {
                joint.GetComponent<EnemyManager>().enabled = true;
                joint.GetComponent<Rigidbody2D>().simulated = true;
            }
        }
        ScoringLevel.fillAmount = 0;
        StartCoroutine(CheckingFinishBtn());
    }

    IEnumerator CheckingFinishBtn()
    {
        yield return new WaitForSeconds(0.5f);
        CheckFinish = true;
    }

    /// <summary>F.G.4 — true when Luzart owns death state. Reads MigrationFlags via SceneRootManager.
    /// Domain isn't ready until late Unity init, so this null-guards every step.</summary>
    private bool LuzartOwnsDeath()
    {
        var srm = Luzart.SceneRootManager.Instance;
        var flags = srm != null ? srm.Domain?.Get<Luzart.Migration.MigrationFlags>() : null;
        return flags != null && flags.UseLuzartPlayerController;
    }
}
