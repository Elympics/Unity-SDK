using UnityEngine.UIElements;

#nullable enable

namespace Elympics.Editor
{
    internal static class VisualElementExtensions
    {
        public static void SetVisible(this VisualElement element, bool visible) => element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
