using UnityEngine;
using UnityEngine.UI;

// 세팅 완료 / 턴 종료를 겸하는 통합 액션 버튼.
// Setup 페이즈: "세팅 완료"(양측 배치 완료 시 활성) — 클릭 시 전투 시작.
// Battle 페이즈: "턴 종료"(내 턴·로딩 아님일 때 활성) — 클릭 시 턴 종료.
public class ActionBtn : MonoBehaviour
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

    // 페이즈에 따라 텍스트·활성 상태를 매 프레임 갱신한다 (Unity 메시지)
    private void Update()
    {
        if (TurnManager.Inst.phase == TurnManager.EGamePhase.Setup)
        {
            _btnText.text = "세팅 완료";
            SetActiveLook(EntityManager.Inst.IsMyPlaceDone && EntityManager.Inst.IsOtherPlaceDone);
        }
        else
        {
            _btnText.text = "턴 종료";
            SetActiveLook(TurnManager.Inst.myTurn && !TurnManager.Inst.isLoading);
        }
    }

    // 클릭 — 세팅 페이즈면 전투 시작, 전투 페이즈면 턴 종료 (버튼 OnClick에 할당)
    public void OnClick()
    {
        if (TurnManager.Inst.phase == TurnManager.EGamePhase.Setup)
        {
            TurnManager.Inst.OnSetupDone();
        }
        else if (TurnManager.Inst.myTurn && !TurnManager.Inst.isLoading)
        {
            TurnManager.Inst.EndTurn();
        }
    }

    // 활성/비활성 외형 갱신
    private void SetActiveLook(bool isActive)
    {
        _image.sprite        = isActive ? _activeSprite : _inactiveSprite;
        _button.interactable = isActive;
        _btnText.color       = isActive ? ActiveTextColor : InactiveTextColor;
    }
}
