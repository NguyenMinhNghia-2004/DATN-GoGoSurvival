namespace Luzart
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    #if DOTWEEN
    using DG.Tweening;
    #endif
    using TMPro;
    public class RewardSliderXValue : UnityEngine.MonoBehaviour
    {
        public Button btStopPointer;
        public Action<float> action;
        public RectTransform rtPointer;
        public RectTransform rtLine;
        public float timeMove = 1.2f;
    #if DOTWEEN
        private Sequence twLoop = null;
    #endif
        public TMP_Text[] tx = new TMP_Text[5];
        public float[] valueX = new float[5];
        public GameObject[] obShadow; 
        private float widthLine;
        private float posLineX;
        private float widthPointer;
        private float startValueX;
        private float endValueX;
        private float widthEach;
        private int curCoins;
        private float anchorPos;
        private GameObject obPreShadow = null;
        public TMP_Text coinRewardTxt;
        public void Start()
        {
            GameUtil.ButtonOnClick(btStopPointer, OnClickButton, false);
        }
        public void Initialize(int coinCur, int xValue, Action<float> actionClick)
        {
            this.action = actionClick;
            curCoins = coinCur;
            for (int i = 0; i < tx.Length; i++)
            {
                valueX[i] = valueX[i] * xValue;
                tx[i].text = $"x{valueX[i]}";
            }
            SetSlider();
        }
        private void SetSlider()
        {
            widthLine = rtLine.rect.width;
            widthEach = widthLine / valueX.Length;
            widthPointer = rtPointer.rect.width;
            posLineX = rtLine.anchoredPosition.x;
            anchorPos = posLineX - widthLine / 2;
            startValueX = posLineX - widthLine / 2;
            endValueX = posLineX + widthLine / 2;
            LoopMove();
        }
        private void LoopMove()
        {
    #if DOTWEEN
            twLoop = DOTween.Sequence();
            Vector2 pos = rtPointer.anchoredPosition;
            twLoop.AppendCallback(()=> rtPointer.anchoredPosition = new Vector2(startValueX + 30f, pos.y));
            twLoop.Append(rtPointer.DOAnchorPosX(endValueX - 30f, timeMove, false).SetEase(Ease.InOutQuad));
            twLoop.Append(rtPointer.DOAnchorPosX(startValueX + 30f, timeMove, false).SetEase(Ease.InOutQuad));
            twLoop.SetEase(Ease.Linear);
            twLoop.OnUpdate(UpdateInMove);
            twLoop.SetLoops(-1);
    #endif
        }
        private void OnDestroy()
        {
    #if DOTWEEN
            twLoop?.Kill();
    #endif
        }
        public string strPreReward = null;
        private void UpdateInMove()
        {
            float pointerPos = rtPointer.anchoredPosition.x;
            for (int i = 0; i < valueX.Length; i++)
            {
                if (pointerPos <= anchorPos + widthEach * (i + 1))
                {
                    SetActiveObLighting(obShadow[i]);
                    coinRewardTxt.text = $"{strPreReward}{valueX[i]}";
                    break;
                }
            }
        }
        private void SetActiveObLighting(GameObject go)
        {
            if (!go.activeInHierarchy)
            {
                return;
            }
            if (obPreShadow != null)
            {
                obPreShadow.SetActive(true);
            }
            obPreShadow = go;
            obPreShadow.SetActive(false);
        }
        public void OnClickButton()
        {
    #if DOTWEEN
            twLoop?.Pause();
    #endif
            float value = OnClickStopPointer();
            action?.Invoke(value);
        }
        public float OnClickStopPointer()
        {
            float pointerPos = rtPointer.anchoredPosition.x;
            for (int i = 0; i < valueX.Length; i++)
            {
                if (pointerPos <= anchorPos + widthEach * (i + 1))
                {
                    return valueX[i];
                }
            }
            return valueX[valueX.Length - 1];
        }
        public int GetCoins()
        {
            return curCoins;
        }
        private int Calculate(float a)
        {
            return (int)(curCoins * a);
        }
    }
}
