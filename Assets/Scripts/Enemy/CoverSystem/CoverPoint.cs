using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoverPoint : MonoBehaviour
{
    public bool occupied;

    [Header("Visibility")]
    [SerializeField] private bool hidePointInGame = true;

    public void SetOccupied(bool occupied) => this.occupied = occupied;

    private void Awake()
    {
        if (!hidePointInGame)
            return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = false;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }
}