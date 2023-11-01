using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class TrainData
{
    StringBuilder sb_train = new StringBuilder();
    StringBuilder sb_test = new StringBuilder();
    
    public int AppendedFrameCount { get; private set; }

    public void Init()
    {
        sb_train.Clear();
        sb_test.Clear();
        AppendedFrameCount = 0;
    }
    public void AppendTrainAndTest(Vector3[] positions, Vector2 screenPointUV)
    {
        foreach (Vector3 position in positions)
        {
            sb_train.Append(position.x.ToString("G6")).Append(',')
                .Append(position.y.ToString("G6")).Append(',')
                .Append(position.z.ToString("G6")).Append(',');
        }
        sb_test.Append(screenPointUV.x.ToString("G6")).Append(',')
            .Append(screenPointUV.y.ToString("G6")).Append(',');
        ++AppendedFrameCount;
    }
    public string GetTrain()
    {
        if (sb_train.Length == 0)
            return "";
        return sb_train.ToString().Substring(0, sb_train.Length - 2); //remove lastest comma
    }
    public string GetTest()
    {
        if (sb_test.Length == 0)
            return "";
        return sb_test.ToString().Substring(0, sb_test.Length - 2); //remove lastest comma
    }
}
