using UnityEngine;
using UnityEngine.UI;

// 한 진영의 마나를 10칸 분할 바로 표시한다. 채워진 칸 수만큼 색을 켜고 나머지는 빈칸 색으로 둔다.
// ManaManager.ManaChanged를 구독해 자기 진영(_isMine)의 변동만 반영한다 (Observer).
public class ManaUI : MonoBehaviour
{
    [CenterHeader("< 참조 >")]
    [SerializeField] private Image[] _cells; // 좌→우 10칸 (ManaManager.MaxMana와 동일 길이)

    [CenterHeader("< 설정 >")]
    [SerializeField] private bool _isMine;

    private static readonly Color FilledColor = new Color(0.30f, 0.65f, 1f, 1f);   // 채워진 칸(파랑)
    private static readonly Color EmptyColor  = new Color(0.20f, 0.20f, 0.25f, 1f); // 빈 칸(어두운 회색)


    // 이벤트 구독 + 현재 값으로 초기 동기화 (Unity 메시지)
    private void Start()
    {
        ManaManager.ManaChanged += OnManaChanged;

        Refresh(ManaManager.Inst.GetMana(_isMine));
    }

    // 이벤트 구독 해제 (Unity 메시지)
    private void OnDestroy()
    {
        ManaManager.ManaChanged -= OnManaChanged;
    }

    // 내 진영 마나가 바뀌면 칸을 갱신한다 (ManaChanged 구독)
    private void OnManaChanged(bool isMine, int currentMana)
    {
        if (isMine != _isMine)
        {
            return;
        }

        Refresh(currentMana);
    }

    // 채워진 칸 수만큼 색을 켜고 나머지는 빈칸 색으로 둔다
    private void Refresh(int currentMana)
    {
        for (int i = 0; i < _cells.Length; i++)
        {
            _cells[i].color = i < currentMana ? FilledColor : EmptyColor;
        }
    }
}
