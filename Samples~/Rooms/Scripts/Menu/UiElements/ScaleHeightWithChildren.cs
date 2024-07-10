using UnityEngine;
using UnityEngine.UI;

public class ScaleHeightWithChildren : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private VerticalLayoutGroup layoutGroup;

    private void OnTransformChildrenChanged()
    {
        UpdateScale();
    }

    private void Awake()
    {
        UpdateScale();
    }

    private void UpdateScale()
    {
        float newSize = layoutGroup.padding.top + layoutGroup.padding.bottom;
        if (content.childCount > 0)
        {
            newSize += content.childCount * content.GetChild(0).GetComponent<RectTransform>().rect.height;
            newSize += (content.childCount - 1) * layoutGroup.spacing;
        }

        content.sizeDelta = new Vector2(content.sizeDelta.x, newSize);
    }
}
