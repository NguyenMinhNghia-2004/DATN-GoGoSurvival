using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MyGame.Scripts.CheatManagement
{
    public class CheatPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject lockObj;
        [SerializeField] private TMP_InputField inputPassword;

        [Header("Cheat UI")]
        [SerializeField] private TMP_Dropdown cheatOptsDropdown;
        [SerializeField] private TMP_Dropdown[] cheatDropdowns;
        [SerializeField] private TMP_InputField[] inputValues;

        [Header("FPS")]
        [SerializeField] private GameObject fpsLog;

        private GameManager gameManager;
        private UIManager uiManager;
        private ManagerMecanique mecanique;

        public static bool IsOpenCheat()
        {
            return PlayerPrefs.GetInt("SONAT_CHEATED", 0) == 1 || Application.isEditor;
        }

        private void Start()
        {
            gameManager = FindAnyObjectByType<GameManager>();
            uiManager = FindAnyObjectByType<UIManager>();
            mecanique = FindAnyObjectByType<ManagerMecanique>();

            InitOpts();

#if UNITY_EDITOR
            lockObj.SetActive(false);
#else
            if (PlayerPrefs.HasKey("SONAT_CHEATED"))
                lockObj.SetActive(false);
            else
                lockObj.SetActive(true);
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            if (Input.GetKeyDown(KeyCode.F1))
                OnOffCheat();
#endif
        }

        public void Unlock()
        {
            if (inputPassword.text.Trim().Equals("Sonat@111"))
            {
                lockObj.SetActive(false);
                PlayerPrefs.SetInt("SONAT_CHEATED", 1);
            }
        }

        public void CancelUnlock()
        {
            if (panel != null && panel.activeInHierarchy)
                panel.SetActive(false);
        }

        public void OnOffCheat()
        {
            bool willOpen = !panel.activeInHierarchy;
            panel.SetActive(willOpen);

            if (willOpen)
            {
                if (gameManager == null)
                    gameManager = FindAnyObjectByType<GameManager>();
                if (uiManager == null)
                    uiManager = FindAnyObjectByType<UIManager>();
                if (mecanique == null)
                    mecanique = FindAnyObjectByType<ManagerMecanique>();

                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = 1f;
            }
        }

        private CheatOption GetCheatOpt()
        {
            string text = cheatOptsDropdown.options[cheatOptsDropdown.value].text;
            return (CheatOption)Enum.Parse(typeof(CheatOption), text);
        }

        private void OnCheatOptChange(int value)
        {
            SetDropdownCount(0);
            SetInputFieldCount(0);

            CheatOption opt = GetCheatOpt();
            switch (opt)
            {
                case CheatOption.SetCoins:
                case CheatOption.SetGems:
                case CheatOption.SetFlash:
                case CheatOption.SetHealth:
                case CheatOption.SetScore:
                case CheatOption.SpeedEnemy:
                    SetInputFieldCount(1);
                    break;
                case CheatOption.PlayerPrefs:
                    SetInputFieldCount(2);
                    break;
                default:
                    break;
            }
        }

        public void InitOpts()
        {
            List<TMP_Dropdown.OptionData> opts = new List<TMP_Dropdown.OptionData>();
            for (CheatOption i = 0; i < CheatOption.MAX; i++)
            {
                if (!Enum.IsDefined(typeof(CheatOption), i)) continue;
                opts.Add(new TMP_Dropdown.OptionData(i.ToString()));
            }

            cheatOptsDropdown.ClearOptions();
            cheatOptsDropdown.options = opts;
            cheatOptsDropdown.onValueChanged.RemoveAllListeners();
            cheatOptsDropdown.onValueChanged.AddListener(OnCheatOptChange);
            OnCheatOptChange(0);
        }

        public void OnCheatClick()
        {
            CheatOption opt = GetCheatOpt();
            switch (opt)
            {
                case CheatOption.SetCoins:
                    if (int.TryParse(inputValues[0].text, out int coins))
                        CheatSetCoins(coins);
                    break;

                case CheatOption.SetGems:
                    if (int.TryParse(inputValues[0].text, out int gems))
                        CheatSetGems(gems);
                    break;

                case CheatOption.SetFlash:
                    if (int.TryParse(inputValues[0].text, out int flash))
                        CheatSetFlash(flash);
                    break;

                case CheatOption.SetHealth:
                    if (float.TryParse(inputValues[0].text, out float hp))
                        CheatSetHealth(hp);
                    break;

                case CheatOption.SetScore:
                    if (int.TryParse(inputValues[0].text, out int score))
                        CheatSetScore(score);
                    break;

                case CheatOption.SpeedEnemy:
                    if (float.TryParse(inputValues[0].text, out float speed))
                        CheatSpeedEnemy(speed);
                    break;

                case CheatOption.KillAllEnemies:
                    CheatKillAllEnemies();
                    break;

                case CheatOption.FullHealth:
                    CheatFullHealth();
                    break;

                case CheatOption.GodMode:
                    CheatGodMode();
                    break;

                case CheatOption.ResetData:
                    CheatResetData();
                    break;

                case CheatOption.PlayerPrefs:
                    if (inputValues.Length >= 2)
                        CheatPlayerPrefs(inputValues[0].text, inputValues[1].text);
                    break;
            }
        }

        #region Cheat Logic

        private void CheatSetCoins(int value)
        {
            DataManager.Instance.SetCoins(value);
            if (mecanique != null)
                mecanique.CoinsInt = value;
            ShowToast($"Coins = {value}");
        }

        private void CheatSetGems(int value)
        {
            DataManager.Instance.SetGems(value);
            if (mecanique != null)
                mecanique.GemsInt = value;
            ShowToast($"Gems = {value}");
        }

        private void CheatSetFlash(int value)
        {
            DataManager.Instance.SetFlash(value);
            if (mecanique != null)
                mecanique.FlashInt = value;
            ShowToast($"Flash = {value}");
        }

        private void CheatSetHealth(float value)
        {
            if (gameManager != null)
            {
                gameManager.Health = Mathf.Clamp(value, 0f, 100f);
                ShowToast($"Health = {gameManager.Health}");
            }
        }

        private void CheatSetScore(int value)
        {
            DataManager.Instance.SetCurrentScore(value);
            if (mecanique != null)
                mecanique.LevelInt = value;
            ShowToast($"Score = {value}");
        }

        private void CheatSpeedEnemy(float speed)
        {
            if (gameManager != null)
            {
                gameManager.SpeedEnemy = speed;
                ShowToast($"SpeedEnemy = {speed}");
            }
        }

        private void CheatKillAllEnemies()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            int count = enemies.Length;
            foreach (var e in enemies)
                Destroy(e);
            ShowToast($"Killed {count} enemies");
        }

        private void CheatFullHealth()
        {
            if (gameManager != null)
            {
                gameManager.Health = 100f;
                gameManager.HealthBar.color = Color.green;
                ShowToast("Health = 100");
            }
        }

        private static bool godModeActive = false;
        private void CheatGodMode()
        {
            godModeActive = !godModeActive;
            if (godModeActive && gameManager != null)
            {
                gameManager.Health = 100f;
                gameManager.HealthBar.color = Color.cyan;
            }
            ShowToast($"GodMode = {(godModeActive ? "ON" : "OFF")}");
        }

        private void LateUpdate()
        {
            if (godModeActive && gameManager != null && gameManager.Boolean != null && gameManager.Boolean.GameStart)
            {
                gameManager.Health = 100f;
            }
        }

        private void CheatResetData()
        {
            DataManager.Instance.SetCoins(0);
            DataManager.Instance.SetGems(0);
            DataManager.Instance.SetFlash(0);
            DataManager.Instance.SetCurrentScore(1);
            DataManager.Instance.SetScore(0f);
            DataManager.Instance.SetCurrentKilled(0);
            DataManager.Instance.SetCurrentCurrency(0);
            DataManager.Instance.SetCheckEvolve("");
            PlayerPrefs.Save();
            ShowToast("All data reset!");
        }

        private void CheatPlayerPrefs(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (value.Length < 8 && int.TryParse(value, out int intVal))
                PlayerPrefs.SetInt(key, intVal);
            else if (float.TryParse(value, out float floatVal))
                PlayerPrefs.SetFloat(key, floatVal);
            else
                PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
            ShowToast($"PlayerPrefs[{key}] = {value}");
        }

        #endregion

        #region FPS

        public void ToggleFPSLog()
        {
            if (fpsLog != null)
                fpsLog.SetActive(!fpsLog.activeSelf);
        }

        public void ShowFPSLog()
        {
            if (fpsLog != null) fpsLog.SetActive(true);
        }

        public void HideFPSLog()
        {
            if (fpsLog != null) fpsLog.SetActive(false);
        }

        #endregion

        #region Helpers

        private void SetDropdownCount(int count)
        {
            if (cheatDropdowns == null) return;
            for (int i = 0; i < cheatDropdowns.Length; i++)
                cheatDropdowns[i].gameObject.SetActive(i < count);
        }

        private void SetInputFieldCount(int count)
        {
            if (inputValues == null) return;
            for (int i = 0; i < inputValues.Length; i++)
            {
                inputValues[i].gameObject.SetActive(i < count);
                inputValues[i].text = "";
            }
        }

        private void ShowToast(string msg)
        {
            Debug.Log($"[Cheat] {msg}");
        }

        #endregion
    }

    public enum CheatOption
    {
        SetCoins,
        SetGems,
        SetFlash,
        SetHealth,
        SetScore,
        SpeedEnemy,
        KillAllEnemies,
        FullHealth,
        GodMode,
        ResetData,
        PlayerPrefs,
        MAX,
    }
}