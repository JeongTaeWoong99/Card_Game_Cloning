using UnityEngine;
using TMPro;
using DG.Tweening;

public class Damage : MonoBehaviour
{
    [SerializeField] private TMP_Text _damageTMP;

    private Transform _target;

    private const int DamageOrder = 1000;


    public void SetupTransform(Transform target)
    {
        _target = target;
    }

    private void Update()
    {
        // 데미지 텍스트가 대상 위치를 따라다니도록 매 프레임 위치를 동기화한다
        if (_target != null)
        {
            transform.position = _target.position;
        }
    }

    public void Damaged(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        GetComponent<Order>().SetOrder(DamageOrder);
        _damageTMP.text = $"-{damage}";

        DOTween.Sequence()
            .Append(transform.DOScale(Vector3.one * 1.8f, 0.5f).SetEase(Ease.InOutBack))
            .AppendInterval(1.2f)
            .Append(transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InOutBack))
            .OnComplete(() => Destroy(gameObject));
    }
}
