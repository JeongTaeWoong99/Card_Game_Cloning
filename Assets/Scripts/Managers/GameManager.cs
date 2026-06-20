using System.Collections;
using UnityEngine;

// 게임 흐름의 최상위: 프레임 고정, UI 패널 제어, 승패 판정, 게임오버 연출, 치트.
public class GameManager : MonoBehaviour
{
    public static GameManager Inst { get; private set; }

    [CenterHeader("< 치트 >")]
    [Multiline(10)]
    [SerializeField] private string _cheatInfo;

    [CenterHeader("< UI 패널 >")]
    [SerializeField] private NotificationPanel _notificationPanel;
    [SerializeField] private ResultPanel       _resultPanel;
    [SerializeField] private TitlePanel        _titlePanel;

    [CenterHeader("< 연출 / 버튼 >")]
    [SerializeField] private CameraEffect _cameraEffect;
    [SerializeField] private GameObject   _endTurnBtn;

    private readonly WaitForSeconds _gameOverDelay    = new WaitForSeconds(2f);
    private readonly WaitForSeconds _resultCheckDelay = new WaitForSeconds(2f);


    private void Awake()
    {
        Inst = this;

        // 모바일 제출 기준 60프레임 고정 (vSync가 켜져 있으면 targetFrameRate가 무시되므로 끈다)
        QualitySettings.vSyncCount  = 0;
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        UISetup();
    }

    private void Update()
    {
#if UNITY_EDITOR
        InputCheatKey();
#endif
    }

    public void StartGame()
    {
        StartCoroutine(TurnManager.Inst.StartGameCo());
    }

    public void Notification(string message)
    {
        _notificationPanel.Show(message);
    }

    // 전투 결과를 확인할 시점(공격·보스 피격 후)에 호출한다. 연출 여유를 두고 판정한다
    public void CheckBattleResult()
    {
        StartCoroutine(CheckBattleResultCo());
    }

    public IEnumerator GameOver(bool isMyWin)
    {
        TurnManager.Inst.isLoading = true;
        _endTurnBtn.SetActive(false);
        yield return _gameOverDelay;

        TurnManager.Inst.isLoading = true;
        _resultPanel.Show(isMyWin ? "승리" : "패배");
        _cameraEffect.SetGrayScale(true);
    }

    private IEnumerator CheckBattleResultCo()
    {
        yield return _resultCheckDelay;

        if (EntityManager.Inst.MyBoss.isDie)
        {
            StartCoroutine(GameOver(false));
        }

        if (EntityManager.Inst.OtherBoss.isDie)
        {
            StartCoroutine(GameOver(true));
        }
    }

    private void UISetup()
    {
        _notificationPanel.ScaleZero();
        _resultPanel.ScaleZero();
        _titlePanel.Active(true);
        _cameraEffect.SetGrayScale(false);
    }

    private void InputCheatKey()
    {
        if (CheatKeyDown(KeyCode.Alpha1, KeyCode.Keypad1))
        {
            TurnManager.OnAddCard?.Invoke(true);
        }

        if (CheatKeyDown(KeyCode.Alpha2, KeyCode.Keypad2))
        {
            TurnManager.OnAddCard?.Invoke(false);
        }

        if (CheatKeyDown(KeyCode.Alpha3, KeyCode.Keypad3))
        {
            TurnManager.Inst.EndTurn();
        }

        if (CheatKeyDown(KeyCode.Alpha4, KeyCode.Keypad4))
        {
            CardManager.Inst.TryPutCard(false);
        }

        if (CheatKeyDown(KeyCode.Alpha5, KeyCode.Keypad5))
        {
            EntityManager.Inst.DamageBoss(true, 19);
        }

        if (CheatKeyDown(KeyCode.Alpha6, KeyCode.Keypad6))
        {
            EntityManager.Inst.DamageBoss(false, 19);
        }
    }

    // 상단 숫자열(Alpha)과 넘패드(Keypad) 둘 다 인식한다
    private static bool CheatKeyDown(KeyCode alpha, KeyCode keypad)
    {
        return Input.GetKeyDown(alpha) || Input.GetKeyDown(keypad);
    }
}
