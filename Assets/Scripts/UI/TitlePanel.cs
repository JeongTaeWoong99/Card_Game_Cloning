using UnityEngine;

// 타이틀 패널. 시작 버튼으로 게임을 시작하고 자신을 숨긴다.
public class TitlePanel : MonoBehaviour
{
    public void StartGameClick()
    {
        GameManager.Inst.StartGame();
        Active(false);
    }

    public void Active(bool isActive)
    {
        gameObject.SetActive(isActive);
    }
}
