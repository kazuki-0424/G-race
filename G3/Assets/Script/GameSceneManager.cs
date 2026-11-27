using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    [SerializeField] private string NextSceneName = "Title";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void BackTitle()
    {
        SceneManager.LoadScene(NextSceneName);
    }

   
}
