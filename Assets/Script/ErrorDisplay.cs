using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ErrorDisplay : MonoBehaviour
{
    public TextMeshProUGUI TextBox;

    public void SetText(string text) {
        TextBox.text = text;
    }
}
