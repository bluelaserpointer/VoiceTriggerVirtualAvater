using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TimeProgress : MonoBehaviour
{
    [SerializeField]
    Image _fillImage;

    public float maxTime;
    public bool IsRunning { get; private set; }
    public float AccumulatedTime { get; private set; }
    public float AccumulationRatio => AccumulatedTime / maxTime;
    public System.Action maxAccumulateAction;
    public void SetMaxTime(float maxTime)
    {
        this.maxTime = maxTime;
    }
    public void Run()
    {
        IsRunning = true;
    }
    public void Pause()
    {
        IsRunning = false;
    }
    public void Reset()
    {
        AccumulatedTime = 0;
    }
    void Update()
    {
        if(IsRunning)
        {
            AccumulatedTime += Time.deltaTime;
            if (AccumulationRatio >= 1)
            {
                maxAccumulateAction?.Invoke();
                IsRunning = false;
            }
        }
        _fillImage.fillAmount = AccumulationRatio;
    }
}
