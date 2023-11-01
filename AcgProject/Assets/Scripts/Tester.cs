using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tester : MonoBehaviour
{
    public int phase;
    [SerializeField]
    ModelController _modelController;
    [SerializeField]
    Transform _modelRoot;
    [SerializeField]
    TimeProgress _timeProgress;

    [SerializeField]
    Image _noseLookAtPosDisplay;
    [SerializeField]
    Text _messageText;
    [SerializeField]
    Image _progressFill;
    [SerializeField]
    GameObject _scoreButtonGroup;
    [SerializeField]
    Button _scoreButtonPrefab;

    [SerializeField]
    Image _gazeGuide;
    [SerializeField]
    GameObject _ifAngleFixAppliedNotice;
    [SerializeField]
    Text _ifAngleFixAppliedText;
    [SerializeField]
    TrackModel _nextModel;


    List<string> _testResultStrings = new List<string>();

    float _deltaSum;
    float _deltaSampleCount;

    private static readonly string[] _buttonMessagesOfPrecision = {"很偏(5cm以上)", "偏(3~5cm)", "一般(2~3cm)", "准(1~2cm)", "很准(1cm以内)" };
    private static readonly string[] _buttonMessagesOfImprove = {"反而变差", "没有区别", "轻微改善", "一般改善", "明显改善" };
    System.Action _updateAction;

    private void Start()
    {
        NextPhase();
    }
    public void NextPhase()
    {
        _updateAction = null;
        switch (++phase)
        {
            case 1:
                _noseLookAtPosDisplay.gameObject.SetActive(false);
                _modelRoot.gameObject.SetActive(false);
                _messageText.text = "本测试需鼠标与键盘操作。翻页请按回车键。";
                break;
            case 2:
                _gazeGuide.gameObject.SetActive(true);
                _gazeGuide.transform.position = new Vector2(Screen.width / 2, Screen.height / 2);
                _messageText.text = "请正视屏幕中央，并按下R键（如误触可重新按下）。完成后，请按回车键继续。";
                break;
            case 3:
                _gazeGuide.gameObject.SetActive(false);
                _noseLookAtPosDisplay.gameObject.SetActive(true);
                _messageText.text = "测试1-1：请通过头部转动看向屏幕各处（而不是转动眼睛）。绿色标记是否正好是你现在注视的方向？请打分。";
                GenerateButtons(_buttonMessagesOfPrecision.Length, (button, index) =>
                {
                    button.GetComponentInChildren<Text>().text = _buttonMessagesOfPrecision[index];
                    button.onClick.AddListener(() =>
                    {
                        _testResultStrings.Add("自然座位距离：" + (_modelController.IrisDistance * 100) + "cm");
                        _testResultStrings.Add("自然座位注视点估计分数：" + index);
                    });
                });
                break;
            case 4:
                _messageText.text = "测试1-2：请比平时距离远离屏幕大约10cm左右，做相同的测试，请打分。";
                GenerateButtons(_buttonMessagesOfPrecision.Length, (button, index) =>
                {
                    button.GetComponentInChildren<Text>().text = _buttonMessagesOfPrecision[index];
                    button.onClick.AddListener(() =>
                    {
                        _testResultStrings.Add("离远距离：" + (_modelController.IrisDistance * 100) + "cm");
                        _testResultStrings.Add("离远注视点估计分数：" + index);
                    });
                });
                break;
            case 5:
                _messageText.text = "测试1-3：请比平时距离靠近屏幕大约10cm左右，做相同的测试，请打分。";
                GenerateButtons(_buttonMessagesOfPrecision.Length, (button, index) =>
                {
                    button.GetComponentInChildren<Text>().text = _buttonMessagesOfPrecision[index];
                    button.onClick.AddListener(() =>
                    {
                        _testResultStrings.Add("靠近距离：" + (_modelController.IrisDistance * 100) + "cm");
                        _testResultStrings.Add("靠近注视点估计分数：" + index);
                    });
                });
                break;
            case 6:
                _messageText.text = "测试1-4：请在任意距离下注视屏幕中央的红色标记，注视期间保持按住空格键3秒。";
                _noseLookAtPosDisplay.gameObject.SetActive(false);
                _gazeGuide.gameObject.SetActive(true);
                _timeProgress.gameObject.SetActive(true);
                _timeProgress.maxTime = 3;
                _timeProgress.maxAccumulateAction = () =>
                {
                    _testResultStrings.Add("屏幕距离-1：" + (_modelController.IrisDistance * 100) + "cm");
                    _testResultStrings.Add("屏幕中央平均分散像素量-1：" + _deltaSum / _deltaSampleCount * (0.0254F / Screen.dpi) + "m");
                    _timeProgress.Reset();
                    NextPhase();
                };
                TestGazeAvgDelta();
                break;
            case 7:
                _messageText.text = "测试1-5：请重复相同的测试，在任意距离下注视屏幕中央的红色标记，注视期间保持按住空格键3秒。";
                _timeProgress.maxAccumulateAction = () =>
                {
                    _testResultStrings.Add("屏幕距离-2：" + (_modelController.IrisDistance * 100) + "cm");
                    _testResultStrings.Add("屏幕中央平均分散像素量-2：" + _deltaSum / _deltaSampleCount * (0.0254F / Screen.dpi) + "m");
                    _timeProgress.Reset();
                    NextPhase();
                };
                TestGazeAvgDelta();
                break;
            case 8:
                _gazeGuide.gameObject.SetActive(false);
                _timeProgress.gameObject.SetActive(false);
                _ifAngleFixAppliedNotice.SetActive(true);
                _modelRoot.gameObject.SetActive(true);
                _ifAngleFixAppliedText.text = "修改后";
                _messageText.text = "测试2-1：请做一些社交头部动作（摇头、点头、低头、后仰、歪头），过程中按Tab切换至角度修正前的效果。你认为角度修正的效果从社交需求看，改善程度如何？";
                GenerateButtons(_buttonMessagesOfImprove.Length, (button, index) =>
                {
                    button.GetComponentInChildren<Text>().text = _buttonMessagesOfImprove[index];
                    button.onClick.AddListener(() =>
                    {
                        _testResultStrings.Add("屏幕距离-1：" + (_modelController.IrisDistance * 100) + "cm");
                        _testResultStrings.Add("颈部角度改善效果（动漫风格模型）：" + _buttonMessagesOfImprove[index]);
                    });
                });
                _updateAction = () =>
                {
                    if (Input.GetKeyDown(KeyCode.Tab))
                    {
                        _ifAngleFixAppliedText.text = (_modelController.enableNeckAngleFix = !_modelController.enableNeckAngleFix) ? "修改后" : "修改前";
                    }
                };
                break;
            case 9:
                _messageText.text = "测试2-2：重复一次测试，请再次做一些社交头部动作（摇头、点头、低头、后仰、歪头），过程中按Tab切换至角度修正前的效果。你认为角度修正的效果从社交需求看，改善程度如何？";
                _modelController.SetAvater(_nextModel);
                GenerateButtons(_buttonMessagesOfImprove.Length, (button, index) =>
                {
                    button.GetComponentInChildren<Text>().text = _buttonMessagesOfImprove[index];
                    button.onClick.AddListener(() =>
                    {
                        _testResultStrings.Add("屏幕距离-2：" + (_modelController.IrisDistance * 100) + "cm");
                        _testResultStrings.Add("颈部角度改善效果（拟真风格模型）：" + _buttonMessagesOfImprove[index]);
                    });
                });
                _updateAction = () =>
                {
                    if (Input.GetKeyDown(KeyCode.Tab))
                    {
                        _ifAngleFixAppliedText.text = (_modelController.enableNeckAngleFix = !_modelController.enableNeckAngleFix) ? "修改后" : "修改前";
                    }
                };
                break;
            default:
                _modelController.enableNeckAngleFix = true;
                _messageText.text = "测试结束，感谢参与！测试结果文本已输出在本程序所在路径，请发送该文本至作者。可按右上键退出程序。";
                System.IO.File.WriteAllLines("测试结果-" + System.DateTime.Now.GetDateTimeFormats('r')[0].Replace(':', '-') + ".txt", _testResultStrings);
                break;
        }
    }
    private void TestGazeAvgDelta()
    {
        _gazeGuide.transform.position = new Vector2(Random.value * Screen.width, Random.value * Screen.height);
        _updateAction = () =>
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _timeProgress.Run();
                _deltaSampleCount = 0;
                _deltaSum = 0;
            }
            else if (Input.GetKeyUp(KeyCode.Space))
            {
                _timeProgress.Pause();
                _timeProgress.Reset();
            }
            if (Input.GetKey(KeyCode.Space))
            {
                _deltaSampleCount++;
                _deltaSum += Vector2.Distance(_modelController.NoseLookAtScreenPos, _gazeGuide.transform.position);
            }
        };
    }
    private List<Button> GenerateButtons(int amount, System.Action<Button, int> eachButtonInitAction)
    {
        List<Button> buttons = new List<Button>();
        for(int index = 0; index < amount; index++)
        {
            Button button = Instantiate(_scoreButtonPrefab, _scoreButtonGroup.transform);
            eachButtonInitAction.Invoke(button, index);
            button.onClick.AddListener(() =>
            {
                foreach (Transform tf in _scoreButtonGroup.transform)
                {
                    Destroy(tf.gameObject);
                }
                NextPhase();
            });
            buttons.Add(button);
        }
        return buttons;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            NextPhase();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            _modelController.SetScreenCentreZero();
        }
        _updateAction?.Invoke();
    }
    public void ExitApplication()
    {
        Application.Quit();
    }
}
