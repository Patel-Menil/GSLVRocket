using System.Collections;
using UnityEngine;

public class Reset : MonoBehaviour{
    public void GameQuit() {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;

        #elif UNITY_WEBGL
            Application.OpenURL("about:blank"); 

        #else
            Application.Quit();
        #endif
    }

}