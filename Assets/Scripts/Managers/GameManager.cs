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


    // 싱글톤 등록 + 프레임 고정 (Unity 메시지)
    private void Awake()
    {
        Inst = this;

        // 모바일 제출 기준 60프레임 고정 (vSync가 켜져 있으면 targetFrameRate가 무시되므로 끈다)
        QualitySettings.vSyncCount  = 0;
        Application.targetFrameRate = 60;
    }

    // 시작 UI 세팅 (Unity 메시지)
    private void Start()
    {
        UISetup();
    }

    // 에디터에서만 치트 입력 처리 (Unity 메시지)
    private void Update()
    {
#if UNITY_EDITOR
        InputCheatKey();
#endif
    }

    #region 초기화

    // 게임 시작 — 턴 매니저의 시작 코루틴 가동 (TitlePanel.StartGameClick이 호출)
    public void StartGame()
    {
        StartCoroutine(TurnManager.Inst.StartGameCo());
    }

    // 시작 UI 초기 세팅
    private void UISetup()
    {
        _notificationPanel.ScaleZero();     // 알림 패널 숨김
        _resultPanel.ScaleZero();           // 결과 패널 숨김
        _titlePanel.Active(true);           // 타이틀 패널 켜기
        _cameraEffect.SetGrayScale(false);  // 흑백 효과 끄기
    }

    #endregion

    #region 게임 진행

    // 화면 중앙에 안내 메시지를 띄운다
    public void Notification(string message)
    {
        _notificationPanel.Show(message);
    }

    // 전투·보스 피격 후 승패 확인을 요청한다 (CombatSystem·치트가 호출)
    public void CheckBattleResult()
    {
        StartCoroutine(CheckBattleResultCo());
    }

    // 연출 여유를 둔 뒤 진영 전멸 여부로 승/패를 판정한다 (앞줄+뒷줄 카드가 모두 제거되면 패배)
    private IEnumerator CheckBattleResultCo()
    {
        yield return _resultCheckDelay;

        if (EntityManager.Inst.IsMyAllDead)         // 내 카드 전멸 → 패배
        {
            StartCoroutine(GameOver(false));
        }
        else if (EntityManager.Inst.IsOtherAllDead) // 상대 카드 전멸 → 승리
        {
            StartCoroutine(GameOver(true));
        }
    }

    #endregion

    #region 종료 / 결과

    // 입력을 봉인하고 결과 패널 + 흑백 연출을 띄운다
    public IEnumerator GameOver(bool isMyWin)
    {
        TurnManager.Inst.isLoading = true;
        _endTurnBtn.SetActive(false);
        
        yield return _gameOverDelay;

        TurnManager.Inst.isLoading = true;
        _resultPanel.Show(isMyWin ? "승리" : "패배");
        _cameraEffect.SetGrayScale(true);
    }

    #endregion

    #region 개발 / 치트

    // 에디터 개발 편의 치트키
    private void InputCheatKey()
    {
        // 1. 내 앞줄 첫 카드 즉사 (승격·패배 판정 테스트)
        if (CheatKeyDown(KeyCode.Alpha1, KeyCode.Keypad1))
        {
            KillFrontFirst(true);
        }

        // 2. 상대 앞줄 첫 카드 즉사 (승격·승리 판정 테스트)
        if (CheatKeyDown(KeyCode.Alpha2, KeyCode.Keypad2))
        {
            KillFrontFirst(false);
        }

        // 3. 턴 종료
        if (CheatKeyDown(KeyCode.Alpha3, KeyCode.Keypad3))
        {
            TurnManager.Inst.EndTurn();
        }

        // 4. 내 마나 최대치(10) 충전
        if (CheatKeyDown(KeyCode.Alpha4, KeyCode.Keypad4))
        {
            ManaManager.Inst.FillMana(true);
        }

        // 5. 세팅 단계 — 내 손패 전부 자동 배치
        if (CheatKeyDown(KeyCode.Alpha5, KeyCode.Keypad5))
        {
            CardManager.Inst.CheatAutoPlaceMyCards();
        }
    }

    // 해당 진영 앞줄의 첫 카드를 즉사시키고 제거·승격·승패 판정을 진행한다 (치트용)
    private void KillFrontFirst(bool isMine)
    {
        var frontList = isMine ? EntityManager.Inst.MyFront : EntityManager.Inst.OtherFront;
        if (frontList.Count == 0)
        {
            return;
        }

        var target = frontList[0];
        target.Damaged(9999);
        EntityManager.Inst.RemoveDeadAndRealign(target);
        CheckBattleResult();
    }

    // 상단 숫자열(Alpha)과 넘패드(Keypad) 둘 다 인식한다
    private static bool CheatKeyDown(KeyCode alpha, KeyCode keypad)
    {
        return Input.GetKeyDown(alpha) || Input.GetKeyDown(keypad);
    }

    #endregion
}
