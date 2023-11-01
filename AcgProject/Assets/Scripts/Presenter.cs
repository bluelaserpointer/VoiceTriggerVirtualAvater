using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Presenter : MonoBehaviour
{
    [SerializeField]
    ModelController _modelController;
    [SerializeField]
    Text _ifAngleFixAppliedText;
    [SerializeField]
    TrackModel _model1;
    [SerializeField]
    TrackModel _model2;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            _ifAngleFixAppliedText.text = (_modelController.enableNeckAngleFix = !_modelController.enableNeckAngleFix) ? "修改后" : "修改前";
        }
        if (Input.GetKey(KeyCode.Alpha1))
        {
            _modelController.SetAvater(_model1);
        }
        if (Input.GetKey(KeyCode.Alpha2))
        {
            _modelController.SetAvater(_model2);
        }
    }
}
