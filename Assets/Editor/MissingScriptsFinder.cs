using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class MissingScriptsFinder
{
    [MenuItem("Tools/Find Missing Scripts In Scene")]
    private static void FindMissingScripts()
    {
        int missingCount = 0;

        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();

        foreach (var root in roots)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                var comps = t.GetComponents<Component>();
                for (int i = 0; i < comps.Length; i++)
                {
                    if (comps[i] == null)
                    {
                        missingCount++;
                        Debug.LogWarning($"Missing script on: {GetFullPath(t.gameObject)}", t.gameObject);
                    }
                }
            }
        }

        Debug.Log($"Done. Missing scripts found: {missingCount}");
    }

    [MenuItem("Tools/Remove Missing Scripts In Scene")]
    private static void RemoveMissingScripts()
    {
        int removed = 0;

        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();

        foreach (var root in roots)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
        }

        Debug.Log($"Done. Removed missing scripts: {removed}");
    }

    private static string GetFullPath(GameObject go)
    {
        string path = go.name;
        Transform current = go.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
}
