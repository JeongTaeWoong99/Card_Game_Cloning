using UnityEngine;
using UnityEngine.Rendering;

// 게임오버 시 화면 전체를 흑백·어둡게 만드는 URP 글로벌 볼륨을 토글한다.
// (URP는 Built-in 의 OnRenderImage 를 호출하지 않으므로 Volume 가중치로 포스트프로세싱을 제어)
public class CameraEffect : MonoBehaviour
{
    [SerializeField] private Volume _grayscaleVolume;

    // 흑백·어둡게 효과를 켜고 끈다 (GameManager가 호출)
    public void SetGrayScale(bool isGrayscale)
    {
        if (_grayscaleVolume == null)
        {
            return;
        }

        // weight 0 = 효과 없음, 1 = 흑백·어둡게 전체 적용
        _grayscaleVolume.weight = isGrayscale ? 1f : 0f;
    }
}