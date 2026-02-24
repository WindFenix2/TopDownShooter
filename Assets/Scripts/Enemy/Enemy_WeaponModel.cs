using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_WeaponModel : MonoBehaviour
{
    
    public Enemy_MeleeWeaponType weaponType;
    public AnimatorOverrideController overrideController;
    public Enemy_MeleeWeaponData weaponData;

    [SerializeField] private GameObject[] trailEffects;

    [Header("Damage atributes")]
    public Transform[] damagePoints;
    public float attackRadius;

    [ContextMenu("Assign damage point transforms")]
    private void GetDamagePoints()
    {
        damagePoints = new Transform[trailEffects.Length];
        for (int i = 0; i < trailEffects.Length; i++)
        {
            damagePoints[i] = trailEffects[i].transform;
        }
    }

    [Header("Trail color")]
    [SerializeField] private Color trailColor = new Color(1f, 0.1f, 0.1f, 1f);

    private MaterialPropertyBlock trailMPB;

    public void EnableTrailEffect(bool enable)
    {
        if (trailMPB == null)
            trailMPB = new MaterialPropertyBlock();

        foreach (var effect in trailEffects)
        {
            effect.SetActive(enable);

            if (enable)
            {
                TrailRenderer trail = effect.GetComponent<TrailRenderer>();
                if (trail != null)
                {
                    trail.startColor = trailColor;
                    trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);

                    Color hdrColor = trailColor * 2f;
                    hdrColor.a = 1f;

                    trail.GetPropertyBlock(trailMPB);
                    trailMPB.SetColor("_EmissionColor", hdrColor);
                    trailMPB.SetColor("_BaseColor", trailColor);
                    trailMPB.SetColor("_Color", trailColor);
                    trail.SetPropertyBlock(trailMPB);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if(damagePoints.Length > 0)
        {
            foreach(Transform point in damagePoints)
            {
                Gizmos.DrawWireSphere(point.position, attackRadius);
            }
        }
    }
}
