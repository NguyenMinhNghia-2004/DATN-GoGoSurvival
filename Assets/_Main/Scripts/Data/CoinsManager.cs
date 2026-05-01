using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinsManager : MonoBehaviour
{
    public GameObject Player;
    public GameObject Manager;
    public GameObject UIManager;
    private AudioSource Effect;
    private BooleanManager BoolM;
    internal bool MoveOn = false;
    public bool WorkOn = true;
    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        Manager = GameObject.Find("GameManager");
        UIManager = GameObject.Find("UI");
        Effect = GetComponent<AudioSource>();
        Effect.volume = 0.5f;
        GameObject controller = GameObject.Find("Controller");
        if (controller != null)
            BoolM = controller.GetComponent<BooleanManager>();
    }
    void Update()
    {
        if (MoveOn == true)
        {
            transform.position = Vector2.MoveTowards(this.transform.position, Player.transform.position, 10f * Time.deltaTime);
        }
    }

    IEnumerator FollowPlayer()
    {
        if (WorkOn == true)
        {
            yield return new WaitForSeconds(0.0f);
            MoveOn = true;
            yield return new WaitForSeconds(1f);
            Destroy(this.gameObject);
        }
    }
    IEnumerator FlashingLed()
    {
        if (BoolM != null && BoolM.Sound) Effect.Play();
        yield return new WaitForEndOfFrame();
        Manager.GetComponent<GameManager>().FillingFlash.SetActive(true);
        yield return new WaitForSeconds(0.005f);
        Manager.GetComponent<GameManager>().FillingFlash.SetActive(false);
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.9f);
        Destroy(this.gameObject);

    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(FollowPlayer());
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCoins(1);
                UIManager.GetComponent<ManagerMecanique>().CoinsInt = CurrencyManager.Instance.Coins;
            }
            else
            {
                UIManager.GetComponent<ManagerMecanique>().CoinsInt += 1;
                DataManager.Instance.SetCoins(UIManager.GetComponent<ManagerMecanique>().CoinsInt);
            }
            Manager.GetComponent<GameManager>().CurrentCurrency += 1;
            StartCoroutine(FlashingLed());
        }
    }
}
