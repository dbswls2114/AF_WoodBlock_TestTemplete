using UnityEngine;

public class ButtonSystem : MonoBehaviour
{
    public void OnRestartGame()
    {
        GameManager.Instance.RestartGame();
    }

    public void OnReturnToMainMenu()
    {
        //메인메뉴로 이동
        //GameManager.Instance.ReturnToMainMenu();
    }

    public void OnStartGame()
    {
        //메인메뉴에서 게임 시작
        //GameManager.Instance.StartGame();
    }

}
