using UnityEngine;
using UnityEngine.UI;

// 턴 종료 버튼. 내 턴일 때만 활성 스프라이트·색상으로 바뀌고 클릭 가능해진다.
public class EndTurnBtn : MonoBehaviour
{
    [CenterHeader("< 스프라이트 / 텍스트 >")]
    [SerializeField] private Sprite _activeSprite;
    [SerializeField] private Sprite _inactiveSprite;
    [SerializeField] private Text   _btnText;

    private Image  _image;
    private Button _button;

    private static readonly Color32 ActiveTextColor   = new Color32(255, 195, 90, 255);
    private static readonly Color32 InactiveTextColor = new Color32(55, 55, 55, 255);
    
    // 컴포넌트 캐싱 (Unity 메시지)
    private void Awake()
    {
        _image  = GetComponent<Image>();
        _button = GetComponent<Button>();
    }

    // 초기 비활성 + 턴 이벤트 구독 (Unity 메시지)
    private void Start()
    {
        Setup(false);
        TurnManager.OnTurnStarted += Setup;
    }

    // 이벤트 구독 해제 (Unity 메시지)
    private void OnDestroy()
    {
        TurnManager.OnTurnStarted -= Setup;
    }

    // 내 턴이면 활성, 아니면 비활성 외형으로 갱신 (OnTurnStarted 구독)
    public void Setup(bool isActive)
    {
        _image.sprite        = isActive ? _activeSprite : _inactiveSprite;
        _button.interactable = isActive;
        _btnText.color       = isActive ? ActiveTextColor : InactiveTextColor;
    }
}
