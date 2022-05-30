using System;
using UnityEngine;

namespace PG
{
    /// <summary> To hide unnecessary fields from the inspector </summary>
    [AttributeUsage (AttributeTargets.Field)]
    public class HideInInspectorIf :PropertyAttribute
    {
        public string PropertyToCheck;

        public bool HideInInspector;

        public HideInInspectorIf (string propertyToCheck, bool hideInInspector = true)
        {
            PropertyToCheck = propertyToCheck;
            HideInInspector = hideInInspector;
        }
    }

    [AttributeUsage (AttributeTargets.Field)]
    public class ShowInInspectorIf: HideInInspectorIf
    {
        public ShowInInspectorIf (string propertyToCheck): base(propertyToCheck, false)
        {
        }
    }
}