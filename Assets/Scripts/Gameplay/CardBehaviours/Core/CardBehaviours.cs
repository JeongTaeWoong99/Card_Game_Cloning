using System.Collections.Generic;

// ECardType → 행동 객체 매핑. 새 카드 타입을 추가할 때만 이 표에 한 줄을 더한다 (흩어진 switch 제거).
public static class CardBehaviours
{
    private static readonly Dictionary<ECardType, ICardBehaviour> Map = new()
    {
        { ECardType.Normal,  new NormalBehaviour()  },
        { ECardType.Ranged,  new RangedBehaviour()  },
        { ECardType.Musou,   new MusouBehaviour()   },
        { ECardType.Healer,  new HealerBehaviour()  },
        { ECardType.Shield,  new ShieldBehaviour()  },
        { ECardType.Vampire, new VampireBehaviour() },
    };

    // 해당 타입의 행동 객체를 반환한다
    public static ICardBehaviour Of(ECardType type)
    {
        return Map[type];
    }
}
