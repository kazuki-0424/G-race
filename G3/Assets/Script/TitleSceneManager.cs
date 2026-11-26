using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneManager : MonoBehaviour
{
    [Header("移行先シーン名")]
    [SerializeField] private string NextSceneName = "Racing_2D";

    /// <summary>
    /// ゲームシーンに移行する（ボタンのOnClickに設定可能）
    /// </summary>
    public void StartGame()
    {
        SceneManager.LoadScene(NextSceneName);
    }

    /// <summary>
    /// 指定したシーン名へ移行する（別シーンへ遷移したい場合などに設定可能）
    /// </summary>
    /// <param name="sceneName">シーン名</param>
    public void LoadCustomScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// ゲームを終了する（ボタンのOnClickに設定可能）
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
