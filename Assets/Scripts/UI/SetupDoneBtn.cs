using UnityEngine;
using UnityEngine.UI;

// 배치 완료 버튼. Setup 페이즈에서 내·상대 배치가 모두 끝나면 활성화되고,
// 클릭하면 전투 페이즈를 시작한다. 전투가 시작되면 스스로 사라진다.
public class SetupDoneBtn : MonoBehaviour
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

    // 배치 완료 가능 여부를 매 프레임 갱신하고, 전투가 시작되면 숨긴다 (Unity 메시지)
    private void Update()
    {
        if (TurnManager.Inst.IsBattlePhase)
        {
            gameObject.SetActive(false);

            return;
        }

        bool ready = EntityManager.Inst.IsMyPlaceDone && EntityManager.Inst.IsOtherPlaceDone;
        SetActiveLook(ready);
    }

    // 클릭 — 전투 페이즈 시작 (버튼 OnClick에 할당)
    public void OnClick()
    {
        TurnManager.Inst.OnSetupDone();
    }

    // 활성/비활성 외형 갱신
    private void SetActiveLook(bool isActive)
    {
        _image.sprite        = isActive ? _activeSprite : _inactiveSprite;
        _button.interactable = isActive;
        _btnText.color       = isActive ? ActiveTextColor : InactiveTextColor;
    }
}
