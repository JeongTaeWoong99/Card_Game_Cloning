using UnityEngine;

// 카드 한 종류의 원본 데이터. percent는 덱 뽑기 가중치(높을수록 자주 등장).
[System.Serializable]
public class Item
{
    public string name;
    public int    attack;
    public int    health;
    public Sprite sprite;
    public float  percent;
}

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Object/ItemSO")]
public class ItemSO : ScriptableObject
{
    public Item[] items;
}
