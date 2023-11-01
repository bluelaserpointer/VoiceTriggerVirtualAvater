using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JohnBasicBlendShaper : BlendShapeApplier
{
    [SerializeField]
    SkinnedMeshRenderer ref_2S_Body;
    public override void SetEyeOpenRatio(float ratio)
    {
        ref_2S_Body.SetBlendShapeWeight(16, ratio);
    }

    public override void SetMouthOpenRatio(float ratio)
    {
        ref_2S_Body.SetBlendShapeWeight(67, ratio);
    }
}
