using UnityEngine;

public interface IProjectionInputReceiver
{
    void ApplyProjectionInput(Mesh mesh, float rpm, float helicoidSize);
}
