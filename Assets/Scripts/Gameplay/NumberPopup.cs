using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using DG.Tweening;

// HP 변동(-N 피해 / +N 회복·버프)을 대상 위에 띄우는 숫자 팝업.
// 텍스트 색·크기는 프리팹(데미지/힐)이 각자 결정하고, 코드는 부호와 수치만 채운다.
public class NumberPopup : MonoBehaviour
{
    [CenterHeader("< 참조 >")]
    [FormerlySerializedAs("_damageTMP")]
    [SerializeField] private TMP_Text _numberTMP;

    private Transform _target;

    private const int   PopupOrder   = 1000;
    private const float PopScale     = 1.8f; // 등장 시 프리팹 크기 대비 확대 배율
    private const float RiseDistance = 1.0f; // 회복 팝업이 위로 떠오르는 거리(월드)
    private const float RiseTime     = 0.8f; // 떠오르며 사라지는 시간

    // 따라다닐 대상을 지정한다 (CombatSystem이 호출)
    public void SetupTransform(Transform target)
    {
        _target = target;

        if (target != null)
        {
            transform.position = target.position; // 첫 프레임부터 대상 위치에 맞춘다
        }
    }

    // 대상 위치 추적 (Unity 메시지) — 회복 팝업은 _target을 비워 추적을 끄고 위로 떠오른다
    private void Update()
    {
        if (_target != null)
        {
            transform.position = _target.position;
        }
    }

    // -N 표시 + 제자리에서 커졌다 사라지는 연출 (데미지 프리팹 — 대상을 계속 추적)
    public void Damaged(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        SetText($"-{damage}");

        Vector3 peakScale = transform.localScale * PopScale; // 프리팹 지정 크기를 기준으로 확대

        DOTween.Sequence()
            .Append(transform.DOScale(peakScale, 0.5f).SetEase(Ease.InOutBack))
            .AppendInterval(0.8f)
            .Append(transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InOutBack))
            .OnComplete(() => Destroy(gameObject));
    }

    // +N 표시 + 위로 떠오르며 서서히 사라지는 연출 (힐 프리팹 — 추적을 끄고 상승)
    public void Healed(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        _target = null; // 상승 동안 위치 고정을 풀어 떠오를 수 있게 한다
        SetText($"+{amount}");

        Vector3 peakScale = transform.localScale * PopScale;
        float   targetY   = transform.position.y + RiseDistance;

        _numberTMP.alpha = 1f;

        DOTween.Sequence()
            .Append(transform.DOScale(peakScale, 0.2f).SetEase(Ease.OutBack))    // 살짝 팝
            .Append(transform.DOMoveY(targetY, RiseTime).SetEase(Ease.OutQuad))  // 위로 상승
            .Join(DOVirtual.Float(1f, 0f, RiseTime, a => _numberTMP.alpha = a))  // 서서히 사라짐
            .OnComplete(() => Destroy(gameObject));
    }

    // 정렬 순서를 올리고 텍스트를 세팅한다 (색은 프리팹 TMP 설정을 그대로 사용)
    private void SetText(string content)
    {
        GetComponent<Order>().SetOrder(PopupOrder);
        _numberTMP.text = content;
    }
}
