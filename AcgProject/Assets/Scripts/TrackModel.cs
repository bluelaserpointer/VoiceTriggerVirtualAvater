using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public abstract class TrackModel : MonoBehaviour
{
    [Header("IK")]
    public UnityEvent<int> onIKPass;

    public Animator Animator { get; private set; }
    private void Awake()
    {
        Animator = GetComponent<Animator>();
    }
    public void AddIKPassAction(UnityAction<int> ikPassAction)
    {
        onIKPass.AddListener(ikPassAction);
    }
    public abstract void SetEyeOpenRatio(float ratio);
    public abstract void SetMouthOpenRatio(float ratio);
    private void OnAnimatorIK(int layerIndex)
    {
        onIKPass.Invoke(layerIndex);
    }
}
