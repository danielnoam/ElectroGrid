using System;

public interface IMenuScreen
{
    void Show(bool animated = true, Action onComplete = null);
    void Hide(bool animated = true, Action onComplete = null);
    void SetInteractable(bool interactable);
}