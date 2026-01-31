using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sniper_MarkChainShotAbility : MonoBehaviour
{
    [Header("Mark")]
    [SerializeField] private KeyCode markKey = KeyCode.T;
    [SerializeField] private int maxMarks = 3;
    [SerializeField] private float markMaxDistance = 30f;
    [SerializeField] private LayerMask markRaycastMask = ~0;

    [Header("Chain")]
    [SerializeField] private bool clearMarksAfterChainShot = true;

    [Header("Visual")]
    [SerializeField] private float chainBulletSpeed = 55f;
    [SerializeField] private float markHeightOffset = 1.9f;

    private Player player;
    private Player_WeaponController weapon;

    private readonly List<Enemy> markedTargets = new List<Enemy>(3);
    private readonly Dictionary<Enemy, Sniper_MarkIndicator> indicators = new Dictionary<Enemy, Sniper_MarkIndicator>();

    private bool isSniperEquipped;

    private void Awake()
    {
        player = GetComponent<Player>();
        weapon = GetComponent<Player_WeaponController>();
    }

    private void Update()
    {
        CleanupDeadOrMissingTargets();

        if (!isSniperEquipped)
            return;

        if (player != null && player.health != null && player.health.isDead)
            return;

        if (player != null && player.controlsEnabled == false)
            return;

        if (Input.GetKeyDown(markKey))
            TryToggleMark();
    }

    public void OnEquippedWeaponChanged(WeaponType weaponType)
    {
        isSniperEquipped = weaponType == WeaponType.Rifle;

        if (!isSniperEquipped)
            ClearAllMarks();
    }

    private void TryToggleMark()
    {
        if (weapon == null)
            return;

        Transform gunPoint = weapon.GunPoint();
        if (gunPoint == null)
            return;

        Vector3 dir = weapon.BulletDirection();

        if (!Physics.Raycast(gunPoint.position, dir, out RaycastHit hit, markMaxDistance, markRaycastMask, QueryTriggerInteraction.Ignore))
            return;

        Enemy enemy = hit.collider != null ? hit.collider.GetComponentInParent<Enemy>() : null;
        if (enemy == null)
            enemy = hit.transform != null ? hit.transform.GetComponentInParent<Enemy>() : null;

        if (enemy == null)
            return;

        if (enemy.IsDead)
            return;

        if (markedTargets.Contains(enemy))
        {
            Unmark(enemy);
            return;
        }

        if (markedTargets.Count >= Mathf.Max(1, maxMarks))
        {
            Enemy toRemove = markedTargets[0];
            Unmark(toRemove);
        }

        markedTargets.Add(enemy);
        CreateOrRefreshIndicator(enemy);
    }

    private void CreateOrRefreshIndicator(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (!indicators.TryGetValue(enemy, out Sniper_MarkIndicator indicator) || indicator == null)
        {
            GameObject go = new GameObject("Sniper_MarkIndicator");
            go.transform.SetParent(enemy.transform, false);

            indicator = go.AddComponent<Sniper_MarkIndicator>();
            indicator.Setup(markHeightOffset);
            indicators[enemy] = indicator;
        }

        indicator.SetIndex(markedTargets.IndexOf(enemy) + 1);
    }

    private void RefreshAllIndicatorIndices()
    {
        for (int i = 0; i < markedTargets.Count; i++)
        {
            Enemy e = markedTargets[i];
            if (e == null) continue;
            if (indicators.TryGetValue(e, out Sniper_MarkIndicator ind) && ind != null)
                ind.SetIndex(i + 1);
        }
    }

    private void Unmark(Enemy enemy)
    {
        if (enemy == null)
            return;

        markedTargets.Remove(enemy);

        if (indicators.TryGetValue(enemy, out Sniper_MarkIndicator ind) && ind != null)
            Destroy(ind.gameObject);

        indicators.Remove(enemy);
        RefreshAllIndicatorIndices();
    }

    private void ClearAllMarks()
    {
        for (int i = markedTargets.Count - 1; i >= 0; i--)
        {
            Enemy e = markedTargets[i];
            if (e == null) continue;
            if (indicators.TryGetValue(e, out Sniper_MarkIndicator ind) && ind != null)
                Destroy(ind.gameObject);
        }

        markedTargets.Clear();
        indicators.Clear();
    }

    private void CleanupDeadOrMissingTargets()
    {
        if (markedTargets.Count == 0)
            return;

        bool changed = false;

        for (int i = markedTargets.Count - 1; i >= 0; i--)
        {
            Enemy e = markedTargets[i];

            if (e == null || e.IsDead)
            {
                if (e != null && indicators.TryGetValue(e, out Sniper_MarkIndicator ind) && ind != null)
                    Destroy(ind.gameObject);

                if (e != null)
                    indicators.Remove(e);

                markedTargets.RemoveAt(i);
                changed = true;
            }
        }

        if (changed)
            RefreshAllIndicatorIndices();
    }

    private Vector3 TargetPoint(Enemy e)
    {
        if (e == null)
            return Vector3.zero;

        Collider col = e.GetComponentInChildren<Collider>();
        if (col != null)
            return col.bounds.center;

        return e.transform.position + Vector3.up * markHeightOffset;
    }

    public bool TryFireChainShot(
        Vector3 gunPosition,
        Vector3 direction,
        float maxDistance,
        int baseDamage,
        float fallbackVisualSpeed
    )
    {
        CleanupDeadOrMissingTargets();

        if (!isSniperEquipped)
            return false;

        if (markedTargets.Count == 0)
            return false;

        if (!Physics.Raycast(gunPosition, direction, out RaycastHit hit, maxDistance, markRaycastMask, QueryTriggerInteraction.Ignore))
            return false;

        Enemy hitEnemy = hit.collider != null ? hit.collider.GetComponentInParent<Enemy>() : null;
        if (hitEnemy == null)
            hitEnemy = hit.transform != null ? hit.transform.GetComponentInParent<Enemy>() : null;

        if (hitEnemy == null)
            return false;

        if (hitEnemy.IsDead)
            return false;

        if (!markedTargets.Contains(hitEnemy))
            return false;

        int dmg = Mathf.Max(1, baseDamage);

        HitBox hb = null;
        if (hit.collider != null)
        {
            hb = hit.collider.GetComponent<HitBox>();
            if (hb == null)
                hb = hit.collider.GetComponentInParent<HitBox>();
        }

        if (hb != null)
        {
            dmg = Mathf.RoundToInt(dmg * hb.DamageMultiplier);
            dmg = Mathf.Max(1, dmg);
        }

        hitEnemy.GetHit(dmg);

        for (int i = 0; i < markedTargets.Count; i++)
        {
            Enemy e = markedTargets[i];
            if (e == null) continue;
            if (e.IsDead) continue;
            if (e == hitEnemy) continue;

            e.GetHit(dmg);
        }

        float speed = chainBulletSpeed > 0 ? chainBulletSpeed : Mathf.Max(5f, fallbackVisualSpeed);
        StartCoroutine(PlayChainVisual(gunPosition, hitEnemy, speed));

        if (clearMarksAfterChainShot)
            ClearAllMarks();

        return true;
    }

    private IEnumerator PlayChainVisual(Vector3 gunPosition, Enemy firstHit, float speed)
    {
        if (Camera.main == null)
            yield break;

        List<Vector3> path = new List<Vector3>(4);
        path.Add(gunPosition);

        if (firstHit != null)
            path.Add(TargetPoint(firstHit));

        for (int i = 0; i < markedTargets.Count; i++)
        {
            Enemy e = markedTargets[i];
            if (e == null) continue;
            if (e == firstHit) continue;
            path.Add(TargetPoint(e));
        }

        if (path.Count < 2)
            yield break;

        GameObject go = new GameObject("Sniper_ChainBullet");
        Sniper_ChainBulletVisual vis = go.AddComponent<Sniper_ChainBulletVisual>();
        vis.Play(path, speed);

        yield return null;
    }
}
