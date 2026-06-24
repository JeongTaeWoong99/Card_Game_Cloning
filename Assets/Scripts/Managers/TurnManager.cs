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

    private const int StartCardCount = 6; // 시작 손패 수 (앞줄 3 + 뒷줄 3)
    public  const int SetupSkillDraw = 4; // 세팅 완료 시 받는 스킬 수 (EnemyAI도 참조)
    private const int TurnSkillDraw  = 1; // 첫 전투 턴 이후 매 턴 스킬 드로우 수

    [CenterHeader("< Develop >")]
    [SerializeField, Tooltip("카드 배분이 매우 빨라집니다")] private bool _fastMode;

    [CenterHeader("< Properties >")]
    public bool isLoading; // true면 카드·엔티티 클릭을 막는다 (분배 중·게임오버 등)
    public bool myTurn;
    public EGamePhase phase;

    public bool IsBattlePhase => phase == EGamePhase.Battle;

    private readonly WaitForSeconds _addCardDelay      = new WaitForSeconds(0.5f);
    private readonly WaitForSeconds _fastAddCardDelay  = new WaitForSeconds(0.05f);
    private readonly WaitForSeconds _notificationDelay = new WaitForSeconds(1.5f); // 턴 알림 연출(커짐→유지→사라짐) 시간

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

        GameManager.Inst.Notification("세팅 턴"); // 배치 단계 안내
        EnemyAI.Inst.SetupPlace();                // 상대가 자기 6장을 자동 배치(끝나면 스킬 4장 드로우)
        isLoading = false;                        // 내 배치 입력 허용
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

        CardManager.Inst.DrawSkillCards(true, SetupSkillDraw); // 세팅 완료 시 내 스킬 4장

        StartCoroutine(StartTurnCo());
    }

    #endregion

    #region 전투 페이즈

    // 턴 종료 — 턴을 넘기고 다음 턴 시작 (EndTurnBtn 버튼·EnemyAI·치트가 호출)
    public void EndTurn()
    {
        EntityManager.Inst.TickBuffs(myTurn); // 방금 끝난 진영의 한시 버프 만료

        myTurn = !myTurn;

        StartCoroutine(StartTurnCo());
    }

    // 턴 시작 — 마나·드로우 → 알림(완전히 사라질 때까지) → 턴 시작 효과 → 입력 잠금 해제 → 버튼 갱신/AI 가동
    private IEnumerator StartTurnCo()
    {
        isLoading = true; // 효과가 모두 끝날 때까지 입력 잠금 유지

        // 턴 시작 진영 마나 +1 회복
        ManaManager.Inst.GainMana(myTurn);

        // 첫 4장은 세팅 완료 시 받았으므로, 첫 전투 턴은 드로우 0, 이후 매 턴 1장
        bool isFirstTurn = myTurn ? _myFirstBattleTurn : _otherFirstBattleTurn;
        CardManager.Inst.DrawSkillCards(myTurn, isFirstTurn ? 0 : TurnSkillDraw);

        if (myTurn)
        {
            _myFirstBattleTurn = false;
        }
        else
        {
            _otherFirstBattleTurn = false;
        }

        // 턴 알림을 먼저 띄우고, 패널이 완전히 사라질 때까지 기다린다 (효과가 알림에 가리지 않게)
        GameManager.Inst.Notification(myTurn ? "나의 턴" : "상대 턴");
        yield return _notificationDelay;

        // 알림이 사라진 뒤 후방/턴시작 효과를 한 장씩 순차로 발동하고, 모두 끝날 때까지 대기한다
        yield return StartCoroutine(EntityManager.Inst.ProcessTurnStartEffectsCo(myTurn));

        isLoading = false;

        OnTurnStarted?.Invoke(myTurn); // 구독자(턴 종료 버튼 등) 갱신

        if (!myTurn)
        {
            EnemyAI.Inst.Play(); // 상대 턴이면 AI 행동 시작
        }
    }

    #endregion
}
