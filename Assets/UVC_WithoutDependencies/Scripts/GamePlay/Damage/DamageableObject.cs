using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    /// <summary>
    /// Damageable object, Base class.
    /// </summary>
    public class DamageableObject :MonoBehaviour
    {
        public float Health = 100;
        public float MaxDamage = float.PositiveInfinity;                //Maximum damage done at one time
        public event System.Action<float> OnChangeHealthAction;
        public event System.Action OnDeathAction;
        
        public bool IsDead { get { return Health <= 0; } }
        public bool IsInited { get; private set; }

        public float InitHealth { get; private set; }
        public float HealthPercent { get; private set; }

        MeshFilter _MeshFilter;
        public MeshFilter MeshFilter
        {
            get
            {
                if (!_MeshFilter)
                {
                    _MeshFilter = GetComponent<MeshFilter> ();
                }
                return _MeshFilter;
            }
        }
        Vector3? _LocalCenterPoint;     //Local center of mass of vertices, if there is a mesh filter.
        public Vector3 LocalCenterPoint
        {
            get
            {
                if (!MeshFilter)
                {
                    return Vector3.zero;
                }
                if (!_LocalCenterPoint.HasValue)
                {
                    Vector3 sum = Vector3.zero;
                    foreach (var vert in MeshFilter.sharedMesh.vertices)
                    {
                        sum += vert;
                    }
                    _LocalCenterPoint = sum / MeshFilter.sharedMesh.vertices.Length;
                }

                return _LocalCenterPoint.Value;
            }
        }

        public virtual void Awake ()
        {
            InitDamageObject ();
        }

        public virtual void InitDamageObject ()
        {
            if (!IsInited)
            {
                IsInited = true;
                InitHealth = Health;
                HealthPercent = 1;
            }
        }

        public float GetClampedDamage (float damage)
        {
            return damage.Clamp (0, MaxDamage);
        }

        public virtual void SetDamage (float damage)
        {
            damage = GetClampedDamage (damage);
            if (IsDead)
                return;

            Health -= damage;
            HealthPercent = (Health / InitHealth).Clamp();
            OnChangeHealthAction.SafeInvoke (-damage);

            if (Health <= 0)
            {
                Death ();
            }
        }

        /// <summary>
        /// Destroy the object completely.
        /// </summary>
        public void Kill ()
        {
            if (!IsDead)
            {
                OnChangeHealthAction.SafeInvoke (-Health);
                HealthPercent = 0;
                Health = 0;
                Death ();
            }
        }

        /// <summary>
        /// The method called after death.
        /// </summary>
        void Death ()
        {
            OnDeathAction.SafeInvoke ();
            DoDeath ();
        }

        /// <summary>
        /// Method to override.
        /// </summary>
        public virtual void DoDeath () { }
    }
}
