using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 세팅 완료 / 턴 종료를 겸하는 통합 액션 버튼.
// Setup 페이즈: "세팅 완료"(양측 배치 완료 시 활성) — 클릭 시 전투 시작.
// Battle 페이즈: "턴 종료"(내 턴·로딩 아님일 때 활성) — 클릭 시 턴 종료.
public class ActionBtn : MonoBehaviour
{
    [CenterHeader("< 스프라이트 / 텍스트 >")]
    [SerializeField] private Sprite          _activeSprite;
    [SerializeField] private Sprite          _inactiveSprite;
    [SerializeField] private TextMeshProUGUI _btnText;

    private Image _image;

    private static readonly Color32 ActiveTextColor   = new Color32(255, 195, 90, 255);
    private static readonly Color32 InactiveTextColor = new Color32(127, 127, 127, 255);

    // 컴포넌트 캐싱 (Unity 메시지)
    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    // 페이즈에 따라 텍스트·활성 상태를 매 프레임 갱신한다 (Unity 메시지)
    private void Update()
    {
        // 데미지 프리뷰가 떠 있으면 버튼이 앞을 가리지 않도록 숨긴다 (렌더·레이캐스트 모두 차단)
        if (CardManager.Inst.IsDamagePreviewActive)
        {
            SetVisible(false);
            return;
        }
        SetVisible(true);

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

    // 클릭 — 세팅 페이즈면 전투 시작(미완료면 안내), 전투 페이즈면 턴 종료 (버튼 OnClick에 할당)
    // 버튼은 항상 클릭 가능하게 두고(비활성 외형만 표시), 실제 가능 여부는 여기서 판정한다.
    public void OnClick()
    {
        if (TurnManager.Inst.isLoading) // 로딩 중 입력 무시
        {
            return;
        }

        if (TurnManager.Inst.phase == TurnManager.EGamePhase.Setup)
        {
            if (EntityManager.Inst.IsMyPlaceDone && EntityManager.Inst.IsOtherPlaceDone)
            {
                TurnManager.Inst.OnSetupDone();
            }
            else
            {
                CardManager.Inst.ShowWarning("아직 배치가 끝나지 않았습니다.\n6장의 카드를 모두 배치해 주세요.");
            }
        }
        else if (TurnManager.Inst.myTurn)
        {
            TurnManager.Inst.EndTurn();
        }
    }

    // 활성/비활성 외형 갱신 (클릭 자체는 항상 허용 — interactable은 건드리지 않는다)
    private void SetActiveLook(bool isActive)
    {
        _image.sprite  = isActive ? _activeSprite : _inactiveSprite;
        _btnText.color = isActive ? ActiveTextColor : InactiveTextColor;
    }

    // 버튼 렌더·레이캐스트 표시 여부 (이미지/텍스트 끄면 클릭도 통과)
    private void SetVisible(bool visible)
    {
        _image.enabled   = visible;
        _btnText.enabled = visible;
    }
}
