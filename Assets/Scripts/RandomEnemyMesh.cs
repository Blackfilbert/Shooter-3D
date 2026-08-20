using UnityEngine;

public class RandomEnemyMesh : MonoBehaviour
{
    [SerializeField] private GameObject[] meshes;

    private GameObject _activeMesh;

    public GameObject ActiveMesh => _activeMesh;

    private void Awake()
    {
        EnsureSelected();
    }

    public void EnsureSelected()
    {
        if (_activeMesh != null)
            return;

        if (meshes == null || meshes.Length <= 0)
            return;

        _activeMesh = GetRandomMesh();

        if (_activeMesh == null)
            return;

        for (int i = 0; i < meshes.Length; i++)
        {
            if (meshes[i] != null)
                meshes[i].SetActive(meshes[i] == _activeMesh);
        }
    }

    public Animator GetActiveAnimator()
    {
        EnsureSelected();

        return _activeMesh != null ? _activeMesh.GetComponentInChildren<Animator>() : null;
    }

    private GameObject GetRandomMesh()
    {
        int count = 0;

        for (int i = 0; i < meshes.Length; i++)
        {
            if (meshes[i] != null)
                count++;
        }

        if (count <= 0)
            return null;

        int selectedIndex = Random.Range(0, count);

        for (int i = 0; i < meshes.Length; i++)
        {
            if (meshes[i] == null)
                continue;

            if (selectedIndex == 0)
                return meshes[i];

            selectedIndex--;
        }

        return null;
    }
}
