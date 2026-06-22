using UnityEngine;

// 타이틀 패널. 시작 버튼으로 게임을 시작하고 자신을 숨긴다.
public class TitlePanel : MonoBehaviour
{
    // 게임 시작 (GameStartBtn OnClick에 할당)
    public void StartGameClick()
    {
        GameManager.Inst.StartGame();

        // 시작 패널 끄기
        Active(false);
    }

    // 패널 켜기/끄기
    public void Active(bool isActive)
    {
        gameObject.SetActive(isActive);
    }
}