public interface IInteractable
{
    void Interact(UnityEngine.GameObject actor);
    string Hint { get; }
}