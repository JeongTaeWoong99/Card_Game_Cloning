using System.Collections;
using UnityEngine;

// 치트, UI, 게임오버 관리
public class GameManager : MonoBehaviour
{
    public static GameManager Inst { get; private set; }

    [Multiline(10)]
    [SerializeField] private string           _cheatInfo;
    [SerializeField] private NotificationPanel _notificationPanel;
    [SerializeField] private ResultPanel       _resultPanel;
    [SerializeField] private TitlePanel        _titlePanel;
    [SerializeField] private CameraEffect      _cameraEffect;
    [SerializeField] private GameObject        _endTurnBtn;

    private readonly WaitForSeconds _gameOverDelay = new WaitForSeconds(2f);


    private void Awake()
    {
        Inst = this;
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

    public IEnumerator GameOver(bool isMyWin)
    {
        TurnManager.Inst.isLoading = true;
        _endTurnBtn.SetActive(false);
        yield return _gameOverDelay;

        TurnManager.Inst.isLoading = true;
        _resultPanel.Show(isMyWin ? "승리" : "패배");
        _cameraEffect.SetGrayScale(true);
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
