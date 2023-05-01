using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class BlockingPanel : MonoBehaviour
{
    private VisualElement panel;

    private void Awake()
    {
        panel = GetComponent<UIDocument>().rootVisualElement;
        panel.style.display = DisplayStyle.None;
    }

    private void OnApplicationFocus()
    {
        // Freeze UI briefly on focus switch to prevent accidental click throughs.
        StartCoroutine(Block());
    }

    private IEnumerator Block()
    {
        panel.style.display = DisplayStyle.Flex;
        yield return new WaitForSeconds(0.2f);
        panel.style.display = DisplayStyle.None;
    }
}
