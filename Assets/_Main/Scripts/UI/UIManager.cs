using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using DATN.Legacy;

namespace DATN.Legacy {
public class UIManager : MonoBehaviour
{
    [Header("Managers")]
    public GameManager Manager;
    // Phase B1 migrated: legacy TimerManager removed. Game time is owned by Luzart.GameController._countTime.
    public BooleanManager Bool;
    // Phase D.7: LevelsManager removed. Default level prefab now resolved via Domain.Get<LevelCatalog>().
    public SpriteWeapons Weapons;
    // Phase B2 migrated: ManagerMecanique removed. CurrencyManager singleton owns Coins/Gems.

    [Header("Componenet Player")]
    public GameObject Player;
    public GameObject HelthUI;
    public GameObject LevelLocalisation;
    public GameObject CurrentLevel;

    [Header("UI Manager")]
    public GameObject ScreenPause;
    public GameObject ScreenMainMenu;
    public GameObject ScreenGamePlay;
    public GameObject EffectFade;
    public GameObject EffectFadeGamePlay;
    public GameObject FinishScreen;

    [Header("Weapons Manager")]
    public GameObject SpinerContainer;
    public GameObject GunW;
    public GameObject SpinnerAW;
    public GameObject SpinnerBW;
    

    [Header("Sprites Guns")]
    public Sprite Sahem;
    public Sprite Gun;
    public Sprite SpinerA;
    public Sprite SpinerB;
    public Sprite Ball;
    public Sprite Rocket;
    public Sprite DroneA;
    public Sprite DroneB;
    public Sprite DroneC;
    public Sprite FireGlass;
    public Sprite Brick;
    public Sprite FireGase;

    [Header("Strings Manager")]
    internal string CheckEvolve;
    internal string Checking;
    public string CurrentName;

    [Header("Boolaen Manager")]
    internal bool FinishScreenB = false;
    internal bool DestroyEnemys = false;
    internal bool StopAllAudios = false;
    internal bool _mapReady = false;
    internal bool MapReady
    {
        get => _mapReady;
        set
        {
            _mapReady = value;
            // Phase D.4 bridge: propagate to framework GameController so non-legacy readers
            // (ControllerSpawening, DiamondVip, LocalisationPresent) can drop their UIManager
            // dependency. Phase F will absorb the writer side too.
            var srm = Luzart.SceneRootManager.Instance;
            var gc = srm != null ? srm.Domain?.Get<Luzart.GameController>() : null;
            if (gc != null) gc.MapReady = value;
        }
    }

    [Header("End-game UI migration")]
    [Tooltip("If true (default), legacy FinishScreen popup is suppressed — SV_LoseScreenUI is used instead via SV_EndGameBridge listening to Data_ClassicEndGame.")]
    public bool useSurvivorIoEndGame = true;

    void Start()
    {
        Checking = DataManager.Instance.GetCheckEvolve();
    }
    void Update()
    {
        if (DataManager.Instance != null) CheckEvolve = DataManager.Instance.GetCheckEvolve();
        if (CheckEvolve == "work" && Manager != null && Manager.ManagerDownBtn != null)
        {
            Manager.ManagerDownBtn.Evolve = true;
        }
        if(Manager != null && Manager.PlayerDeath == true && FinishScreenB == false)
        {
            DestroyEnemys = true;
            StopAllAudios = true;
            if (!useSurvivorIoEndGame)
            {
                FinishScreen.SetActive(true);
            }
            // Reset death flag + restore HP regardless of which screen we show,
            // since gameplay state mutates here. The SV path will fire Data_ClassicEndGame
            // through GameController.OnHPChange before we resetto 100.
            Manager.PlayerDeath = false;
            Manager.Health = 100;
            if (Manager.HealthBar != null) Manager.HealthBar.color = Color.green;
            FinishScreenB = true;
        }
        else
        {
            StopAllAudios = false;
        }
            CurrentLevel = GameObject.Find(CurrentName);
    }
    public void BackBtn()
    {
        MapReady = false;
        EffectFadeGamePlay.SetActive(true);
        DestroyEnemys = true;
        Weapons.DesactivateAll();
        Destroy(CurrentLevel);
        StartCoroutine(StartBacking());
    }
    public void BackFinish()
    {
        MapReady = false;
        DestroyEnemys = true;
        EffectFadeGamePlay.SetActive(true);
        FinishScreen.SetActive(false);
        Weapons.DesactivateAll();
        Destroy(CurrentLevel);
        StartCoroutine(StartBacking());
    }

