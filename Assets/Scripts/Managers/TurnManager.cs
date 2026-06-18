using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Inst { get; private set; }

    public static Action<bool> OnAddCard;
    public static event Action<bool> OnTurnStarted;

    private enum ETurnMode { Random, My, Other }

    [Header("Develop")]
    [SerializeField, FormerlySerializedAs("eTurnMode"), Tooltip("시작 턴 모드를 정합니다")]       private ETurnMode _turnMode;
    [SerializeField, FormerlySerializedAs("fastMode"), Tooltip("카드 배분이 매우 빨라집니다")]     private bool      _fastMode;
    [SerializeField, FormerlySerializedAs("startCardCount"), Tooltip("시작 카드 개수를 정합니다")] private int       _startCardCount;

    [Header("Properties")]
    public bool isLoading; // 게임 끝나면 isLoading을 true로 하면 카드와 엔티티 클릭방지
    public bool myTurn;

    private WaitForSeconds _delay05 = new WaitForSeconds(0.5f);
    private readonly WaitForSeconds _delay07 = new WaitForSeconds(0.7f);


    private void Awake()
    {
        Inst = this;
    }

    public IEnumerator StartGameCo()
    {
        GameSetup();
        isLoading = true;

        for (int i = 0; i < _startCardCount; i++)
        {
            yield return _delay05;
            OnAddCard?.Invoke(false);
            yield return _delay05;
            OnAddCard?.Invoke(true);
        }
        StartCoroutine(StartTurnCo());
    }

    public void EndTurn()
    {
        myTurn = !myTurn;
        StartCoroutine(StartTurnCo());
    }

    private void GameSetup()
    {
        if (_fastMode)
        {
            _delay05 = new WaitForSeconds(0.05f);
        }

        myTurn = _turnMode switch
        {
            ETurnMode.My    => true,
            ETurnMode.Other => false,
            _               => Random.Range(0, 2) == 0,
        };
    }

    private IEnumerator StartTurnCo()
    {
        isLoading = true;
        if (myTurn)
        {
            GameManager.Inst.Notification("나의 턴");
        }

        yield return _delay07;
        OnAddCard?.Invoke(myTurn);
        yield return _delay07;
        isLoading = false;
        OnTurnStarted?.Invoke(myTurn);
    }
}
