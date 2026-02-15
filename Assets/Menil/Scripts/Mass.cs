using UnityEngine;
using TMPro;

public class Mass : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text totalMassText;

    float totalMass;
    float totalThrust;
    float thrustToMassRatio;

    void OnEnable()
    {
        PartDrag.OnAssemblyChanged += Recalculate;
    }

    void OnDisable()
    {
        PartDrag.OnAssemblyChanged -= Recalculate;
    }

    public void Recalculate()
    {
        totalMass = 0f;
        totalThrust = 0f;

        foreach (PartDrag part in PartDrag.assemblyParts)
        {
            totalMass += part.partWeight;
            totalThrust += part.partThrust;
        }

        thrustToMassRatio = totalMass > 0 ? (totalThrust / totalMass) / 9.8f : 0f;

        UpdateUI();
    }

    void UpdateUI()
    {
        if (totalMassText)
            totalMassText.text =
                $"MASS: {totalMass:F1} KG\n" +
                $"THRUST/WEIGHT: {thrustToMassRatio:F2}";
    }
}
