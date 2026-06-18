using UnityEngine;
using UnityEngine.Serialization;

public class Order : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("backRenderers")]    private Renderer[] _backRenderers;
    [SerializeField, FormerlySerializedAs("middleRenderers")]  private Renderer[] _middleRenderers;
    [SerializeField, FormerlySerializedAs("sortingLayerName")] private string     _sortingLayerName;

    private int _originOrder;

    private const int OrderMultiplier = 10;
    private const int MostFrontOrder = 100;


    public void SetOriginOrder(int originOrder)
    {
        _originOrder = originOrder;
        SetOrder(originOrder);
    }

    public void SetMostFrontOrder(bool isMostFront)
    {
        SetOrder(isMostFront ? MostFrontOrder : _originOrder);
    }

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
