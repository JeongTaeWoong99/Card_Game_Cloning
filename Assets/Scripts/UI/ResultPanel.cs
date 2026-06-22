using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

// 승리/패배 결과 패널. 스케일 연출로 등장하며 Restart로 씬을 다시 로드한다.
public class ResultPanel : MonoBehaviour
{
    [CenterHeader("< 참조 >")]
    [SerializeField] private TMP_Text _resultTMP;


    // 시작 시 숨김 (Unity 메시지)
    private void Start() => ScaleZero();

    // 결과 텍스트 표시 + 스케일 등장 연출 (GameManager.GameOver가 호출)
    public void Show(string message)
    {
        _resultTMP.text = message;
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.InOutQuad);
    }

    // 씬을 처음부터 재시작 (Restart 버튼 OnClick에 할당)
    public void Restart()
    {
        SceneManager.LoadScene(0);
    }

    // 즉시 펼침 (인스펙터 디버그용)
    [ContextMenu("ScaleOne")]
    private void ScaleOne() => transform.localScale = Vector3.one;

    // 즉시 숨김 (초기화 / 디버그용)
    [ContextMenu("ScaleZero")]
    public void ScaleZero() => transform.localScale = Vector3.zero;
}
