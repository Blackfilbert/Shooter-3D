using UnityEngine;

public class PlayerAmmoView : MonoBehaviour
{
    private void OnEnable()
    {
        Clear();
        gameObject.SetActive(false);
    }

    private void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }
}
