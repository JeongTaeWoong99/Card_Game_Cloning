using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class ResultPanel : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("resultTMP")] private TMP_Text _resultTMP;


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
