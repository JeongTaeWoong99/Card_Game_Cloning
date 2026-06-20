using UnityEngine;
using TMPro;
using DG.Tweening;

// 화면 중앙에 잠깐 떠올랐다 사라지는 안내 메시지(예: "나의 턴") 패널.
public class NotificationPanel : MonoBehaviour
{
    [CenterHeader("< 참조 >")]
    [SerializeField] private TMP_Text _notificationTMP;
    
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
