using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 게임오버 시 화면 채도를 낮춰(흑백) 표현한다.
// URP 글로벌 볼륨의 Color Adjustments → Saturation 을 제어한다. (weight가 아니라 채도 값 직접 조정)
public class CameraEffect : MonoBehaviour
{
    private const float GrayscaleSaturation = -100f; // 흑백으로 보이게 하는 채도

    [SerializeField] private Volume _volume;

    private ColorAdjustments _colorAdjustments;

    // 볼륨 프로파일의 Color Adjustments 오버라이드를 캐싱한다 (Unity 메시지)
    private void Awake()
    {
        if (_volume != null)
        {
            _volume.profile.TryGet(out _colorAdjustments);
        }
    }

    // 흑백 효과를 켜고 끈다 — Saturation을 -100(흑백) / 0(원상)으로 설정 (GameManager가 호출)
    public void SetGrayScale(bool isGrayscale)
    {
        if (_colorAdjustments == null)
        {
            return;
        }

        _colorAdjustments.saturation.overrideState = true;
        _colorAdjustments.saturation.value         = isGrayscale ? GrayscaleSaturation : 0f;
    }
}
