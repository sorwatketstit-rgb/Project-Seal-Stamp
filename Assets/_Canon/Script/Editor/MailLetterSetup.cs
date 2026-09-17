#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SM64.Editor
{
    /// <summary>
    /// Editor utility that sets up the full mail letter collection system.
    /// Run via:  Tools > Seal Stamp > Setup Mail Letter System
    /// </summary>
    public static class MailLetterSetup
    {
        private const string LETTER_TAG    = "Letter";
        private const string COLLECT_LAYER = "Collectables";

        [MenuItem("Tools/Seal Stamp/Setup Mail Letter System")]
        public static void Setup()
        {
            EnsureTag(LETTER_TAG);
            EnsureLayer(COLLECT_LAYER);

            CreateCollectableManager();
            AttachPlayerInventory();
            CreateSampleLetter();
            CreateLetterCountUI();

            Debug.Log("[MailLetterSetup] Mail letter system setup complete!");
            EditorUtility.DisplayDialog("Mail Letter System",
                "Setup complete!\n\n" +
                "- 'Letter' tag added\n" +
                "- 'Collectables' layer added\n" +
                "- CollectableManager (DontDestroyOnLoad) created\n" +
                "- PlayerInventory attached to player\n" +
                "- Sample MailLetter created in scene\n" +
                "- Letter Count UI created under Canvas",
                "OK");
        }

        // -----------------------------------------------------------------------
        //  Tag & Layer helpers
        // -----------------------------------------------------------------------
        private static void EnsureTag(string tag)
        {
            SerializedObject tagManager = GetTagManager();
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"[MailLetterSetup] Tag '{tag}' added.");
        }

        private static void EnsureLayer(string layerName)
        {
            SerializedObject tagManager = GetTagManager();

            for (int i = 8; i <= 31; i++)
            {
                SerializedProperty prop = tagManager.FindProperty("layers");
                if (prop == null) break;

                SerializedProperty layerProp = prop.GetArrayElementAtIndex(i);
                if (layerProp == null) break;

                if (layerProp.stringValue == layerName) return;
                if (string.IsNullOrEmpty(layerProp.stringValue))
                {
                    layerProp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[MailLetterSetup] Layer '{layerName}' added at index {i}.");
                    return;
                }
            }
        }

        private static SerializedObject GetTagManager()
        {
            return new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        }

        // -----------------------------------------------------------------------
        //  Scene helpers
        // -----------------------------------------------------------------------
        private static void CreateCollectableManager()
        {
            CollectableManager existing = Object.FindFirstObjectByType<CollectableManager>();
            if (existing != null)
            {
                Debug.Log("[MailLetterSetup] CollectableManager already exists in scene.");
                return;
            }

            GameObject managerGO = new GameObject("CollectableManager");
            managerGO.AddComponent<CollectableManager>();
            Undo.RegisterCreatedObjectUndo(managerGO, "Create CollectableManager");
            Debug.Log("[MailLetterSetup] CollectableManager created (DontDestroyOnLoad).");
        }

        private static void AttachPlayerInventory()
        {
            SM64PlayerController player = Object.FindFirstObjectByType<SM64PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[MailLetterSetup] No SM64PlayerController found. Add PlayerInventory manually.");
                return;
            }

            if (player.GetComponent<PlayerInventory>() == null)
            {
                player.gameObject.AddComponent<PlayerInventory>();
                EditorUtility.SetDirty(player.gameObject);
                Debug.Log("[MailLetterSetup] PlayerInventory added to player.");
            }
        }

        private static void CreateSampleLetter()
        {
            int letterLayer = LayerMask.NameToLayer(COLLECT_LAYER);

            GameObject letter = new GameObject("MailLetter_Sample");
            letter.tag = LETTER_TAG;
            if (letterLayer != -1) letter.layer = letterLayer;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(letter.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale    = new Vector3(0.4f, 0.3f, 0.05f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            BoxCollider col = letter.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size      = new Vector3(0.8f, 0.8f, 0.8f);

            letter.AddComponent<MailLetterCollectable>();
            letter.transform.position = new Vector3(0f, 1.5f, 3f);

            Undo.RegisterCreatedObjectUndo(letter, "Create Sample Mail Letter");
            Selection.activeGameObject = letter;
            Debug.Log("[MailLetterSetup] Sample MailLetter created at (0, 1.5, 3).");
        }

        private static void CreateLetterCountUI()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[MailLetterSetup] No Canvas found. Create one first, then re-run.");
                return;
            }

            LetterCountUIDisplay existing = Object.FindFirstObjectByType<LetterCountUIDisplay>();
            if (existing != null)
            {
                Debug.Log("[MailLetterSetup] LetterCountUIDisplay already exists.");
                return;
            }

            GameObject uiGO = new GameObject("LetterCountUI");
            uiGO.transform.SetParent(canvas.transform, false);

            RectTransform rect = uiGO.AddComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0f, 1f);
            rect.anchorMax        = new Vector2(0f, 1f);
            rect.pivot            = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(10f, -90f);
            rect.sizeDelta        = new Vector2(300f, 40f);

            TMPro.TextMeshProUGUI tmp = uiGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text      = "Letters: 0";
            tmp.fontSize  = 18;
            tmp.color     = Color.white;

            uiGO.AddComponent<LetterCountUIDisplay>();
            Undo.RegisterCreatedObjectUndo(uiGO, "Create Letter Count UI");
            Debug.Log("[MailLetterSetup] LetterCountUI created under Canvas.");
        }
    }
}
#endif
