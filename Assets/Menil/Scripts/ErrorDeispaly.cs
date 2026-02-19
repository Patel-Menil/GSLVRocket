using UnityEngine;

public class ErrorDeispaly : MonoBehaviour
{
    [Header("Error Screens")]
    public GameObject errorScreen1;
    public GameObject errorScreen2;

    void OnEnable()
    {
        UpdateErrorUI();
    }

    public void UpdateErrorUI()
    {
        // turn everything OFF first
        if (errorScreen1) errorScreen1.SetActive(false);
        if (errorScreen2) errorScreen2.SetActive(false);

        // 🔥 read result from PartDrag
        int result = PartDrag.resultNumber;

        if (result == 1)
        {
            if (errorScreen1) errorScreen1.SetActive(true);
        }
        else if (result == 2)
        {
            if (errorScreen2) errorScreen2.SetActive(true);
        }
    }

    
}
