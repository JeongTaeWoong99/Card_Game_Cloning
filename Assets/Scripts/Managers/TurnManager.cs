using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

// 턴 흐름을 제어한다: 게임 시작 시 카드 분배 → 턴 시작/종료 이벤트 발행.
public class TurnManager : MonoBehaviour
{
    public static TurnManager Inst { get; private set; }

    // 외부(GameManager 치트 등)에서도 Invoke 해야 하므로 event가 아닌 Action으로 둔다
    // (event는 선언 클래스 내부에서만 Invoke 가능)
    public static Action<bool> OnAddCard;

    // 내부에서만 발행하는 순수 발행-구독 이벤트 (외부 Invoke 차단)
    public static event Action<bool> OnTurnStarted;

    private enum ETurnMode { Random, My, Other }

    [CenterHeader("< Develop >")]
    [SerializeField, Tooltip("시작 턴 모드를 정합니다")]    private ETurnMode _turnMode;
    [SerializeField, Tooltip("카드 배분이 매우 빨라집니다")] private bool      _fastMode;
    [SerializeField, Tooltip("시작 카드 개수를 정합니다")]   private int       _startCardCount;

    [CenterHeader("< Properties >")]
    public bool isLoading; // true면 카드·엔티티 클릭을 막는다 (분배 중·게임오버 등)
    public bool myTurn;

    private readonly WaitForSeconds _addCardDelay     = new WaitForSeconds(0.5f);
    private readonly WaitForSeconds _fastAddCardDelay = new WaitForSeconds(0.05f);
    private readonly WaitForSeconds _turnDelay        = new WaitForSeconds(0.7f);

    private WaitForSeconds _currentAddCardDelay; // _fastMode에 따라 선택되는 현재 카드 분배 딜레이


    // 싱글톤 등록 (Unity 메시지)
    private void Awake()
    {
        Inst = this;
    }

    #region 초기화

    // 게임 시작 코루틴 — 선공 결정 후 시작 카드를 상대→나 순으로 번갈아 분배 (GameManager.StartGame이 호출)
    public IEnumerator StartGameCo()
    {
        GameSetup();
        isLoading = true;

        for (int i = 0; i < _startCardCount; i++)
        {
            yield return _currentAddCardDelay;

            OnAddCard?.Invoke(false); // 상대 카드 한 장

            yield return _currentAddCardDelay;

            OnAddCard?.Invoke(true);  // 내 카드 한 장
        }

        StartCoroutine(StartTurnCo());
    }

    // fastMode 딜레이 선택 + 선공(myTurn) 결정
    private void GameSetup()
    {
        _currentAddCardDelay = _fastMode ? _fastAddCardDelay : _addCardDelay;

        myTurn = _turnMode switch
        {
            ETurnMode.My    => true,
            ETurnMode.Other => false,
            _               => Random.Range(0, 2) == 0, // Random 모드는 동전 던지기
        };
    }

    #endregion

    #region 게임 진행

    // 턴 종료 — 턴을 넘기고 다음 턴 시작 (EndTurnBtn 버튼·EnemyAI·치트가 호출)
    public void EndTurn()
    {
        myTurn = !myTurn;

        StartCoroutine(StartTurnCo());
    }

    // 턴 시작 — 알림 → 카드 1장 분배 → 입력 잠금 해제 → OnTurnStarted 발행
    private IEnumerator StartTurnCo()
    {
        isLoading = true;

        GameManager.Inst.Notification(myTurn ? "나의 턴" : "상대 턴");

        yield return _turnDelay;

        OnAddCard?.Invoke(myTurn); // 턴 시작 시 카드 보충

        yield return _turnDelay;

        isLoading = false;

        OnTurnStarted?.Invoke(myTurn); // 구독자들(매니저·엔티티·버튼)에게 턴 시작 알림
    }

    #endregion
}
