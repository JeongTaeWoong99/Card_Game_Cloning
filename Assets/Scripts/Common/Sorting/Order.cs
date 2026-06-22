using UnityEngine;

public class Order : MonoBehaviour
{
    private const int OrderMultiplier = 10;
    private const int MostFrontOrder  = 100;

    [CenterHeader("< 렌더러 >")]
    [SerializeField] private Renderer[] _backRenderers;
    [SerializeField] private Renderer[] _middleRenderers;

    [CenterHeader("< 정렬 레이어 >")]
    [SerializeField] private string _sortingLayerName;

    private int _originOrder;
    
    // 기준 정렬 순서를 저장하고 즉시 적용한다
    public void SetOriginOrder(int originOrder)
    {
        _originOrder = originOrder;
        SetOrder(originOrder);
    }

    // 최상단으로 끌어올리거나(true) 기준 순서로 복귀(false)
    public void SetMostFrontOrder(bool isMostFront)
    {
        SetOrder(isMostFront ? MostFrontOrder : _originOrder);
    }

    // 지정 order로 back/middle 렌더러 정렬. 같은 order 안에서도 middle이 back보다 위로 오도록 +1 (배수로 간격 확보)
    public void SetOrder(int order)
    {
        int sortingOrder = order * OrderMultiplier;

        foreach (Renderer renderer in _backRenderers)
        {
            if (renderer == null) // 인스펙터 미할당(빈 슬롯) 방어
            {
                continue;
            }

            renderer.sortingLayerName = _sortingLayerName;
            renderer.sortingOrder     = sortingOrder;
        }

        foreach (Renderer renderer in _middleRenderers)
        {
            if (renderer == null) // 인스펙터 미할당(빈 슬롯) 방어
            {
                continue;
            }

            renderer.sortingLayerName = _sortingLayerName;
            renderer.sortingOrder     = sortingOrder + 1;
        }
    }
}
