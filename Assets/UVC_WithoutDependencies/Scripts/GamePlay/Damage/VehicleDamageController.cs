using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace PG
{
    /// <summary>
    /// Car damage controller. Visual and technical effects of damage.
    /// </summary>
    [RequireComponent (typeof (VehicleController))]
    public class VehicleDamageController :MonoBehaviour
    {
#pragma warning disable 0649

        [Range(0, 5)]
        [SerializeField] float DamageFactor = 1;
        [SerializeField] float MaxCollisionMagnitude = 50;
        [SerializeField] float MaxImpulseMagnitude = 35;
        [SerializeField] float MaxDeformRadiusInMaxMag = 3;         //Radius of influence on the vertices (At maximum magnitude).
        [SerializeField] float DeformMultiplier = 0.3f;             //Radius of influence on the vertices (At maximum magnitude).
        [SerializeField] int MaxContactPoints = 2;                  //The maximum number of contacts per collision.
        [SerializeField] float MinTimeBetweenCollisions = 0.1f;     //The minimum interval between collisions.
        [SerializeField] AnimationCurve DamageDistanceCurve;        //To calculate the intensity of damage (In order to use sqrMagnitude for optimization).

        [SerializeField] bool UseNoise = true;
        [SerializeField] int NoiseSize = 10;
        [SerializeField] bool CalculateNormals = true;

        //The search for meshes and colliders is automatic; lists are needed for exceptions. For example a wheel renderer or a wheel collider.
        public List<MeshFilter> IgnoreDeformMeshes = new List<MeshFilter>();
        public List<MeshCollider> IgnoreDeformColliders = new List<MeshCollider>();
        public List<Transform> IgnoreFindInChildsMeshesAndColliders = new List<Transform>();

        [SerializeField] bool EnableLogAndGizmo;                     //Сollision log and gizmo for debug.

#pragma warning restore 0649

        Transform TR;
        Rigidbody RB;

        DamageMeshData[] DamageableMeshes;

        DetachableObject[] DetachableObjects;
        DamageableObjectData[] DamageableObjects;
        MoveableDO[] MoveObjects;

        float LastHitTime;
        Vector3 LocalCenterPoint;
        Vector3 CenterPoint { get { return TR.TransformPoint (LocalCenterPoint); } }

        public event System.Action<DamageData> OnDamageAction;

        void Start ()
        {
            TR = transform;
            RB = GetComponent<Rigidbody> ();
            DamageMeshData meshData;

            var damageableMeshes = new List<DamageMeshData>();

            var deformFilters = GetComponentsInChildren<MeshFilter> ().Where (m => 
                !IgnoreDeformMeshes.Contains (m) && 
                m.mesh.isReadable &&
                IgnoreFindInChildsMeshesAndColliders.All(p => !m.transform.CheckParent(p))
            );

            //Set up mesh data.
            foreach (var filter in deformFilters)
            {
                meshData = new DamageMeshData ();
                meshData.Transform = filter.transform;
                meshData.Mesh = filter.mesh;

                var collider = filter.GetComponent<MeshCollider>();
                if (collider != null && !IgnoreDeformColliders.Contains (collider))
                {
                    collider.sharedMesh = meshData.Mesh;
                    meshData.MeshCollider = collider;
                }

                meshData.Verts = meshData.Mesh.vertices;
                meshData.Damaged = false;

                damageableMeshes.Add (meshData);
            }

            var deformColliders = GetComponentsInChildren<MeshCollider> ().Where (c => 
                !IgnoreDeformColliders.Contains (c) && 
                c.sharedMesh.isReadable &&
                IgnoreFindInChildsMeshesAndColliders.All(p => !c.transform.CheckParent(p)) &&
                !damageableMeshes.Any(m => m.MeshCollider == c)
            );

            //Set up mesh collider data.
            foreach(var collider in deformColliders)
            {
                meshData = new DamageMeshData ();
                meshData.MeshCollider = collider;
                meshData.Transform = collider.transform;
                meshData.Mesh = (Mesh)Instantiate (collider.sharedMesh);
                meshData.Verts = collider.sharedMesh.vertices;
                meshData.Damaged = false;
                damageableMeshes.Add (meshData);
            }

            DamageableMeshes = damageableMeshes.ToArray ();

            DetachableObjects = GetComponentsInChildren<DetachableObject> ();
            MoveObjects = GetComponentsInChildren<MoveableDO> (true);

            var damageableObjects = GetComponentsInChildren<DamageableObject> ();
            DamageableObjects = new DamageableObjectData[damageableObjects.Length];
            for (int i = 0; i < damageableObjects.Length; i++)
            {
                DamageableObjects[i] = new DamageableObjectData(damageableObjects[i]);
            }

            LocalCenterPoint = Vector3.zero;
            int vertsCount = 0;
            foreach (var damageableMesh in DamageableMeshes)
            {
                foreach (var vert in damageableMesh.Mesh.vertices)
                {
                    LocalCenterPoint += damageableMesh.Transform.localPosition + vert;
                    vertsCount++;
                }
            }

            LocalCenterPoint /= vertsCount;
        }

        public void OnCollisionEnter (Collision col)
        {
            OnCollision (col, false);
        }

        public void OnCollision (Collision col, bool fromStayCollistion)
        {
            if (Time.time - LastHitTime < MinTimeBetweenCollisions)
            {
                return;
            }

            ContactPoint contact;
            Vector3 force = Vector3.zero;
            Vector3 normal = Vector3.zero;
            Vector3 forceResult;
            Vector3 normalResult;
            float massFactor;
            Rigidbody rb;
            int contactsCount = Mathf.Min(col.contacts.Length, MaxContactPoints);

            float impuls = (col.impulse.magnitude * 0.001f).Clamp (0, MaxImpulseMagnitude);
            float relativeVelocityMagnitude = col.relativeVelocity.magnitude;

            rb = col.rigidbody;
            if (rb != null && !rb.isKinematic)
            {
                var percent = (rb.mass / RB.mass).Clamp();
                massFactor = percent <= 0.1f ? Mathf.Sqrt (percent) : 1;    //To prevent damage from small objects (eg cones).
            }
            else
            {
                massFactor = 1;
            }

            if (Mathf.Max(relativeVelocityMagnitude, fromStayCollistion? impuls: 0) * DamageFactor > 1)
            {
                if (fromStayCollistion && impuls > relativeVelocityMagnitude)
                {
                    force = Vector3.ClampMagnitude (col.impulse * 0.001f, MaxImpulseMagnitude);
                    normal = col.impulse.normalized;
                }
                
                for (int i = 0; i < contactsCount; i++)
                {
                    contact = col.contacts[i];

                    if (fromStayCollistion)
                    {
                        if (relativeVelocityMagnitude > impuls)
                        {
                            normal = contact.normal;
                            force = col.relativeVelocity;
                        }

                        if (fromStayCollistion && Vector3.Dot (force, contact.point - CenterPoint) > 0)
                        {
                            forceResult = -force;
                            normalResult = -normal;
                        }
                        else
                        {
                            forceResult = force;
                            normalResult = normal;
                        }
                    }
                    else
                    {
                        forceResult = col.relativeVelocity;
                        normalResult = contact.normal;
                    }

                    SetDamage (new DamageData ()
                    {
                        DamagePoint = contact.point,
                        DamageForce = forceResult,
                        SurfaceNormal = normalResult,
                        MassFactor = massFactor,
                    });

                    if (EnableLogAndGizmo)
                    {
                        CurrentGizmoIndex = MathExtentions.Repeat (CurrentGizmoIndex + 1, 0, GizmosData.Length - 1);
                        GizmosData[CurrentGizmoIndex].ContactPoint = contact.point;
                        GizmosData[CurrentGizmoIndex].Force = forceResult;
                        GizmosData[CurrentGizmoIndex].Normal = normalResult;
                    }
                }

                if (EnableLogAndGizmo)
                {
                    if (!fromStayCollistion || relativeVelocityMagnitude > impuls)
                    {
                        Debug.LogFormat ("relativeVelocity {0}", relativeVelocityMagnitude);
                    }
                    else
                    {
                        Debug.LogFormat ("impuls {0}", impuls);
                    }
                }

                FinalizeDamage ();
                LastHitTime = Time.time;
            }
        }

        public void OnCollisionStay (Collision collision)
        {
            OnCollision (collision, true);
        }

        /// <summary>
        /// Set damage to the meshes and all damaged objects.
        /// </summary>
        public void SetDamage (DamageData data)
        {
            Vector3 damagePoint = data.DamagePoint;
            Vector3 damageForce = data.DamageForce;
            Vector3 surfaceNormal = data.SurfaceNormal;

            //Declaring shared variables.
            DamageMeshData curDamageMesh;

            float sqrDist;
            float percent;

            Vector3 localDamagePoint;
            Vector3 localDamageForceAndSurfaceDot;

            //Сalculate all the necessary values.
            Vector3 clampForce = Vector3.ClampMagnitude(damageForce, MaxCollisionMagnitude);                                //Limiting force if force exceeds maximum.
            Vector3 normalizedForce = clampForce.normalized;
            float forceMagFactor = clampForce.magnitude * DamageFactor * data.MassFactor;                                   //Accept all existing factors.
            float maxDamageRadius = MaxDeformRadiusInMaxMag * (forceMagFactor / MaxCollisionMagnitude);
            float sqrMaxDamageRadius = Mathf.Pow(maxDamageRadius, 2);
            float maxDeformDist = DeformMultiplier * (forceMagFactor / MaxCollisionMagnitude);
            float sqrMaxDeformDist = Mathf.Pow (maxDeformDist, 2);                                                          //Calculation of the square of the maximum damage distance.
            float surfaceDot = Mathf.Clamp01 (Vector3.Dot (surfaceNormal, normalizedForce)) *
                (Vector3.Dot ((CenterPoint - damagePoint).normalized, normalizedForce) + 1) * 0.3f;                         //Calculation of surfaceDot to reduce the tangential damage force.
            float deformSurfaceDot = surfaceDot * 0.01f * DamageFactor              ;                                       //Applying all multipliers and decreasing by 100 surfaceDot for an adequate result.
            
            if (surfaceDot <= 0.02f)
            {
                return;
            }

            Bounds damageBounds = new Bounds(Vector3.zero, new Vector3(maxDamageRadius, maxDamageRadius, maxDamageRadius));

            //Damage to all meshes.
            for (int i = 0; i < DamageableMeshes.Length; i++)
            {
                curDamageMesh = DamageableMeshes[i];
                damageBounds.center = curDamageMesh.Transform.InverseTransformPoint(damagePoint);
                if (!curDamageMesh.Mesh.bounds.Intersects (damageBounds))
                {
                    continue;
                }

                localDamagePoint = curDamageMesh.Transform.InverseTransformPoint (damagePoint);
                localDamageForceAndSurfaceDot = curDamageMesh.Transform.InverseTransformDirection (clampForce) * deformSurfaceDot;

                for (int j = 0; j < DamageableMeshes[i].Verts.Length; j++)
                {
                    //The squares of lengths are used everywhere for optimization, 
                    //then the percentage of the distance from the square of the maximum distance is found. 
                    //The calculation is not accurate, but visually the damage looks good.
                    sqrDist = (DamageableMeshes[i].Verts[j] - localDamagePoint).sqrMagnitude;
                    if (sqrDist < sqrMaxDamageRadius)
                    {
                        percent = DeformMultiplier * DamageDistanceCurve.Evaluate (sqrDist / sqrMaxDamageRadius);

                        DamageableMeshes[i].Verts[j] += localDamageForceAndSurfaceDot * percent *
                            (UseNoise ? 0.5f + Mathf.PerlinNoise (DamageableMeshes[i].Verts[j].x * NoiseSize, DamageableMeshes[i].Verts[j].y * NoiseSize) : 1);

                        DamageableMeshes[i].Damaged = true;
                    }
                }
            }

            //Further, similar algorithms are applied to all similar objects.

            DamageableObject damageableObject;

            for (int i = 0; i < DamageableObjects.Length; i++)
            {
                damageableObject = DamageableObjects[i].DamageableObject;

                if (!damageableObject || damageableObject.IsDead)
                {
                    continue;
                }

                sqrDist = (damageableObject.transform.TransformPoint(damageableObject.LocalCenterPoint) - damagePoint).sqrMagnitude;

                if (sqrDist < sqrMaxDamageRadius)
                {
                    percent = DamageDistanceCurve.Evaluate (sqrDist / sqrMaxDamageRadius);
                    DamageableObjects[i].TrySetMaxDamage(forceMagFactor * percent * surfaceDot);
                }
            }

            MoveableDO moveableDO;

            for (int i = 0; i < MoveObjects.Length; i++)
            {
                moveableDO = MoveObjects[i];

                if (!moveableDO || moveableDO.IsDead)
                {
                    continue;
                }

                localDamagePoint = moveableDO.transform.InverseTransformPoint (damagePoint);
                localDamageForceAndSurfaceDot = moveableDO.transform.InverseTransformDirection (clampForce) * deformSurfaceDot;

                sqrDist = (moveableDO.transform.TransformPoint (moveableDO.LocalCenterPoint) - damagePoint).sqrMagnitude;

                if (sqrDist < sqrMaxDamageRadius)
                {
                    percent = DamageDistanceCurve.Evaluate (sqrDist / sqrMaxDamageRadius);
                    moveableDO.MoveObject (localDamageForceAndSurfaceDot * percent * DeformMultiplier);
                }
            }

            DetachableObject detachableObject;

            for (int i = 0; i < DetachableObjects.Length; i++)
            {
                detachableObject = DetachableObjects[i];
                localDamagePoint = detachableObject.transform.InverseTransformPoint (damagePoint);
                localDamageForceAndSurfaceDot = detachableObject.transform.InverseTransformDirection (clampForce) * deformSurfaceDot;

                for (int j = 0; j < detachableObject.DamageCheckPoints.Length; j++)
                {
                    sqrDist = (detachableObject.DamageCheckPoints[j] - localDamagePoint).sqrMagnitude;
                    if (sqrDist < sqrMaxDamageRadius)
                    {
                        percent = DamageDistanceCurve.Evaluate (sqrDist / sqrMaxDamageRadius);
                        detachableObject.SetDamageForce (forceMagFactor * surfaceDot * percent);
                    }
                }
            }

            OnDamageAction.SafeInvoke (data);
        }

        /// <summary>
        /// Apply all damage to meshes and colliders.
        /// </summary>
        public void FinalizeDamage ()
        {
            for (int i = 0; i < DamageableMeshes.Length; i++)
            {
                if (DamageableMeshes[i].Damaged)
                {
                    if (DamageableMeshes[i].Mesh)
                    {
                        DamageableMeshes[i].Mesh.vertices = DamageableMeshes[i].Verts;

                        if (CalculateNormals)
                        {
                            DamageableMeshes[i].Mesh.RecalculateNormals ();
                        }

                        DamageableMeshes[i].Mesh.RecalculateBounds ();
                    }

                    if (DamageableMeshes[i].MeshCollider)
                    {
                        DamageableMeshes[i].MeshCollider.sharedMesh = null;
                        DamageableMeshes[i].MeshCollider.sharedMesh = DamageableMeshes[i].Mesh;
                    }

                    DamageableMeshes[i].Damaged = false;
                }
            }

            for (int i = 0; i < DamageableObjects.Length; i++)
            {
                DamageableObjects[i].ApplyDamage ();
            }
        }

        int CurrentGizmoIndex;
        GizmoData[] GizmosData = new GizmoData[20];

        private void OnDrawGizmosSelected ()
        {
            if (EnableLogAndGizmo)
            {
                foreach (var gizmoData in GizmosData)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere (gizmoData.ContactPoint, 0.1f);

                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine (gizmoData.ContactPoint, gizmoData.ContactPoint + gizmoData.Normal);

                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine (gizmoData.ContactPoint, gizmoData.ContactPoint + (gizmoData.Force * 0.1f));
                }
            }
        }

        struct GizmoData
        {
            public Vector3 ContactPoint;
            public Vector3 Normal;
            public Vector3 Force;
        }

        public void CopySettings (VehicleDamageController otherController)
        {
            DamageFactor = otherController.DamageFactor;
            MaxCollisionMagnitude = otherController.MaxCollisionMagnitude;
            MaxImpulseMagnitude = otherController.MaxImpulseMagnitude;
            MaxDeformRadiusInMaxMag = otherController.MaxDeformRadiusInMaxMag;
            DeformMultiplier = otherController.DeformMultiplier;
            MaxContactPoints = otherController.MaxContactPoints;
            MinTimeBetweenCollisions = otherController.MinTimeBetweenCollisions;
            DamageDistanceCurve = otherController.DamageDistanceCurve;

            UseNoise = otherController.UseNoise;
            NoiseSize = otherController.NoiseSize;
            CalculateNormals = otherController.CalculateNormals;
        }
    }

    struct DamageMeshData
    {
        public Transform Transform;
        public MeshFilter MeshFilter;
        public MeshCollider MeshCollider;
        public bool Damaged;
        public Vector3[] Verts;
        public Mesh Mesh;
    }

    public struct DamageData
    {
        public Vector3 DamagePoint;
        public Vector3 DamageForce;
        public Vector3 SurfaceNormal;
        public float MassFactor;
    }

    struct DamageableObjectData
    {
        public DamageableObject DamageableObject;
        
        float Damage;

        public DamageableObjectData (DamageableObject damageableObject)
        {
            DamageableObject = damageableObject;
            Damage = 0;
        }

        public void TrySetMaxDamage (float damage)
        {
            if (Damage < damage)
            {
                Damage = damage;
            }
        }

        public void ApplyDamage ()
        {
            if (Damage > 0)
            {
                DamageableObject.SetDamage (Damage);
            }
            Damage = 0;
        }
    }
}