using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

// 게임 흐름의 최상위: 프레임 고정, UI 패널 제어, 승패 판정, 게임오버 연출, 치트.
public class GameManager : MonoService<IGameFlow>, IGameFlow
{
    [CenterHeader("< 치트 >")]
    [Multiline(10)]
    [SerializeField] private string _cheatInfo;

    [CenterHeader("< UI 패널 >")]
    [SerializeField] private NotificationPanel _notificationPanel;
    [SerializeField] private ResultPanel       _resultPanel;
    [SerializeField] private TitlePanel        _titlePanel;
    [SerializeField] private FadePanel         _fadePanel;

    [CenterHeader("< 연출 / 버튼 >")]
    [SerializeField] private CameraEffect _cameraEffect;

    [CenterHeader("< 게임플레이 HUD >")] // 타이틀·게임오버 동안 숨기고 세팅 시작 시 켜는 묶음
    [SerializeField] private GameObject   _endTurnBtn;  // ActionBtn
    [SerializeField] private GameObject   _myManaBar;
    [SerializeField] private GameObject   _otherManaBar;
    [SerializeField] private GameObject[] _inGameMenuButtons; // 게임 중 종료·다시·메인메뉴 (HUD와 함께 토글)

    private const string BlurIntensityId = "_BlurIntensity"; // 전체화면 블러 RenderFeature 글로벌 강도
    private const float  BlurTarget      = 1f;               // 게임오버 시 블러 목표 강도
    private const float  BlurInTime      = 0.6f;             // 블러가 차오르는 시간

    private readonly WaitForSeconds _gameOverDelay    = new WaitForSeconds(2f);
    private readonly WaitForSeconds _resultCheckDelay = new WaitForSeconds(2f);

    private bool        _isGameOver; // 결과 처리 1회 보장 (Show·연출 중첩 방지)
    private static bool _autoStart;  // 씬 재로드 후 자동 게임 시작 여부 (다시하기)


    // 서비스 등록(베이스) + 프레임 고정 (Unity 메시지)
    protected override void Awake()
    {
        base.Awake();

        // 모바일 제출 기준 60프레임 고정 (vSync가 켜져 있으면 targetFrameRate가 무시되므로 끈다)
        QualitySettings.vSyncCount  = 0;
        Application.targetFrameRate = 60;
    }

    // 시작 UI 세팅 + 다시하기면 곧바로 게임 시작 (Unity 메시지)
    private void Start()
    {
        UISetup();

        if (_autoStart) // 다시하기로 재로드된 경우 타이틀을 스킵하고 바로 세팅을 시작
        {
            _autoStart = false;
            _titlePanel.SetDissolved(); // 타이틀 배경(월드 디졸브)을 즉시 제거 — 타이틀 스킵 시 배경 잔존 방지
            _titlePanel.Active(false);
            StartGame();
        }

        _fadePanel.FadeIn(); // 검정에서 서서히 밝아지는 씬 진입 연출
    }

    // 에디터에서만 치트 입력 처리 (Unity 메시지)
    private void Update()
    {
#if UNITY_EDITOR
        InputCheatKey();
#endif
    }

    #region 초기화

    // 게임 시작 — 게임플레이 HUD를 켜고 턴 매니저의 시작 코루틴 가동 (TitlePanel.StartGameClick이 호출)
    public void StartGame()
    {
        SetGameplayHudActive(true); // 디졸브 후 세팅 로직이 시작되는 시점에 HUD 노출
        StartCoroutine(Services.Get<ITurnManager>().StartGameCo());
    }

    // 시작 UI 초기 세팅
    private void UISetup()
    {
        _notificationPanel.ScaleZero();             // 알림 패널 숨김
        _resultPanel.Hide();                        // 결과 패널 끔(스케일 0)
        _titlePanel.Active(true);                   // 타이틀 패널 켜기
        _cameraEffect.SetGrayScale(false);          // 흑백 효과 끄기
        Shader.SetGlobalFloat(BlurIntensityId, 0f); // 평소 전체화면 블러 끔(보드 선명)
        SetGameplayHudActive(false);                // 타이틀 동안 턴종료 버튼·마나바 숨김
    }

