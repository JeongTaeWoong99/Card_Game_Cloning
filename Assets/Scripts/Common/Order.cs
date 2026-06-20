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
    
    public void SetOriginOrder(int originOrder)
    {
        _originOrder = originOrder;
        SetOrder(originOrder);
    }

    public void SetMostFrontOrder(bool isMostFront)
    {
        SetOrder(isMostFront ? MostFrontOrder : _originOrder);
    }

    // 같은 order 안에서도 middle이 back보다 항상 위로 오도록 +1 한다 (배수로 간격 확보)
    public void SetOrder(int order)
    {
        int sortingOrder = order * OrderMultiplier;

        foreach (Renderer renderer in _backRenderers)
        {
            renderer.sortingLayerName = _sortingLayerName;
            renderer.sortingOrder = sortingOrder;
        }

        foreach (Renderer renderer in _middleRenderers)
        {
            renderer.sortingLayerName = _sortingLayerName;
            renderer.sortingOrder = sortingOrder + 1;
        }
    }
}
