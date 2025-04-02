public interface IInteractable
{
    bool CanInteract();
    void OnHoverEnter();
    void OnHoverExit();
    void OnInteractStart();
    void OnInteractHold(float duration);
    void OnInteractEnd(bool completed);
}
