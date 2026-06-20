using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

// 승리/패배 결과 패널. 스케일 연출로 등장하며 Restart로 씬을 다시 로드한다.
public class ResultPanel : MonoBehaviour
{
    [CenterHeader("< 참조 >")]
    [SerializeField] private TMP_Text _resultTMP;


    private void Start() => ScaleZero();

    public void Show(string message)
    {
        _resultTMP.text = message;
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.InOutQuad);
    }

    public void Restart()
    {
        SceneManager.LoadScene(0);
    }

    [ContextMenu("ScaleOne")]
    private void ScaleOne() => transform.localScale = Vector3.one;

    [ContextMenu("ScaleZero")]
    public void ScaleZero() => transform.localScale = Vector3.zero;
}
