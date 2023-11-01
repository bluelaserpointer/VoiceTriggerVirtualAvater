using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public abstract class BlendShapeApplier : MonoBehaviour
{
    public abstract void SetEyeOpenRatio(float ratio);
    public abstract void SetMouthOpenRatio(float ratio);
}
