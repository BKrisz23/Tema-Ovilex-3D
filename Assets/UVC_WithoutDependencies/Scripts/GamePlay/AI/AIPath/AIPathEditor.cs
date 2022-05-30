#if UNITY_EDITOR

using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace PG
{
    //This partial class is needed for convenient path editing and gizmo display.

    public partial class AIPath :MonoBehaviour
    {
        [SerializeField] bool ShowGizmo = true;
        [Range(0.1f, 100f),SerializeField] float EditorVisualisationSubsteps = 2;   //The smaller the smoother the path will be (Visual only), the AI's behavior is not affected.
        [SerializeField] bool ShowOvertakeGizmo = true;                             //If necessary, you can hide the overtaking gizmo (For example: for Drift).
        [SerializeField] float MaxHeight = 1000f;                                   //Maximum path height (When updating path points, rays is shot down from this height).
        [SerializeField] float HeightAboveRoad = 1f;                                //Height above the surface when updating waypoints.
        [SerializeField] LayerMask RoadMask = 1 << 10 | 1 << 11;                    //Mask for checking the surface at the bottom, by default layers 10 and 11, your layer number may differ.
        
        [System.NonSerialized] public int HighlightedIndex = -1;                    //The index of the point the cursor is hovering over (Selection logic only).
        public WaypointData SelectedWaypoint;                                       //Just a field for editing a selected point, for the ability to edit a point without opening the list.

        public int SelectedWaypointIndex
        {
            get
            {
                for (int i = 0; i < Waypoints.Count; i++)
                {
                    if (Waypoints[i].Point != null && Selection.activeObject == Waypoints[i].Point.gameObject)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        public void UpdateHeightForAllWaypoints ()
        {
            RaycastHit hit;
            Vector3 pos;
            foreach (var point in Waypoints)
            {
                pos = point.position;
                pos.y = MaxHeight;
                if (Physics.Raycast (pos, Vector3.down, out hit, MaxHeight, RoadMask))
                {
                    pos = hit.point;
                    pos.y += HeightAboveRoad;
                    point.Point.position = pos;
                }
            }
        }

        private void OnDrawGizmos ()
        {
            bool isSelected = false;
            var selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
            {
                isSelected = selectedObject == gameObject;
                if (!isSelected)
                {
                    foreach (var w in Waypoints)
                    {
                        if (w.Point != null && selectedObject == w.Point.gameObject)
                        {
                            isSelected = true;
                            break;
                        }
                    }
                }

            }
            DrawGizmos (isSelected);
        }

        private void DrawGizmos (bool selected)
        {
            if (!ShowGizmo)
            {
                return;
            }

            if (Waypoints.Count > 1)
            {
                PointsCount = Waypoints.Count;

                CachePositionsAndDistances ();

                Gizmos.color = selected ? Color.yellow : new Color (1, 1, 0, 0.5f);
                Vector3 prev = Waypoints[0].position;
                float prevOvertakeLeft = Waypoints[0].OvertakeZoneLeft;
                float prevOvertakeRight = Waypoints[0].OvertakeZoneRight;
                float nextOvertakeLeft;
                float nextOvertakeRight;
                float speedLimit;

                //Displays path.
                for (float dist = 0; dist < Length; dist += EditorVisualisationSubsteps)
                {
                    Vector3 next = GetRoutePosition (dist + EditorVisualisationSubsteps, out nextOvertakeLeft, out nextOvertakeRight, out speedLimit);
                    if (selected)
                    {
                        var dir = next - prev;
                        var left = Vector3.Cross(dir.normalized, Vector3.up);

                        //Displays overtake.
                        if (ShowOvertakeGizmo)
                        {
                            Gizmos.color = Color.blue;
                            Gizmos.DrawLine (prev + left * prevOvertakeLeft, next + left * nextOvertakeLeft);
                            Gizmos.DrawLine (prev + -left * prevOvertakeRight, next + -left * nextOvertakeRight);

                            prevOvertakeLeft = nextOvertakeLeft;
                            prevOvertakeRight = nextOvertakeRight;
                        }
                    }

                    //Depending on the speed limit, a color for the path is selected (from minSpeed - red to maxSpeed - green)
                    Gizmos.color = Color.Lerp (Color.red, Color.green, speedLimit / MaxSpeedLimit);
                    
                    Gizmos.DrawLine (prev, next);
                    prev = next;
                }

                if (selected)
                {
                    var guiStyle = new GUIStyle();
                    guiStyle.normal.textColor = Color.green;
                    Vector3 pos;
                    for (int i = 0; i < Waypoints.Count; i++)
                    {
                        bool isHighlighted = HighlightedIndex == i;
                        Gizmos.color = isHighlighted ? Color.yellow : Color.red;
                        Gizmos.DrawSphere (Waypoints[i].position, isHighlighted ? 2: 1.5f);

                        //Displays the speed limit near the waypoint.
                        if (Waypoints[i].SpeedLimit < MaxSpeedLimit && SceneView.lastActiveSceneView.camera)
                        {
                            pos = Waypoints[i].position;
                            pos += SceneView.lastActiveSceneView.camera.transform.right * 3;
                            Handles.color = Color.green;
                            Handles.Label (pos, Waypoints[i].SpeedLimit.ToString (), guiStyle);
                        }
                    }
                }
            }
        }
    }

    #region WaypointDataDrawer

    /// <summary>
    /// All WaypointData is displayed in one line (For ease of editing in the list).
    /// </summary>
    [CustomPropertyDrawer (typeof (AIPath.WaypointData))]
    public class WaypointDataDrawer :PropertyDrawer
    {
        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            var pointProperty = property.FindPropertyRelative("Point");
            var overtakeZoneLeftProperty = property.FindPropertyRelative("OvertakeZoneLeft");
            var overtakeZoneRightProperty = property.FindPropertyRelative("OvertakeZoneRight");
            var speedLimitProperty = property.FindPropertyRelative("SpeedLimit");

            var obj = (Transform)pointProperty.objectReferenceValue;
            if (obj != null && Selection.activeObject == obj.gameObject)
            {
                var selectRect = position;
                selectRect.x -= 2;
                selectRect.y -= 2;
                selectRect.width += 4;
                selectRect.height += 4;

                var style = new GUIStyle (GUI.skin.box);
                style.normal.background = SelectedTexture (1, 1, Color.yellow);

                EditorGUI.LabelField (selectRect, GUIContent.none, style: style);
            }


            float floatWidth = position.width * 0.2f;

            position.height = EditorGUIUtility.singleLineHeight;
            position.width -= floatWidth * 3 - 10;

            EditorGUI.PropertyField (position, pointProperty, GUIContent.none);

            position.x += position.width;
            position.width = floatWidth;

            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 20;

            EditorGUI.PropertyField (position, overtakeZoneLeftProperty, new GUIContent (" ", tooltip: "Left overtaking distance"));

            position.x += position.width;

            EditorGUI.PropertyField (position, overtakeZoneRightProperty, new GUIContent (" ", tooltip: "Right overtaking distance"));

            position.x += position.width;

            EditorGUI.PropertyField (position, speedLimitProperty, new GUIContent (" ", tooltip: "Speed limit"));

            EditorGUIUtility.labelWidth = labelWidth;
        }

        public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        /// <summary>
        /// A texture for highlighting the selected point.
        /// </summary>
        private Texture2D SelectedTexture (int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D( width, height );
            result.SetPixels (pix);
            result.Apply ();
            return result;
        }
    }

    #endregion //WaypointDataDrawer

    #region AIPathEditor

    /// <summary>
    /// For the convenience of editing the path.
    /// </summary>
    [CustomEditor (typeof (AIPath))]
    public class AIPathEditor :Editor
    {
        AIPath AIPath { get { return target as AIPath; } }
        List<AIPath.WaypointData> Waypoints { get { return AIPath.Waypoints; } }
        int HighlightedIndex { get { return AIPath.HighlightedIndex; } set { AIPath.HighlightedIndex = value; } }
        int PrevSelectedIndex = -1;

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI ();
            var selectedIndex = AIPath.SelectedWaypointIndex;

            if (selectedIndex >= 0)
            {
                EditorGUI.BeginChangeCheck ();
                if (PrevSelectedIndex != selectedIndex)
                {
                    PrevSelectedIndex = selectedIndex;
                    AIPath.SelectedWaypoint = Waypoints[selectedIndex];
                }

                Waypoints[selectedIndex] = AIPath.SelectedWaypoint;
            }

            if (GUILayout.Button ("AddWaypoint"))
            {
                AddWaypoint ();
            }

            if (selectedIndex != -1)
            {
                if (GUILayout.Button ("Add after selected"))
                {
                    AddWaypoint (selectedIndex);
                }

                if (GUILayout.Button ("Remove selected"))
                {
                    RemoveWaypoint (selectedIndex);
                }
            }

            if (GUILayout.Button ("Update list"))
            {
                UpdateList ();
            }

            GUILayout.Space (10);

            if (Application.isPlaying)
            {
                if (GUILayout.Button ("Copy waypoints (For save in play mode)"))
                {
                    CopyWaypoints ();
                }
            }
            else
            {
                if (GUILayout.Button ("Paste waypoints (For paste in edit mode)"))
                {
                    PasteWaypoints ();
                }
            }
        }

        #region Select item logic

        Vector2[] WaypointScreenPositions = new Vector2[0];

        /// <summary>
        /// Point selection logic in the editor.
        /// !!!Important. The inspector must be locked for easy editing!!!
        /// </summary>
        private void OnSceneGUI ()
        {
            if (Event.current.clickCount > 0 && HighlightedIndex >= 0 && Waypoints[HighlightedIndex].Point != null)
            {
                Selection.activeObject = Waypoints[HighlightedIndex].Point.gameObject;
                SceneView.RepaintAll ();
                return;
            }

            if (WaypointScreenPositions.Length != Waypoints.Count)
            {
                WaypointScreenPositions = new Vector2[Waypoints.Count];
            }

            for (int i = 0; i < WaypointScreenPositions.Length; i++)
            {
                WaypointScreenPositions[i] = SceneView.lastActiveSceneView.camera.WorldToScreenPoint (Waypoints[i].position);
            }


            var prevIndex = HighlightedIndex;
            HighlightedIndex = -1;
            
            Vector2 mousePos = Event.current.mousePosition;
            mousePos.y = SceneView.lastActiveSceneView.camera.pixelHeight - mousePos.y;

            for (int i = 0; i < WaypointScreenPositions.Length; i++)
            {
                if ((mousePos - WaypointScreenPositions[i]).sqrMagnitude < 100)
                {
                    HighlightedIndex = i;
                    break;
                }
            }

            if (prevIndex != HighlightedIndex)
            {
                SceneView.RepaintAll ();
            }
        }

        #endregion //Select item logic

        #region Button methods

        /// <summary>
        /// Adding a point to the end of the list or after the "Index" parameter, all data is taken from the previous point.
        /// </summary>
        void AddWaypoint (int index = -1)
        {

            var newItem = new GameObject().transform;
            newItem.SetParent (AIPath.transform);

            if (index == -1)
            {
                if (AIPath.Waypoints.Count > 0)
                {
                    var lastWaypoint = AIPath.Waypoints.Last ();
                    newItem.position = lastWaypoint.position;
                    AIPath.Waypoints.Add (new AIPath.WaypointData (newItem, lastWaypoint.OvertakeZoneLeft, lastWaypoint.OvertakeZoneRight, lastWaypoint.SpeedLimit));
                }
                else
                {
                    AIPath.Waypoints.Add (new AIPath.WaypointData (newItem, speedLimit: AIPath.MaxSpeedLimit));
                }
            }
            else
            {
                var waypoint = AIPath.Waypoints[index];

                newItem.position = waypoint.position;
                newItem.transform.SetSiblingIndex (index + 1);
                AIPath.Waypoints.Insert (index + 1, new AIPath.WaypointData (newItem, waypoint.OvertakeZoneLeft, waypoint.OvertakeZoneRight, waypoint.SpeedLimit));
            }

            UpdateList ();

            Selection.activeObject = newItem.gameObject;
            EditorUtility.SetDirty (AIPath);

            if (SceneView.lastActiveSceneView != null)
            {
                //SceneView.lastActiveSceneView.pivot = newItem.position + new Vector3 (0, 30, 0);
                SceneView.lastActiveSceneView.Repaint ();

                Repaint ();
            }
        }

        /// <summary>
        /// Remove point with specified index.
        /// </summary>
        void RemoveWaypoint (int index)
        {
            if (AIPath.Waypoints[index].Point != null)
            {
                if (index - 1 >= 0 && AIPath.Waypoints[index - 1].Point != null)
                {
                    Selection.activeObject = AIPath.Waypoints[index - 1].Point.gameObject;
                }

                GameObject.DestroyImmediate (AIPath.Waypoints[index].Point.gameObject);

                Repaint ();
                EditorUtility.SetDirty (AIPath);
            }

            UpdateList ();
        }

        /// <summary>
        /// Update point names and align all points in height.
        /// </summary>
        void UpdateList ()
        {
            AIPath.Waypoints.RemoveAll (w => w.Point == null);

            for (int i = 0; i < AIPath.Waypoints.Count; i++)
            {
                AIPath.Waypoints[i].Point.name = "Waypoint " + (i + 1).ToString ("000");
            }
            AIPath.UpdateHeightForAllWaypoints ();
        }

        /// <summary>
        /// Copying settings for all points, for transferring nostrokes from playmode to editmod.
        /// </summary>
        void CopyWaypoints ()
        {
            WaypointDataDebugList waypointsForSave = new WaypointDataDebugList();
            foreach (var w in Waypoints)
            {
                waypointsForSave.DataList.Add (new WaypointDataDebug(w));
            }

            EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson (waypointsForSave);
            Debug.Log ("Wayoints have been copied");
        }

        /// <summary>
        /// Paste settings for all points, for transferring nostrokes from playmode to editmod.
        /// </summary>
        void PasteWaypoints ()
        {
            try
            {
                var str = EditorGUIUtility.systemCopyBuffer;
                WaypointDataDebugList waypointsForSave = JsonUtility.FromJson<WaypointDataDebugList>(EditorGUIUtility.systemCopyBuffer);
                if (waypointsForSave != null)
                {
                    for (int i = 0; i < waypointsForSave.DataList.Count; i++)
                    {
                        if (i  >= Waypoints.Count)
                        {
                            AddWaypoint ();
                        }

                        var w = Waypoints[i];
                        w.Point.position = waypointsForSave.DataList[i].position;
                        w.OvertakeZoneLeft = waypointsForSave.DataList[i].OvertakeZoneLeft;
                        w.SpeedLimit = waypointsForSave.DataList[i].SpeedLimit;

                        Waypoints[i] = w;
                    }
                }
                UpdateList ();
                EditorUtility.SetDirty (target);
                Debug.Log ("Wayoints have been pasted");
            }
            catch (System.Exception e)
            {
                Debug.LogError (e);
            }
        }

        #endregion //Button methods

        /// <summary>
        /// Class for serializing the list.
        /// </summary>
        [System.Serializable]
        public class WaypointDataDebugList
        {
            public List<WaypointDataDebug> DataList = new List<WaypointDataDebug>();
        }

        /// <summary>
        /// The structure is only needed to copy points from the playmod to the editmod.
        /// </summary>
        [System.Serializable]
        public struct WaypointDataDebug
        {
            public Vector3 position;
            public float OvertakeZoneLeft;
            public float OvertakeZoneRight;
            public float SpeedLimit;

            public WaypointDataDebug (AIPath.WaypointData data)
            {
                position = data.position;
                OvertakeZoneLeft = data.OvertakeZoneLeft;
                OvertakeZoneRight = data.OvertakeZoneRight;
                SpeedLimit = data.SpeedLimit;
            }
        }
    }

    #endregion //AIPathEditor
}

#endif