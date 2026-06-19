using UnityEngine;

// 카메라 뷰포트를 기준 비율(16:9)로 고정하고, 화면과 비율이 맞지 않는 부분은
// 레터박스(위/아래) 또는 필러박스(좌/우)로 비워 게임 전체가 항상 잘림 없이 보이게 한다.
// 비워진 영역은 뒤쪽 배경 카메라(검은색)가 채운다.
[RequireComponent(typeof(Camera))]
public class AspectRatioController : MonoBehaviour
{
    [SerializeField, Tooltip("고정할 기준 화면 비율 (가로, 세로). 예: 16 x 9")]
    private Vector2 _targetAspect = new Vector2(16f, 9f);

    private Camera _camera;
    private int    _lastScreenWidth;
    private int    _lastScreenHeight;


    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Start()
    {
        ApplyLetterbox();
    }

    private void Update()
    {
        // 화면 크기(회전·창 크기 변경 등)가 바뀐 경우에만 재계산한다
        if (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight)
        {
            return;
        }

        ApplyLetterbox();
    }

    // 현재 화면 비율과 기준 비율을 비교해 카메라 뷰포트 Rect를 조정한다
    private void ApplyLetterbox()
    {
        _lastScreenWidth  = Screen.width;
        _lastScreenHeight = Screen.height;

        float targetAspect = _targetAspect.x / _targetAspect.y;
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight  = windowAspect / targetAspect;

        Rect rect = _camera.rect;

        if (scaleHeight < 1f)
        {
            // 화면이 기준보다 세로로 길다 → 위/아래 레터박스
            rect.width  = 1f;
            rect.height = scaleHeight;
            rect.x      = 0f;
            rect.y      = (1f - scaleHeight) * 0.5f;
        }
        else
        {
            // 화면이 기준보다 가로로 넓다 → 좌/우 필러박스
            float scaleWidth = 1f / scaleHeight;
            rect.width  = scaleWidth;
            rect.height = 1f;
            rect.x      = (1f - scaleWidth) * 0.5f;
            rect.y      = 0f;
        }

        _camera.rect = rect;
    }
}
