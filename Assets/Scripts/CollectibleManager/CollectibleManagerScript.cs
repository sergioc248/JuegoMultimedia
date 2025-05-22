using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class CollectibleManagerScript : MonoBehaviour
{
    [Header("SecuenciaMovimiento")]
    public float amplitud = 0.25f;
    public float floatSpeed = 2f;
    public float rotationSpeed = 45;

    [Header("SistemaRecoleccion")]
    public TextMeshProUGUI itemCounter;
    public string collectibleTag = "Collectible";
    private int totalItemsScene = 0;
    private int itemsCollected = 0;

    [Header("Lista de objetos")]
    public List<Transform> collectibles = new List<Transform>();
    private Dictionary<Transform, Vector3> startPosition = new Dictionary<Transform, Vector3>();

    [Header("Special Collectibles")]
    public List<Transform> specialCollectibles = new List<Transform>();
    private Dictionary<Transform, Vector3> specialStartPosition = new Dictionary<Transform, Vector3>();
    private List<string> specialInventory = new List<string>();

    [System.Serializable]
    public class SpecialItemSlot
    {
        [Tooltip("The exact tag that your special‐pickup prefab carries.")]
        public string itemTag;

        [Tooltip("The UI Image you want to enable/disable when that tag is in inventory.")]
        public Image uiImage;
    }
    [Header("Special UI Slots")]
    public List<SpecialItemSlot> specialItemSlots = new List<SpecialItemSlot>();

    // --- SPECIAL ITEM TAGS ---
    [Header("Slow-Time Controller")]
    [Tooltip("Give the exact tag name that your 'clock' pickup uses.")]
    public string slowTimeTag = "ClockSlowTime";
    public TimeController timeController;

    [Header("ScaleUp Controller")]
    public string scaleUpTag = "ScalePowerUp";
    public ScalePowerupController scaleController;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Gather ANY GameObject in the scene tagged "Collectible"
        collectibles = GameObject.FindGameObjectsWithTag(collectibleTag)
                                  .Select(go => go.transform)
                                  .ToList();

        totalItemsScene = collectibles.Count;

        // Disable powerup controllers until we pick them up:
        timeController.enabled = false;
        scaleController.enabled = false;

        // Make sure inventory/UI starts empty:
        specialInventory.Clear();

        // ----- Normal collectibles setup -----
        foreach (var obj in collectibles)
        {
            if (obj == null) continue;

            // 1) Remember its original position
            startPosition[obj] = obj.position;

            // 2) Ensure it has a collider (and make it a trigger)
            Collider col = obj.GetComponent<Collider>();
            if (col == null)
                col = obj.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;

            // 3) Ensure exactly one CollectibleDetectorScript, and initialize it
            var detector = obj.GetComponent<CollectibleDetectorScript>();
            if (detector == null)
                detector = obj.gameObject.AddComponent<CollectibleDetectorScript>();
            detector.Init(this);
        }

        // ----- Special collectibles setup -----
        foreach (var obj in specialCollectibles)
        {
            if (obj == null) continue;

            // 1) Remember its original position
            specialStartPosition[obj] = obj.position;

            // 2) Ensure it has a collider (and make it a trigger)
            Collider col = obj.GetComponent<Collider>();
            if (col == null)
                col = obj.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;

            // 3) Ensure exactly one CollectibleDetectorScript, and initialize it
            var detector = obj.GetComponent<CollectibleDetectorScript>();
            if (detector == null)
                detector = obj.gameObject.AddComponent<CollectibleDetectorScript>();
            detector.Init(this);
        }

        UpdateCounterUI();
        UpdateSpecialUI();
    }

    // Update is called once per frame
    void Update()
    {
        // Animate normal collectibles
        foreach (var obj in collectibles)
        {
            if (obj == null) continue;
            // Try to get its start position; if missing, register it now
            if (!startPosition.TryGetValue(obj, out Vector3 startPos))
            {
                startPos = obj.position;
                startPosition[obj] = startPos;
            }

            float phase = Time.time * floatSpeed;
            float newY = startPos.y + Mathf.Sin(phase) * amplitud;
            obj.position = new Vector3(startPos.x, newY, startPos.z);
            obj.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }


        // Animate special collectibles
        foreach (var obj in specialCollectibles)
        {
            if (obj == null) continue;
            if (!specialStartPosition.TryGetValue(obj, out Vector3 startPos))
            {
                startPos = obj.position;
                specialStartPosition[obj] = startPos;
            }

            float phase = Time.time * floatSpeed;
            float newY = startPos.y + Mathf.Sin(phase) * amplitud;

            obj.position = new Vector3(startPos.x, newY, startPos.z);
            obj.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    public void CollectItem(Transform obj)
    {
        // Normal collectible
        if (collectibles.Contains(obj))
        {
            collectibles.Remove(obj);
            itemsCollected++;
            UpdateCounterUI();
            Destroy(obj.gameObject);
        }
        // Special collectible
        else if (specialCollectibles.Contains(obj))
        {
            specialCollectibles.Remove(obj);

            specialInventory.Add(obj.tag);
            UpdateSpecialUI();
            Destroy(obj.gameObject);

            // LIST OF SPECIAL OBJECT TAGS
            // SLOW-TIME Tag
            if (obj.CompareTag(slowTimeTag))
            {
                timeController.enabled = true;
            }
            // SCALE POWERUP Tag
            if (obj.CompareTag(scaleUpTag)) scaleController.enabled = true;
        }
    }

    void UpdateCounterUI()
    {
        if (itemCounter != null)
        {
            itemCounter.text = $"{itemsCollected} / {totalItemsScene}";
        }
    }

    void UpdateSpecialUI()
    {
        // Turn each slot image on/off based on whether its itemTag is in inventory
        foreach (var slot in specialItemSlots)
        {
            if (slot.uiImage == null) continue;
            bool hasIt = specialInventory.Contains(slot.itemTag);
            slot.uiImage.enabled = hasIt;
        }
    }

    // Removes the named special item (if present), updates its UI, and returns true if removed.
    public bool RemoveSpecialItem(string itemTag)
    {
        if (specialInventory.Remove(itemTag))
        {
            UpdateSpecialUI();
            return true;
        }
        return false;
    }
    // Expose a quick check for inventory polling.
    public bool SpecialInventoryContains(string itemTag) => specialInventory.Contains(itemTag);
}
