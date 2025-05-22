using UnityEngine;

public class CollectibleDetectorScript : MonoBehaviour
{
    private CollectibleManagerScript manager;
    public void Init(CollectibleManagerScript manager)
    {
        this.manager = manager;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            manager.CollectItem(transform);
        }
    }

}