    // 게임플레이 HUD(턴종료 버튼 + 양쪽 마나바)를 일괄 토글한다
    private void SetGameplayHudActive(bool isActive)
    {
        _endTurnBtn.SetActive(isActive);
        _myManaBar.SetActive(isActive);
        _otherManaBar.SetActive(isActive);

        // 게임 중 종료·다시·메인메뉴 버튼도 액션버튼·마나바와 동일하게 함께 토글한다
        foreach (GameObject button in _inGameMenuButtons)
        {
            if (button != null)
            {
                button.SetActive(isActive);
            }
        }
    }

    #endregion

    #region 공용 액션

    // 여러 화면(타이틀·결과)이 직접 호출하는 상태 없는 액션. 버튼 OnClick → 메서드에 직접 연결.

    // 게임 종료 (타이틀·게임 중·결과 종료 버튼 OnClick에 할당)
    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // 다시하기 — 씬을 재로드한 뒤 타이틀을 건너뛰고 곧바로 새 게임을 시작한다 (버튼 OnClick에 할당)
    public void Restart()
    {
        _autoStart = true;
        SceneManager.LoadScene(0);
    }

    // 메인메뉴 — 검정으로 페이드아웃한 뒤 씬을 재로드해 타이틀로 돌아간다 (버튼 OnClick에 할당)
    public void ToMainMenu()
    {
        _autoStart = false;
        _fadePanel.FadeOut(() => SceneManager.LoadScene(0));
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

        if (Services.Get<IBoardState>().IsMyAllDead)         // 내 카드 전멸 → 패배
        {
            StartCoroutine(GameOver(false));
        }
        else if (Services.Get<IBoardState>().IsOtherAllDead) // 상대 카드 전멸 → 승리
        {
            StartCoroutine(GameOver(true));
        }
    }

    #endregion

    #region 종료 / 결과

    // 입력을 봉인하고 결과 패널 + 흑백 연출을 띄운다
    public IEnumerator GameOver(bool isMyWin)
    {
        if (_isGameOver) // 여러 번 호출돼도 결과 패널 연출이 중첩되지 않게 1회만 처리
        {
            yield break;
        }
        _isGameOver = true;

        Services.Get<ITurnManager>().isLoading = true;
        SetGameplayHudActive(false); // 턴종료 버튼 + 양쪽 마나바 숨김

        yield return _gameOverDelay;

        // 결과 패널 뒤 게임 보드를 서서히 흐리게 (전체화면 블러 RenderFeature 강도 램프)
        DOVirtual.Float(0f, BlurTarget, BlurInTime, value => Shader.SetGlobalFloat(BlurIntensityId, value));

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
            Services.Get<ITurnManager>().EndTurn();
        }

        // 4. 내 마나 최대치(10) 충전
        if (CheatKeyDown(KeyCode.Alpha4, KeyCode.Keypad4))
        {
            Services.Get<IManaManager>().FillMana(true);
        }

        // 5. 세팅 단계 — 내 손패 전부 자동 배치
        if (CheatKeyDown(KeyCode.Alpha5, KeyCode.Keypad5))
        {
            Services.Get<ICardManager>().CheatAutoPlaceMyCards();
        }
    }

    // 해당 진영 앞줄의 첫 카드를 즉사시키고 제거·승격·승패 판정을 진행한다 (치트용)
    private void KillFrontFirst(bool isMine)
    {
        var frontList = isMine ? Services.Get<IBoardState>().MyFront : Services.Get<IBoardState>().OtherFront;
        if (frontList.Count == 0)
        {
            return;
        }

        var target = frontList[0];
        target.Damaged(9999);
        Services.Get<IBoardState>().RemoveDeadAndRealign(target);
        CheckBattleResult();
    }

    // 상단 숫자열(Alpha)과 넘패드(Keypad) 둘 다 인식한다
    private static bool CheatKeyDown(KeyCode alpha, KeyCode keypad)
    {
        return Input.GetKeyDown(alpha) || Input.GetKeyDown(keypad);
    }

    #endregion
}
