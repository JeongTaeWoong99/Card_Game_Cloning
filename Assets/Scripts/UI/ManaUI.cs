using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 한 진영의 마나를 원형 게이지(채움)와 "현재/최대" 텍스트로 표시한다.
// ManaManager.ManaChanged를 구독해 자기 진영(_isMine)의 변동만 반영한다 (Observer).
public class ManaUI : MonoBehaviour
{
    [CenterHeader("< 참조 >")]
    //[SerializeField] private Image    _fillImage; // 원형 채움 (Image Type=Filled, Radial)
    [SerializeField] private TMP_Text _manaText;  // "현재/최대" 표기 (예: 2/3)

    [CenterHeader("< 설정 >")]
    [SerializeField] private bool _isMine;


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

    // 내 진영 마나가 바뀌면 게이지를 갱신한다 (ManaChanged 구독)
    private void OnManaChanged(bool isMine, int currentMana)
    {
        if (isMine != _isMine)
        {
            return;
        }

        Refresh(currentMana);
    }

    // 원형 게이지 채움 비율과 "현재/최대" 텍스트를 갱신한다
    private void Refresh(int currentMana)
    {
        // if (_fillImage != null)
        // {
        //     _fillImage.fillAmount = (float)currentMana / ManaManager.MaxMana;
        // }

        if (_manaText != null)
        {
            _manaText.text = $"{currentMana}/{ManaManager.MaxMana}";
        }
    }
}
