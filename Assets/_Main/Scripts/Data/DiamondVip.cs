using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using DATN.Legacy;

public class DiamondVip : MonoBehaviour
{
    public GameObject Player;
    public GameObject Manager;
    public GameObject UIManager;
    internal bool MoveOn = false;
    public bool Green = false;
    public bool WorkOn = true;
    internal int Checking;
    private UIManager _uiMgrCache;
    void Start()
    {
        StartCoroutine(FollowPlayer());
        Player = GameObject.FindGameObjectWithTag("Player");
        Manager = GameObject.Find("GameManager");
        // Phase B2: ManagerMecanique removed. Coin/Gem state owned by CurrencyManager singleton.
        _uiMgrCache = UnityEngine.Object.FindFirstObjectByType<UIManager>();
        UIManager = _uiMgrCache != null ? _uiMgrCache.gameObject : null;
    }
    void Update()
    {
        Checking = Random.Range(0, 2);
        if (MoveOn == true && Player != null)
        {
            transform.position = Vector2.MoveTowards(this.transform.position, Player.transform.position, 10f * Time.deltaTime);
        }
        if (_uiMgrCache != null && _uiMgrCache.MapReady == false)
        {
            Destroy(this.gameObject);
        }
    }

    IEnumerator FollowPlayer()
    {
        if(WorkOn == true)
        {
            yield return new WaitForSeconds(3f);
            MoveOn = true;
            yield return new WaitForSeconds(1f);
            Destroy(this.gameObject);
        }
    }
    IEnumerator FlashingLed()
    {
        yield return new WaitForEndOfFrame();
        // FillingFlash was legacy ReloadWeapon tint Image (deleted in Phase A migration).
        // Null-guard so pickup doesn't UnassignedReferenceException.
        var gm = Manager != null ? Manager.GetComponent<GameManager>() : null;
        if (gm != null && gm.FillingFlash != null)
        {
            gm.FillingFlash.SetActive(true);
            yield return new WaitForSeconds(0.005f);
            gm.FillingFlash.SetActive(false);
            yield return new WaitForEndOfFrame();
        }
        Destroy(this.gameObject);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            if (Manager != null) Manager.GetComponent<GameManager>().ValureLevel += Checking;
            if(Green == true && CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddGems(1);
            }
            StartCoroutine(FlashingLed());
        }
    }
}
