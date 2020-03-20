using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Steering
{
    [CreateAssetMenu(menuName = "Steering/Create Settings")]
    public class GameWorldSettings : ScriptableObject
    {
        [Header("Behavior Weight")]
        public int SeparationWeight = 1;
        public int AlignmentWeight = 1;
        public int CohesionWeight = 2;
        public int ObstacleAvoidanceWeight = 10;
        public int WallAvoidanceWeight = 10;
        public int WanderWeight = 1;
        public int SeekWeight = 1;
        public int FleeWeight = 1;
        public int ArriveWeight = 1;
        public int PursuitWeight = 1;
        public int InterposeWeight = 1;
        public int HideWeight = 1;
        public float EvadeWeight = 0.01f;
        public float FollowPathWeight = 0.05f;

        [Header("Move Config")]
        [Tooltip("Used to multiply the steering force.")]
        public int ForceMultiper = 13;
        public int MaxSpeed = 10;
        [Tooltip("Finally MaxForce = MaxForce * ForceMultiper")]
        public int MaxForce = 2;


        private static GameWorldSettings _instance;

        public static GameWorldSettings Instance
        {
            get
            {
                GameWorldSettings[] gws = Resources.FindObjectsOfTypeAll<GameWorldSettings>();
                if (!_instance && gws.Length > 0)
                {
                    _instance = (GameWorldSettings)gws.GetValue(0);
                }
#if UNITY_EDITOR
                if (!_instance)
                {
                    InitializeFromDefault(UnityEditor.AssetDatabase.LoadAssetAtPath<GameWorldSettings>("Assets/AI/Scripts/Steering/Game World Settings.asset"));
                }
#endif
                return _instance;
            }
        }

        public static void LoadFromJSON(string path)
        {
            if (_instance) DestroyImmediate(_instance);
            _instance = CreateInstance<GameWorldSettings>();
            JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(path), _instance);
            _instance.hideFlags = HideFlags.HideAndDontSave;
        }

        public static void SaveToJSON(string path)
        {
            Debug.LogFormat("Save Game Setting To {0}", path);
            System.IO.File.WriteAllText(path, JsonUtility.ToJson(Instance, true));
        }

        public static void InitializeFromDefault(GameWorldSettings settings)
        {
            if (_instance) DestroyImmediate(_instance);
            _instance = Instantiate(settings);
            _instance.hideFlags = HideFlags.HideAndDontSave;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Steering/Settings")]
        public static void ShowGameSettings()
        {
            UnityEditor.Selection.activeObject = Instance;
        }
#endif
    }
}

