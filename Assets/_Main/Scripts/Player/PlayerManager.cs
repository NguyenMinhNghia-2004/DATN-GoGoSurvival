using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class PlayerManager : MonoBehaviour
{
    [Header("Manager Controller")]
    public GameManager Manager;
    public BooleanManager Bool;


    [Header("Spawen Transformer")]
    public AudioSource EffectArrow;
    public AudioSource AudioHeat;
    public GameObject Death;
    public GameObject SpawenShoot;
    public GameObject ContainerWap;
    public Vector3 Offest;
    private Vector3 rb;

    [Header("Containers")]
    public GameObject Bolts;
    public GameObject UI;

    [Header("Boolean manager")]
    internal bool Deaths = true;


    void Start()
    {
        
    }
    void Update()
    {
        if(Bool.GameStart == true)
        {
            UI.transform.position = Vector3.SmoothDamp(transform.position, transform.position + Offest, ref rb, 0);
            SpawenShoot.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            HitBolts();
        }
        
        // Death detection: HealthBar was legacy UI Image (deleted). Use Manager.Health
        // directly — same source of truth, GameManager.Update sets PlayerDeath flag.
        if (Death != null) Death.SetActive(Manager != null && Manager.Health <= 0f);
    }
    void HitBolts()
    {
        if (Bool.GameStart == true)
        {
            if (Manager.AvailabelWeapon == true)
            {
                if (Manager.SpawnObject == true)
                {
                    (Instantiate(Bolts, SpawenShoot.transform.position, Quaternion.identity) as GameObject).transform.SetParent(ContainerWap.transform);
                    if (Bool.Sound) EffectArrow.Play();
                }
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Bool.GameStart != true) return;
        if (!other.CompareTag("Enemy")) return;

        // F.G.1: when LuzartPlayerController owns damage (flag ON), don't double-charge.
        // LPC writes Stats.Runtime_HP. Writing legacy Manager.Health here would (a) double
        // the actual damage and (b) trigger GameManager.Update's PlayerDeath freeze
        // (simulated=false), which makes the player un-controllable mid-life.
        var srm = Luzart.SceneRootManager.Instance;
        var flags = srm != null ? srm.Domain?.Get<Luzart.Migration.MigrationFlags>() : null;
        if (flags != null && flags.UseLuzartPlayerController) return;

        if (Bool.Sound) AudioHeat.Play();
        Manager.Health -= 0.5f;
    }

}
