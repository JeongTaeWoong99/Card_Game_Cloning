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
    
    private void Awake()
    {
        _image  = GetComponent<Image>();
        _button = GetComponent<Button>();
    }

    private void Start()
    {
        Setup(false);
        TurnManager.OnTurnStarted += Setup;
    }

    private void OnDestroy()
    {
        TurnManager.OnTurnStarted -= Setup;
    }

    public void Setup(bool isActive)
    {
        _image.sprite        = isActive ? _activeSprite : _inactiveSprite;
        _button.interactable = isActive;
        _btnText.color       = isActive ? ActiveTextColor : InactiveTextColor;
    }
}
