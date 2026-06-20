using System.Collections.Generic;

// ItemSO의 percent 가중치만큼 아이템을 복제해 채운 뒤 셔플한 뽑기 버퍼.
// 버퍼가 비면 자동으로 다시 채워 무한히 뽑을 수 있다. (MonoBehaviour가 아닌 순수 데이터 클래스)
public class Deck
{
    private readonly Item[] _items;

    private List<Item> _buffer;

    public Deck(Item[] items)
    {
        _items = items;
        Refill();
    }

    public Item Pop()
    {
        if (_buffer.Count == 0)
        {
            Refill();
        }

        Item item = _buffer[0];
        _buffer.RemoveAt(0);

        return item;
    }

    // percent 비율만큼 아이템을 복제해 넣고 Fisher-Yates로 섞는다
    private void Refill()
    {
        _buffer = new List<Item>(100);

        foreach (Item item in _items)
        {
            for (int i = 0; i < item.percent; i++)
            {
                _buffer.Add(item);
            }
        }

        Utils.Shuffle(_buffer);
    }
}
