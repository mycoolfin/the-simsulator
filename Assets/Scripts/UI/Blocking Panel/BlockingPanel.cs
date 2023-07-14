using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class BlockingPanel : MonoBehaviour
{
    private VisualElement panel;

    private void OnApplicationFocus()
    {
        // Freeze UI briefly on focus switch to prevent accidental click throughs.
        Block();
    }

    private IEnumerator BlockCoroutine()
    {
        if (panel == null)
            yield break;

        panel.style.display = DisplayStyle.Flex;
        yield return new WaitForSeconds(0.2f);
        panel.style.display = DisplayStyle.None;
    }

    public void Block()
    {
        StartCoroutine(BlockCoroutine());
    }

    public void SetPanel(VisualElement panelElement)
    {
        panel = panelElement;
        if (panel != null)
            panel.style.display = DisplayStyle.None;
    }
}