    /// <summary>Null-safe variant of <see cref="BackFinish"/> for the NinjaUI end-game flow
    /// where <see cref="FinishScreen"/> may be unwired (we use SV_WinScreen/SV_LoseScreen instead).
    /// Destroys the spawned Level prefab, deactivates weapons, runs the back-to-menu coroutine.</summary>
    public void BackFinishSafe()
    {
        MapReady = false;
        DestroyEnemys = true;
        if (EffectFadeGamePlay != null) EffectFadeGamePlay.SetActive(true);
        if (FinishScreen != null) FinishScreen.SetActive(false);
        if (Weapons != null) Weapons.DesactivateAll();
        if (CurrentLevel != null) Destroy(CurrentLevel);
        StartCoroutine(StartBacking());
    }
    IEnumerator StartBacking()
    {
        yield return new WaitForSeconds(0.8f);
        if (Player != null) Player.gameObject.GetComponent<Rigidbody2D>().simulated = true;
        DestroyEnemys = false;
        if (Manager != null) {
            Manager.CurrentReload = 0;
            Manager.CurrentCurrency = 0;
            Manager.CurrentKilled = 0;
        }
        // Timer reset now handled by Luzart.GameController.ResetState() (called via GameplayResetCoordinator).
        FinishScreenB = false;
        if (EffectFadeGamePlay != null) EffectFadeGamePlay.SetActive(false);
        if (Manager != null && Manager.Boolean.GameStart == true)
        {
            if (Player != null) Player.transform.position = new Vector3(0, 0, 0);
            if (HelthUI != null) HelthUI.SetActive(false);
            if (ScreenPause != null) ScreenPause.SetActive(false);
            if (ScreenGamePlay != null) ScreenGamePlay.SetActive(false);
            if (ScreenMainMenu != null) ScreenMainMenu.SetActive(true);
            Manager.Boolean.GameStart = false;
        }
    }
    public void PlayBtn()
    {
        if (EffectFade != null) EffectFade.SetActive(true);
        // Phase D.6 — instantiate via GameController.SpawnDefaultLevel (reads LevelCatalog SO).
        // Bug fix: LevelLocalisation Inspector ref is null in current scene (was always null,
        // legacy code-path also silently skipped). Removed the LevelLocalisation gate so the
        // level spawns into the scene root when no parent is provided — GameController.SpawnDefaultLevel
        // already null-handles the parent arg.
        var gc = Luzart.SceneRootManager.Instance?.Domain?.Get<Luzart.GameController>();
        GameObject spawned = null;
        if (gc != null)
        {
            var parent = LevelLocalisation != null ? LevelLocalisation.transform : null;
            spawned = gc.SpawnDefaultLevel(parent);
        }
        if (spawned != null) CurrentName = spawned.name;
        StartCoroutine(GameStart());
    }
    IEnumerator GameStart()
    {
        yield return new WaitForSeconds(0.7f);
        if(Checking == "")
        {
            Checking = "work";
        }
        if (CheckEvolve == "")
        {
            DataManager.Instance.SetCheckEvolve(Checking);
        }
        MapReady = true;
        if (Manager != null) Manager.startmove = true;
        if (EffectFade != null) EffectFade.SetActive(false);
        if (Bool != null) Bool.GameStart = true;
        // F.G fix: respect the Luzart cutover — when LuzartPlayerController owns the
        // player, LuzartBoltShooter has hard-disabled PlayerManager. Re-enabling it
        // here would resurrect HitBolts (double-fire bolts) and the stale UI/Death
        // SmoothDamp updates. Only re-enable when legacy still owns the player.
        if (Player != null)
        {
            var srm = Luzart.SceneRootManager.Instance;
            var flags = srm != null ? srm.Domain?.Get<Luzart.Migration.MigrationFlags>() : null;
            bool luzartOwnsPlayer = flags != null && flags.UseLuzartPlayerController;
            if (!luzartOwnsPlayer) Player.GetComponent<PlayerManager>().enabled = true;
        }
        if (Manager != null) { Manager.AvailabelWeapon = true; Manager.GameStart = true; }
        if (ScreenMainMenu != null) ScreenMainMenu.SetActive(false);
        if (ScreenGamePlay != null) ScreenGamePlay.SetActive(true);
        if (HelthUI != null) HelthUI.SetActive(true);
        // Timer ticks via Luzart.GameController.StartGameplay() (already called by SV_MainMenuUI.OnPlay).
        if (Manager != null && Manager.EnemyAvailable == true)
        {
            Manager.startmove = false;
            foreach (GameObject joint in Manager.Enemys)
            {
                joint.GetComponent<EnemyManager>().enabled = true;
                joint.GetComponent<Rigidbody2D>().simulated = true;
            }
        }
    }
    public void Pause()
    {
        if (!useSurvivorIoEndGame) ScreenPause.SetActive(true);
        Manager.BtnPause();
    }
    public void Resume()
    {
        if (!useSurvivorIoEndGame && ScreenPause != null) ScreenPause.SetActive(false);
        Manager.ResumeBtn();
    }
}
} // namespace DATN.Legacy
