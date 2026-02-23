using System.Collections.Generic;
using UnityEngine;

public class LevelPart : MonoBehaviour
{
    [Header("Intersection check")]
    [SerializeField] private LayerMask intersectionLayer;
    [SerializeField] private Collider[] intersectionCheckColliders;
    [SerializeField] private Transform intersectionCheckParent;


    [ContextMenu("Set static to envoirment layer")]
    private void AdjustLayerForStaticObjcets()
    {
        foreach (Transform childTransorm in transform.GetComponentsInChildren<Transform>(true))
        {
            if (childTransorm.gameObject.isStatic)
            {
                childTransorm.gameObject.layer = LayerMask.NameToLayer("Environment");
            }
        }
    }

    private void Start()
    {
        if (intersectionCheckColliders.Length <= 0)
        {
            intersectionCheckColliders = intersectionCheckParent.GetComponentsInChildren<Collider>();
        }
    }

    public bool IntersectionDetected()
    {
        Physics.SyncTransforms();

        foreach (var collider in intersectionCheckColliders)
        {
            Collider[] hitColliders =
Physics.OverlapBox(collider.bounds.center, collider.bounds.extents, Quaternion.identity, intersectionLayer);

            foreach (var hit in hitColliders)
            {
                InteresectionCheck interesectionCheck = hit.GetComponentInParent<InteresectionCheck>();

                if (interesectionCheck != null && intersectionCheckParent != interesectionCheck.transform)
                    return true;
            }

        }

        return false;
        
    }


    public void SnapAndAlignPartTo(SnapPoint targetSnapPoint)
    {
        SnapPoint entrancePoint = GetEntrancePoint();

        AlignTo(entrancePoint, targetSnapPoint); // IMPROTANT: Alignment should be before position snapping
        SnapTo(entrancePoint, targetSnapPoint);
    }

    private void AlignTo(SnapPoint ownSnapPoint, SnapPoint targetSnapPoint)
    {

        var rotationOffset =
            ownSnapPoint.transform.rotation.eulerAngles.y - transform.rotation.eulerAngles.y;


        transform.rotation = targetSnapPoint.transform.rotation;


        transform.Rotate(0, 180, 0);


        transform.Rotate(0, -rotationOffset, 0);
    }

    private void SnapTo(SnapPoint ownSnapPoint, SnapPoint targetSnapPoint)
    {

        var offset = transform.position - ownSnapPoint.transform.position;


        var newPosition = targetSnapPoint.transform.position + offset;


        transform.position = newPosition;
    }



    public SnapPoint GetEntrancePoint() => GetSnapPointOfType(SnapPointType.Enter);
    public SnapPoint GetExitPoint() => GetSnapPointOfType(SnapPointType.Exit);

    private SnapPoint GetSnapPointOfType(SnapPointType pointType)
    {
        SnapPoint[] snapPoints = GetComponentsInChildren<SnapPoint>();
        List<SnapPoint> filteredSnapPoints = new List<SnapPoint>();


        foreach (SnapPoint snapPoint in snapPoints)
        {
            if (snapPoint.pointType == pointType)
                filteredSnapPoints.Add(snapPoint);
        }


        if (filteredSnapPoints.Count > 0)
        {
            int randomIndex = Random.Range(0, filteredSnapPoints.Count);
            return filteredSnapPoints[randomIndex];
        }

        return null;
    }

    public Enemy[] MyEnemies() => GetComponentsInChildren<Enemy>(true);
}
