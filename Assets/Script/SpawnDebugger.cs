using UnityEngine;

public class SpawnDebugger : MonoBehaviour {
    void Awake() {
        Debug.Log(
            $"SPAWNED: {gameObject.name}\n{System.Environment.StackTrace}"
        );
    }
}
    