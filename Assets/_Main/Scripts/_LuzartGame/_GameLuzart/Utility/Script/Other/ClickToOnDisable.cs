using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ClickToOnDisable : MonoBehaviour
{
    public Action actionClickBox;
    private void OnEnable()
    {
        StartWaitToUpdate();
    }
    private void OnDisable()
    {
        if (corIEWaitToUpdate != null)
        {
            StopCoroutine(corIEWaitToUpdate);
        }
    }
    private void StartWaitToUpdate()
    {
        if (corIEWaitToUpdate != null)
        {
            StopCoroutine(corIEWaitToUpdate);
        }
        corIEWaitToUpdate = StartCoroutine(IEWaitToUpdate());
    }
    private Coroutine corIEWaitToUpdate = null;
    private IEnumerator IEWaitToUpdate()
    {
        yield return new WaitForSeconds(0.2f);
        while (gameObject.activeInHierarchy)
        {
            if (gameObject.activeSelf)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    gameObject.SetActive(false);
                    actionClickBox?.Invoke();
                }
            }
            yield return null;
        }
    }
}
