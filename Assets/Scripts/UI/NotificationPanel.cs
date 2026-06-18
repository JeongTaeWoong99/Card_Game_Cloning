using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using DG.Tweening;

public class NotificationPanel : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("notificationTMP")] private TMP_Text _notificationTMP;


    private void Start() => ScaleZero();

    public void Show(string message)
    {
        _notificationTMP.text = message;
        DOTween.Sequence()
            .Append(transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InOutQuad))
            .AppendInterval(0.9f)
            .Append(transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InOutQuad));
    }

    [ContextMenu("ScaleOne")]
    private void ScaleOne() => transform.localScale = Vector3.one;

    [ContextMenu("ScaleZero")]
    public void ScaleZero() => transform.localScale = Vector3.zero;
}
