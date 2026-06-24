using System.Collections;
using UnityEngine;
using DG.Tweening;

// 타이틀 패널. 시작 버튼을 누르면 배경을 디졸브로 지운 뒤 게임을 시작하고 자신을 숨긴다.
public class TitlePanel : MonoBehaviour
{
    private const float  DissolveTime = 1.2f;              // 타이틀 배경 디졸브 시간
    private const string DissolveProp = "_DissolveAmount"; // 디졸브 진행도 프로퍼티

    [CenterHeader("< 참조 >")]
    [SerializeField] private SpriteRenderer _titleBackground; // 디졸브 머티리얼이 적용된 배경
    [SerializeField] private GameObject     _framePanel;
    [SerializeField] private GameObject     _gameStartBtn;
    [SerializeField] private GameObject     _exitBtn;

    private Material _dissolveMat;

    // 디졸브 머티리얼을 인스턴스화하고 진행도를 0으로 초기화한다 — 공유 에셋이 영구 변경되지 않도록 (Unity 메시지)
    private void Awake()
    {
        if (_titleBackground != null)
        {
            _dissolveMat = new Material(_titleBackground.material);
            _titleBackground.material = _dissolveMat;
            _dissolveMat.SetFloat(DissolveProp, 0f); // 플레이 시작 시 디졸브 미진행 상태로 리셋
        }
    }

    // 게임 시작 — 배경 디졸브 + 프레임/버튼 숨김 후 게임을 시작한다 (GameStartBtn OnClick에 할당)
    public void StartGameClick()
    {
        StartCoroutine(DissolveAndStartCo());
    }

    // 배경을 디졸브(0→1)로 지우면서 프레임/버튼을 함께 숨기고, 완료되면 게임을 시작한다
    private IEnumerator DissolveAndStartCo()
    {
        _framePanel.SetActive(false);
        _gameStartBtn.SetActive(false);
        _exitBtn.SetActive(false);

        if (_dissolveMat != null)
        {
            yield return _dissolveMat.DOFloat(1f, DissolveProp, DissolveTime).WaitForCompletion();
        }

        GameManager.Inst.StartGame();
        gameObject.SetActive(false);
    }

    // 패널 켜기/끄기 (GameManager가 호출)
    public void Active(bool isActive)
    {
        gameObject.SetActive(isActive);
    }

    // 디졸브를 즉시 완료(1)로 만들어 타이틀 배경을 숨긴다 — 타이틀을 건너뛰는 다시하기에서 사용 (GameManager가 호출)
    public void SetDissolved()
    {
        if (_dissolveMat != null)
        {
            _dissolveMat.SetFloat(DissolveProp, 1f);
        }
    }
}
