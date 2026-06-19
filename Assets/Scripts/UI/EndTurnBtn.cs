using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class EndTurnBtn : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("active")]   private Sprite _activeSprite;
    [SerializeField, FormerlySerializedAs("inactive")] private Sprite _inactiveSprite;
    [SerializeField, FormerlySerializedAs("btnText")]  private Text   _btnText;

    private Image _image;
    private Button _button;

    private static readonly Color32 ActiveTextColor   = new Color32(255, 195, 90, 255);
    private static readonly Color32 InactiveTextColor = new Color32(55, 55, 55, 255);


    private void Awake()
    {
        _image = GetComponent<Image>();
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
        _image.sprite = isActive ? _activeSprite : _inactiveSprite;
        _button.interactable = isActive;
        _btnText.color = isActive ? ActiveTextColor : InactiveTextColor;
    }
}
