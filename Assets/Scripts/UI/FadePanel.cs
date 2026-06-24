using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// 화면 전체를 덮는 검정 페이드. 씬 시작 시 검정→투명, 씬 전환 직전 투명→검정 연출을 담당한다.
public class FadePanel : MonoBehaviour
{
    private const float FadeTime = 0.6f; // 페이드 인/아웃 시간

    [CenterHeader("< 참조 >")]
    [SerializeField] private Image _fadeImage; // 화면을 덮는 검정 이미지(RGB는 검정, 알파만 제어)

    // 씬 시작 — 검정(알파 1)에서 서서히 투명해지고, 끝나면 자신을 끈다 (GameManager.Start가 호출)
    public void FadeIn()
    {
        gameObject.SetActive(true);
        Fade(1f, 0f, () => gameObject.SetActive(false));
    }

    // 씬 전환 직전 — 투명에서 검정(알파 1)으로 덮은 뒤 콜백을 실행한다 (GameManager.ToMainMenu가 호출)
    public void FadeOut(Action onComplete)
    {
        gameObject.SetActive(true);
        Fade(0f, 1f, onComplete);
    }

    // 이미지 알파를 from→to로 보간하고 완료 시 콜백을 실행한다
    private void Fade(float from, float to, Action onComplete)
    {
        SetAlpha(from);
        DOVirtual.Float(from, to, FadeTime, SetAlpha)
                 .SetEase(Ease.InOutQuad)
                 .OnComplete(() => onComplete?.Invoke());
    }

    // 검정 이미지의 알파만 설정한다
    private void SetAlpha(float alpha)
    {
        var color = _fadeImage.color;
        color.a          = alpha;
        _fadeImage.color = color;
    }
}
