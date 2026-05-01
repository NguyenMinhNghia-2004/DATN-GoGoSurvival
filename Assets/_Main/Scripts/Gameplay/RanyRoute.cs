using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RanyRoute : MonoBehaviour
{
    public GameObject SpawenPoint;
    public GameObject FireContainer;
    public GameObject Effect;
    public GameObject Fire;
    private BooleanManager BoolM;
    internal Vector3 Vec;
    private void Start()
    {
        SpawenPoint = GameObject.Find("PointFollowF");
        FireContainer = GameObject.Find("FireContainer");
        Effect = GameObject.Find("GaseBloomb");
        Vec = SpawenPoint.transform.position;
        GameObject controller = GameObject.Find("Controller");
        if (controller != null)
            BoolM = controller.GetComponent<BooleanManager>();
    }

    void Update()
    {
        transform.Rotate(0, 0, +3f);
        transform.position = Vector2.MoveTowards(this.transform.position, Vec, 3 * Time.deltaTime);
        if (transform.position == Vec)
        {
            (Instantiate(Fire, transform.position , Fire.transform.rotation) as GameObject).transform.SetParent(FireContainer.transform);
            if (BoolM != null && BoolM.Sound) Effect.GetComponent<AudioSource>().Play();
            Destroy(this.gameObject);
        }
    }
}
