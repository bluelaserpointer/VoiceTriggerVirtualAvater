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

    private static readonly string[] _buttonMessagesOfPrecision = {"��ƫ(5cm����)", "ƫ(3~5cm)", "һ��(2~3cm)", "׼(1~2cm)", "��׼(1cm����)" };
    private static readonly string[] _buttonMessagesOfImprove = {"�������", "û������", "��΢����", "һ�����", "���Ը���" };
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
                _messageText.text = "���������������̲�������ҳ�밴�س�����";
                break;
            case 2:
                _gazeGuide.gameObject.SetActive(true);
                _gazeGuide.transform.position = new Vector2(Screen.width / 2, Screen.height / 2);
                _messageText.text = "��������Ļ���룬������R�������󴥿����°��£�����ɺ��밴�س���������";
                break;
            case 3:
                _gazeGuide.gameObject.SetActive(false);
                _noseLookAtPosDisplay.gameObject.SetActive(true);
                _messageText.text = "����1-1����ͨ��ͷ��ת��������Ļ������������ת���۾�������ɫ����Ƿ�������������ע�ӵķ������֡�";
                GenerateButtons(_buttonMessagesOfPrecision.Length, (button, index) =>
                {
                    button.GetComponentInChildren<Text>().text = _buttonMessagesOfPrecision[index];
                    button.onClick.AddListener(() =>
                    {
                        _testResultStrings.Add("��Ȼ��λ���룺" + (_modelController.IrisDistance * 100) + "cm");
                        _testResultStrings.Add("��Ȼ��λע�ӵ���Ʒ�����" + index);
                    });
                });
                break;
            case 4:
                _messageText.text = "����1-2�����ƽʱ����Զ����Ļ��Լ10cm���ң�����ͬ�Ĳ��ԣ����֡�";
                GenerateButtons(_buttonMessagesOfPrecision.Length, (button, index) =>
                {
                    button.GetComponentInChildren<Text>().text = _buttonMessagesOfPrecision[index];
                    button.onClick.AddListener(() =>
                    {
                        _testResultStrings.Add("��Զ���룺" + (_modelController.IrisDistance * 100) + "cm");
                        _testResultStrings.Add("��Զע�ӵ���Ʒ�����" + index);
                    });
                });
                break;
            case 5:
                _messageText.text = "����1-3�����ƽʱ���뿿����Ļ��Լ10cm���ң�����ͬ�Ĳ��ԣ����֡�";
                GenerateButtons(_buttonMessagesOfPrecision.Length, (button, index) =>
                {
                    button.GetComponentInChildren<Text>().text = _buttonMessagesOfPrecision[index];
                    button.onClick.AddListener(() =>
                    {
                        _testResultStrings.Add("�������룺" + (_modelController.IrisDistance * 100) + "cm");
                        _testResultStrings.Add("����ע�ӵ���Ʒ�����" + index);
                    });
                });
                break;
            case 6:
                _messageText.text = "����1-4���������������ע����Ļ����ĺ�ɫ��ǣ�ע���ڼ䱣�ְ�ס�ո��3�롣";
                _noseLookAtPosDisplay.gameObject.SetActive(false);
                _gazeGuide.gameObject.SetActive(true);
                _timeProgress.gameObject.SetActive(true);
                _timeProgress.maxTime = 3;
                _timeProgress.maxAccumulateAction = () =>
                {
                    _testResultStrings.Add("��Ļ����-1��" + (_modelController.IrisDistance * 100) + "cm");
                    _testResultStrings.Add("��Ļ����ƽ����ɢ������-1��" + _deltaSum / _deltaSampleCount * (0.0254F / Screen.dpi) + "m");
                    _timeProgress.Reset();
                    NextPhase();
                };
                TestGazeAvgDelta();
                break;
            case 7:
                _messageText.text = "����1-5�����ظ���ͬ�Ĳ��ԣ������������ע����Ļ����ĺ�ɫ��ǣ�ע���ڼ䱣�ְ�ס�ո��3�롣";
                _timeProgress.maxAccumulateAction = () =>
                {
                    _testResultStrings.Add("��Ļ����-2��" + (_modelController.IrisDistance * 100) + "cm");
                    _testResultStrings.Add("��Ļ����ƽ����ɢ������-2��" + _deltaSum / _deltaSampleCount * (0.0254F / Screen.dpi) + "m");
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
                _ifAngleFixAppliedText.text = "�޸ĺ�";
                _messageText.text = "����2-1������һЩ�罻ͷ��������ҡͷ����ͷ����ͷ����������ͷ���������а�Tab�л����Ƕ�����ǰ��Ч��������Ϊ�Ƕ�������Ч�����罻���󿴣����Ƴ̶���Σ�";
                GenerateButtons(_buttonMessagesOfImprove.Length, (button, index) =>
                {
                    button.GetComponentInChildren<Text>().text = _buttonMessagesOfImprove[index];
                    button.onClick.AddListener(() =>
                    {
                        _testResultStrings.Add("��Ļ����-1��" + (_modelController.IrisDistance * 100) + "cm");
                        _testResultStrings.Add("�����Ƕȸ���Ч�����������ģ�ͣ���" + _buttonMessagesOfImprove[index]);
                    });
                });
                _updateAction = () =>
                {
                    if (Input.GetKeyDown(KeyCode.Tab))
                    {
                        _ifAngleFixAppliedText.text = (_modelController.enableNeckAngleFix = !_modelController.enableNeckAngleFix) ? "�޸ĺ�" : "�޸�ǰ";
                    }
                };
                break;
            case 9:
                _messageText.text = "����2-2���ظ�һ�β��ԣ����ٴ���һЩ�罻ͷ��������ҡͷ����ͷ����ͷ����������ͷ���������а�Tab�л����Ƕ�����ǰ��Ч��������Ϊ�Ƕ�������Ч�����罻���󿴣����Ƴ̶���Σ�";
                _modelController.SetAvater(_nextModel);
                GenerateButtons(_buttonMessagesOfImprove.Length, (button, index) =>
                {
                    button.GetComponentInChildren<Text>().text = _buttonMessagesOfImprove[index];
                    button.onClick.AddListener(() =>
                    {
                        _testResultStrings.Add("��Ļ����-2��" + (_modelController.IrisDistance * 100) + "cm");
                        _testResultStrings.Add("�����Ƕȸ���Ч����������ģ�ͣ���" + _buttonMessagesOfImprove[index]);
                    });
                });
                _updateAction = () =>
                {
                    if (Input.GetKeyDown(KeyCode.Tab))
                    {
                        _ifAngleFixAppliedText.text = (_modelController.enableNeckAngleFix = !_modelController.enableNeckAngleFix) ? "�޸ĺ�" : "�޸�ǰ";
                    }
                };
                break;
            default:
                _modelController.enableNeckAngleFix = true;
                _messageText.text = "���Խ�������л���룡���Խ���ı�������ڱ���������·�����뷢�͸��ı������ߡ��ɰ����ϼ��˳�����";
                System.IO.File.WriteAllLines("���Խ��-" + System.DateTime.Now.GetDateTimeFormats('r')[0].Replace(':', '-') + ".txt", _testResultStrings);
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
