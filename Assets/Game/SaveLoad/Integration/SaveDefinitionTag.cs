using UnityEngine;

// Authored on allowed prefab roots; clones inherit a definition ID, never an instance SaveId.
[DisallowMultipleComponent]
public sealed class SaveDefinitionTag : MonoBehaviour
{
    [SerializeField] string m_DefinitionId;
    public string DefinitionId => m_DefinitionId;
#if UNITY_EDITOR
    public void SetAuthoringId(string id) { m_DefinitionId = id; }
#endif
}
