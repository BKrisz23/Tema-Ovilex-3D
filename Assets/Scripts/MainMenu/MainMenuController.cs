using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Controlează schimbarea scenei
public class MainMenuController : MonoBehaviour
{
    readonly string GAMEPLAY_SCENE = "Car";

    public void LoadGameplayScene(){
        StartCoroutine(loadSceneAsync(GAMEPLAY_SCENE));
    }

    IEnumerator loadSceneAsync(string sceneName){
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
