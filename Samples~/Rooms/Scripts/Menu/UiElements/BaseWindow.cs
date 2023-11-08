using UnityEngine;
using JetBrains.Annotations;

public class BaseWindow : MonoBehaviour
{
    [UsedImplicitly]
    public virtual void Show() => gameObject.SetActive(true);

    [UsedImplicitly]
    public virtual void Hide() => gameObject.SetActive(false);
}
