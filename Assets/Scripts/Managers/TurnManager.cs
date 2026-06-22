using System;
using System.Collections;
using UnityEngine;

// 게임 흐름을 제어한다.
// Setup(배치) 페이즈: 손패 6장 분배 → 플레이어/상대가 앞줄 3 + 뒷줄 3을 배치.
// Battle(전투) 페이즈: 배치 완료 버튼 이후 턴 시작/종료 + 스킬 드로우(현재는 디버그) 이벤트 발행.
public class TurnManager : MonoBehaviour
{
    public static TurnManager Inst { get; private set; }

    // 외부(GameManager 치트 등)에서도 Invoke 해야 하므로 event가 아닌 Action으로 둔다
    public static Action<bool> OnAddCard;

    // 내부에서만 발행하는 순수 발행-구독 이벤트 (외부 Invoke 차단)
    public static event Action<bool> OnTurnStarted;

    public enum EGamePhase { Setup, Battle }

    private const int StartCardCount    = 6; // 시작 손패 수 (앞줄 3 + 뒷줄 3)
    private const int FirstTurnSkillDraw = 4; // 첫 턴 스킬 드로우 시도 수
    private const int TurnSkillDraw      = 1; // 이후 매 턴 스킬 드로우 시도 수

    [CenterHeader("< Develop >")]
    [SerializeField, Tooltip("카드 배분이 매우 빨라집니다")] private bool _fastMode;

    [CenterHeader("< Properties >")]
    public bool isLoading; // true면 카드·엔티티 클릭을 막는다 (분배 중·게임오버 등)
    public bool myTurn;
    public EGamePhase phase;

    public bool IsBattlePhase => phase == EGamePhase.Battle;

    private readonly WaitForSeconds _addCardDelay     = new WaitForSeconds(0.5f);
    private readonly WaitForSeconds _fastAddCardDelay = new WaitForSeconds(0.05f);
    private readonly WaitForSeconds _turnDelay        = new WaitForSeconds(0.7f);

    private WaitForSeconds _currentAddCardDelay; // _fastMode에 따라 선택되는 현재 카드 분배 딜레이

    private bool _myFirstBattleTurn    = true; // 내 첫 전투 턴 여부 (스킬 4장 드로우)
    private bool _otherFirstBattleTurn = true; // 상대 첫 전투 턴 여부 (스킬 4장 드로우)


    // 싱글톤 등록 (Unity 메시지)
    private void Awake()
    {
        Inst = this;
    }

    #region 초기화 / 배치 페이즈

    // 게임 시작 코루틴 — 손패 6장씩 분배 후 배치 페이즈로 진입한다 (GameManager.StartGame이 호출)
    public IEnumerator StartGameCo()
    {
        GameSetup();
        isLoading = true;
        phase     = EGamePhase.Setup;

        for (int i = 0; i < StartCardCount; i++)
        {
            yield return _currentAddCardDelay;

            OnAddCard?.Invoke(false); // 상대 카드 한 장

            yield return _currentAddCardDelay;

            OnAddCard?.Invoke(true);  // 내 카드 한 장
        }

        EnemyAI.Inst.SetupPlace(); // 상대가 자기 6장을 자동 배치
        isLoading = false;         // 내 배치 입력 허용
    }

    // fastMode 딜레이 선택
    private void GameSetup()
    {
        _currentAddCardDelay = _fastMode ? _fastAddCardDelay : _addCardDelay;
    }

    // 배치 완료 — 전투 페이즈로 전환하고 내 선공으로 첫 턴을 시작한다 (SetupDoneBtn이 호출)
    public void OnSetupDone()
    {
        if (phase != EGamePhase.Setup)
        {
            return;
        }

        phase  = EGamePhase.Battle;
        myTurn = true; // 배치 완료 후 내가 선공

        StartCoroutine(StartTurnCo());
    }

    #endregion

    #region 전투 페이즈

    // 턴 종료 — 턴을 넘기고 다음 턴 시작 (EndTurnBtn 버튼·EnemyAI·치트가 호출)
    public void EndTurn()
    {
        myTurn = !myTurn;

        StartCoroutine(StartTurnCo());
    }

    // 턴 시작 — 알림 → 스킬 드로우 시도 → 입력 잠금 해제 → OnTurnStarted 발행
    private IEnumerator StartTurnCo()
    {
        isLoading = true;

        GameManager.Inst.Notification(myTurn ? "나의 턴" : "상대 턴");

        yield return _turnDelay;

        // 턴 시작 진영 마나 +1 회복
        ManaManager.Inst.GainMana(myTurn);

        // 전장 배치 이후 드로우는 스킬 카드만. 각 진영 첫 전투 턴은 4장, 이후 1장
        bool isFirstTurn = myTurn ? _myFirstBattleTurn : _otherFirstBattleTurn;
        CardManager.Inst.DrawSkillCards(myTurn, isFirstTurn ? FirstTurnSkillDraw : TurnSkillDraw);

        if (myTurn)
        {
            _myFirstBattleTurn = false;
        }
        else
        {
            _otherFirstBattleTurn = false;
        }

        yield return _turnDelay;

        isLoading = false;

        OnTurnStarted?.Invoke(myTurn); // 구독자들(매니저·버튼)에게 턴 시작 알림
    }

    #endregion
}
