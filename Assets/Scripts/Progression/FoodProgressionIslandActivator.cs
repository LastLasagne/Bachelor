using System.Collections.Generic;
using Obvious.Soap;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class FoodProgressionIslandActivator : MonoBehaviour
{
    private const int DefaultCollectionCount = 10;

    [SerializeField] private IntVariable foodProgression;
    [SerializeField, Min(1)] private int collectionCount = DefaultCollectionCount;
    [SerializeField] private List<GameObject> collections = new List<GameObject>();

    public IntVariable FoodProgression { get => foodProgression; set => foodProgression = value; }
    public IReadOnlyList<GameObject> Collections => collections;

    private void OnEnable()
    {
        EnsureCollectionSetup();
        Subscribe();
        RefreshActiveCollections();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        collectionCount = Mathf.Max(1, collectionCount);
        EnsureCollectionSetup();
        RefreshActiveCollections();
    }

    private void Subscribe()
    {
        if (foodProgression != null)
        {
            foodProgression.OnValueChanged += HandleFoodProgressionChanged;
        }
    }

    private void Unsubscribe()
    {
        if (foodProgression != null)
        {
            foodProgression.OnValueChanged -= HandleFoodProgressionChanged;
        }
    }

    private void HandleFoodProgressionChanged(int _)
    {
        RefreshActiveCollections();
    }

    private void RefreshActiveCollections()
    {
        int unlockedCount = foodProgression != null ? foodProgression.Value : 0;

        for (int i = 0; i < collections.Count; i++)
        {
            GameObject collection = collections[i];
            if (collection != null)
            {
                collection.SetActive(i < unlockedCount);
            }
        }
    }

    private void EnsureCollectionSetup()
    {
        RemoveMissingCollectionReferences();

        for (int i = collections.Count; i < collectionCount; i++)
        {
            GameObject collection = FindDirectChild(BuildCollectionName(i + 1));

            if (collection == null)
            {
                collection = CreateCollection(i + 1);
            }

            collections.Add(collection);
        }

        for (int i = 0; i < collections.Count; i++)
        {
            GameObject collection = collections[i];
            if (collection != null)
            {
                collection.name = BuildCollectionName(i + 1);
                collection.transform.localPosition = GetCollectionLocalPosition(i);
                EnsurePlaceholderContent(collection.transform, i);
            }
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private void RemoveMissingCollectionReferences()
    {
        for (int i = collections.Count - 1; i >= 0; i--)
        {
            if (collections[i] == null)
            {
                collections.RemoveAt(i);
            }
        }
    }

    private GameObject FindDirectChild(string childName)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == childName)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private GameObject CreateCollection(int index)
    {
        var collection = new GameObject(BuildCollectionName(index));
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RegisterCreatedObjectUndo(collection, "Create food progression collection");
        }
#endif
        collection.transform.SetParent(transform, false);
        return collection;
    }

    private void EnsurePlaceholderContent(Transform collection, int collectionIndex)
    {
        if (collection.childCount > 0)
        {
            return;
        }

        AddPlaceholder(collection, $"Species Placeholder {collectionIndex + 1}", PrimitiveType.Capsule, new Vector3(-0.55f, 0.35f, 0f), new Vector3(0.25f, 0.35f, 0.25f));
        AddPlaceholder(collection, $"Population Placeholder {collectionIndex + 1}", PrimitiveType.Sphere, new Vector3(0f, 0.25f, 0f), new Vector3(0.28f, 0.28f, 0.28f));
        AddPlaceholder(collection, $"Forest Placeholder {collectionIndex + 1}", PrimitiveType.Cylinder, new Vector3(0.55f, 0.35f, 0f), new Vector3(0.18f, 0.7f, 0.18f));
    }

    private static void AddPlaceholder(Transform parent, string objectName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale)
    {
        GameObject placeholder = GameObject.CreatePrimitive(primitiveType);
        placeholder.name = objectName;
        placeholder.transform.SetParent(parent, false);
        placeholder.transform.localPosition = localPosition;
        placeholder.transform.localScale = localScale;

        Collider collider = placeholder.GetComponent<Collider>();
        if (collider != null)
        {
            DestroySmart(collider);
        }
    }

    private static void DestroySmart(Object target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private static string BuildCollectionName(int index)
    {
        return $"Food Progression Collection {index:00}";
    }

    private static Vector3 GetCollectionLocalPosition(int index)
    {
        const float radius = 4.5f;
        const float height = 0.25f;
        float angle = index * Mathf.PI * 2f / DefaultCollectionCount;
        return new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
    }
}
