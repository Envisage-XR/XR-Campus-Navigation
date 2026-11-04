#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Editor helper: Campus Nav > Create Location Test Objects builds and wires the test scene objects.</summary>
public static class CampusNavSetup
{
    [MenuItem("Campus Nav/Create Location Test Objects")]
    static void CreateLocationTestObjects()
    {
        if (GameObject.Find("Location") != null || GameObject.Find("Places") != null)
        {
            Debug.LogWarning("[CampusNavSetup] 'Location' or 'Places' already exists in this scene. Delete them first to recreate.");
            return;
        }

        var loc = new GameObject("Location");
        Undo.RegisterCreatedObjectUndo(loc, "Create Location test objects");
        var gps = loc.AddComponent<GPSManager>();
        var compass = loc.AddComponent<CompassManager>();
        var mock = loc.AddComponent<MockLocationProvider>();
        var hub = loc.AddComponent<LocationHub>();

        var places = new GameObject("Places");
        Undo.RegisterCreatedObjectUndo(places, "Create Location test objects");
        var overpass = places.AddComponent<OverpassAPIManager>();
        var db = places.AddComponent<CampusPOIDatabase>();
        var panel = places.AddComponent<LocationDebugPanel>();

        Link(gps, "compass", compass);
        Link(hub, "gps", gps);
        Link(hub, "mock", mock);
        Link(db, "overpass", overpass);
        Link(panel, "hub", hub);
        Link(panel, "db", db);

        EditorSceneManager.MarkSceneDirty(loc.scene);
        Selection.activeGameObject = loc;
        Debug.Log("[CampusNavSetup] Created 'Location' and 'Places' and wired them. Save the scene (Ctrl+S) and press Play.");
    }

    static void Link(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogError($"[CampusNavSetup] {target.GetType().Name} has no serialized field '{field}'."); return; }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
