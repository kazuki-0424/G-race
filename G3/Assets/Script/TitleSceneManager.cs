using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneManager : MonoBehaviour
{
    [Header("移行先シーン名")]
    [SerializeField] private string NextSceneName = "Racing_2D";

    void Update()
    {
        // 任意のキーでシーン移行
        if (Input.anyKeyDown)
        {
            SceneManager.LoadScene(NextSceneName);
        }
    }
}
