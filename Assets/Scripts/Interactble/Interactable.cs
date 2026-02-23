using UnityEngine;

public class Interactable : MonoBehaviour
{
    protected Player_WeaponController weaponController;
    [SerializeField] protected MeshRenderer mesh;

    [HideInInspector] public bool spawnedByDropDirector;

    [Header("Physics")]
    [SerializeField] private bool blockPhysicsPush = true;
    [SerializeField, Range(0f, 1f)] private float physicsPushMultiplier = 1f;

    [SerializeField] private Material highlightMaterial;
    [SerializeField] protected Material defaultMaterial;

    private void Awake()
    {
        Collider c = GetComponent<Collider>();
        if (c != null)
            c.isTrigger = true;
    }

    private void Start()
    {
        if (mesh == null)
            mesh = GetComponentInChildren<MeshRenderer>();

        defaultMaterial = mesh.sharedMaterial;
    }

    protected void UpdateMeshAndMaterial(MeshRenderer newMesh)
    {
        mesh = newMesh;
        defaultMaterial = newMesh.sharedMaterial;
    }

    public virtual bool BlockPhysicsPush() => blockPhysicsPush;

    public virtual float GetPhysicsPushMultiplier()
    {
        if (blockPhysicsPush)
            return 0f;

        return Mathf.Clamp01(physicsPushMultiplier);
    }

    public virtual void Interaction()
    {
        Debug.Log("Interacted with " + gameObject.name);
    }

    protected void WakeNearbyRagdolls(float radius = 2f)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            Ragdoll ragdoll = hits[i].GetComponentInParent<Ragdoll>();
            if (ragdoll != null)
                ragdoll.WakeUp();
        }
    }

    public void HighlightActive(bool active)
    {
        if (active)
            mesh.material = highlightMaterial;
        else
            mesh.material = defaultMaterial;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (weaponController == null)
            weaponController = other.GetComponent<Player_WeaponController>();

        Player_Interaction playerInteraction = other.GetComponent<Player_Interaction>();

        if (playerInteraction == null)
            return;

        playerInteraction.GetInteracbles().Add(this);
        playerInteraction.UpdateClosestInteractble();
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        Player_Interaction playerInteraction = other.GetComponent<Player_Interaction>();

        if (playerInteraction == null)
            return;

        playerInteraction.GetInteracbles().Remove(this);
        playerInteraction.UpdateClosestInteractble();
    }

    private void OnValidate()
    {
        Collider c = GetComponent<Collider>();
        if (c != null)
            c.isTrigger = true;
    }
}