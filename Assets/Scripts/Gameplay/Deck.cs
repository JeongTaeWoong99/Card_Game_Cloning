using System.Collections.Generic;

// 드로우 가중치를 가진 데이터(Item·Skill)가 구현하는 인터페이스. Deck<T>가 가중치 복제에 사용한다.
public interface IWeighted
{
    float Percent { get; }
}

// Percent 가중치만큼 원소를 복제해 채운 뒤 셔플한 뽑기 버퍼.
// 버퍼가 비면 자동으로 다시 채워 무한히 뽑을 수 있다. (MonoBehaviour가 아닌 순수 데이터 클래스)
// 엔티티 카드(Deck<Item>)와 스킬 카드(Deck<Skill>)가 함께 사용한다.
public class Deck<T> where T : IWeighted
{
    private readonly T[] _items;

    private List<T> _buffer;

    public Deck(T[] items)
    {
        _items = items;
        Refill();
    }

    // 버퍼에서 한 장을 꺼낸다 (비면 자동으로 다시 채움)
    public T Pop()
    {
        if (_buffer.Count == 0)
        {
            Refill();
        }

        T item = _buffer[0];
        _buffer.RemoveAt(0);

        return item;
    }

    // Percent 비율만큼 원소를 복제해 넣고 Fisher-Yates로 섞는다
    private void Refill()
    {
        _buffer = new List<T>(100);

        foreach (T item in _items)
        {
            for (int i = 0; i < item.Percent; i++)
            {
                _buffer.Add(item);
            }
        }

        Utils.Shuffle(_buffer);
    }
}
