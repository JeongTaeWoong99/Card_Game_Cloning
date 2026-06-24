using UnityEngine;
using TMPro;
using DG.Tweening;

// 승리/패배 결과 패널. 스케일 연출로 등장한다. (다시하기·메인메뉴·종료 버튼은 GameManager에 직접 연결)
public class ResultPanel : MonoBehaviour
{
    [CenterHeader("< 참조 >")]
    [SerializeField] private TMP_Text _resultTMP;


    // 시작 시 숨김 (Unity 메시지)
    private void Start() => Hide();

    // 결과 텍스트 표시 — 패널을 켜고 스케일 0→1 등장 연출 (GameManager.GameOver가 호출)
    public void Show(string message)
    {
        gameObject.SetActive(true);
        transform.localScale = Vector3.zero;
        _resultTMP.text      = message;
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.InOutQuad);
    }

    // 숨김 — 스케일 0으로 되돌리고 패널을 끈다 (평소엔 꺼져 뒤 보드 입력을 막지 않음) (GameManager.UISetup이 호출)
    public void Hide()
    {
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }

    // 즉시 펼침 (인스펙터 디버그용)
    [ContextMenu("ScaleOne")]
    private void ScaleOne() => transform.localScale = Vector3.one;
}
