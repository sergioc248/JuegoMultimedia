using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScalePowerupController : MonoBehaviour
{
    [Header("Scale Powerup Settings")]
    public string requiredItemTag = "ScaleUpItem";      // The collectible tag
    public KeyCode activationKey = KeyCode.E;            // Key to trigger powerup
    public Vector3 scaledSize = new Vector3(2f, 2f, 2f);
    public float powerupDuration = 5f;
    public float slowMotionFactor = 0.5f;
    public float destroyAnimTime = 0.75f;

    [Header("References")]
    [Tooltip("Drag in the GameObject that has your CollectibleManagerScript on it.")]
    public GameObject collectibleManagerObject;
    private CollectibleManagerScript inventory;
    private bool isReady = false;

    private Transform player;
    private PlayerController playerController;

    void Start()
    {
        if (collectibleManagerObject != null)
            inventory = collectibleManagerObject.GetComponent<CollectibleManagerScript>();
        if (inventory == null)
            Debug.LogError("ScalePowerupController: No CollectibleManagerScript found.");

        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
                Debug.LogError("ScalePowerupController: No PlayerController on Player.");
        }
    }

    void Update()
    {
        if (!isReady)
        {
            if (inventory != null && inventory.SpecialInventoryContains(requiredItemTag))
                isReady = true;
            else
                return;
        }

        if (Input.GetKeyDown(activationKey))
        {
            // consume item
            inventory.RemoveSpecialItem(requiredItemTag);
            isReady = false;

            StartCoroutine(ActivateScalePowerup());
        }
    }

    private IEnumerator ActivateScalePowerup()
    {
        // Cache original state
        Vector3 originalScale = player.localScale;
        float origWalk = playerController.walkSpeed;
        float origSprint = playerController.sprintSpeed;

        // Apply effects
        player.localScale = scaledSize;
        playerController.walkSpeed = origWalk * slowMotionFactor;
        playerController.sprintSpeed = origSprint * slowMotionFactor;
        playerController.SetInvulnerable(true);

        // Destroy and animate destructibles
        var destructibles = GameObject.FindGameObjectsWithTag("Destructible");
        foreach (var obj in destructibles)
            StartCoroutine(DestroyAnim(obj));

        // Duration
        yield return new WaitForSeconds(powerupDuration);

        // Restore
        player.localScale = originalScale;
        playerController.walkSpeed = origWalk;
        playerController.sprintSpeed = origSprint;
        playerController.SetInvulnerable(false);
    }

    private IEnumerator DestroyAnim(GameObject obj)
    {
        Vector3 dir = (obj.transform.position - player.position).normalized;
        Quaternion startRot = obj.transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(90f, 0f, 0f);
        float elapsed = 0f;

        while (elapsed < destroyAnimTime)
        {
            float t = elapsed / destroyAnimTime;
            // smoothly rotate and move away
            obj.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            obj.transform.position += dir * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(obj);
    }
}