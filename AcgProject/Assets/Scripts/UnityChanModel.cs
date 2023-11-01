using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityChanModel : TrackModel
{
    [SerializeField]
    SkinnedMeshRenderer ref_SMR_EYE_DEF;
    [SerializeField]
    SkinnedMeshRenderer ref_SMR_EL_DEF;
    [SerializeField]
    SkinnedMeshRenderer ref_SMR_MTH_DEF;

    public override void SetEyeOpenRatio(float ratio)
    {
        ref_SMR_EYE_DEF.SetBlendShapeWeight(6, ratio);
        ref_SMR_EL_DEF.SetBlendShapeWeight(6, ratio);
    }

    public override void SetMouthOpenRatio(float ratio)
    {
        ref_SMR_MTH_DEF.SetBlendShapeWeight(6, ratio);
    }
}
