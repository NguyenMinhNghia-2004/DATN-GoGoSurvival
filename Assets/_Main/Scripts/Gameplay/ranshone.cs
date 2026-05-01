using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ranshone : MonoBehaviour
{
    public GameObject SpawenPoint;
    public GameObject Fire;
    public GameObject Audio;
    private BooleanManager BoolM;
    internal Vector3 Vec;
    private void Start()
    {
        SpawenPoint = GameObject.Find("PointFollowF");
        Audio = GameObject.Find("Ransha");
        Vec = SpawenPoint.transform.position;
        GameObject controller = GameObject.Find("Controller");
        if (controller != null)
            BoolM = controller.GetComponent<BooleanManager>();
    }

    void Update()
    {
        transform.Rotate(0, 0, +3f);
        transform.position = Vector2.MoveTowards(this.transform.position, Vec, 3 * Time.deltaTime);
        if(transform.position == Vec)
        {
            Instantiate(Fire, transform.position, Fire.transform.rotation);
            if (BoolM != null && BoolM.Sound) Audio.GetComponent<AudioSource>().Play();
            Destroy(this.gameObject);
        }
    }
}
