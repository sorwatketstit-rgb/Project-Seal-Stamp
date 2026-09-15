using UnityEditor;
using UnityEngine;

namespace ProjectDawn.CozyBuilder.Editor
{
    static class CozyMenuItem
    {
        [MenuItem("GameObject/Cozy Builder/Renderer", false, 10)]
        static void CreateRenderer()
        {
            // Create a new GameObject
            GameObject newGameObject = new GameObject("Renderer", typeof(CozyRenderer));

            Transform parentTransform = Selection.activeTransform;

            if (parentTransform != null)
            {
                // Parent the new GameObject to the currently selected object
                newGameObject.transform.SetParent(parentTransform, false);

                // Optionally, set local position and rotation to align with the parent
                newGameObject.transform.localPosition = Vector3.zero;
                newGameObject.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // If no parent, place it at the root of the hierarchy
                newGameObject.transform.SetParent(null);
            }

            // Register the creation of the object for undo (so that it's undoable)
            Undo.RegisterCreatedObjectUndo(newGameObject, "Create Point");

            // Select the new GameObject in the Hierarchy
            Selection.activeGameObject = newGameObject;
        }

        [MenuItem("GameObject/Cozy Builder/Mask", false, 10)]
        static void CreateMask()
        {
            // Create a new GameObject
            GameObject newGameObject = new GameObject("Mask", typeof(CozyMask));

            Transform parentTransform = Selection.activeTransform;

            if (parentTransform != null)
            {
                // Parent the new GameObject to the currently selected object
                newGameObject.transform.SetParent(parentTransform, false);

                // Optionally, set local position and rotation to align with the parent
                newGameObject.transform.localPosition = Vector3.zero;
                newGameObject.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // If no parent, place it at the root of the hierarchy
                newGameObject.transform.SetParent(null);
            }

            // Register the creation of the object for undo (so that it's undoable)
            Undo.RegisterCreatedObjectUndo(newGameObject, "Create Mask");

            // Select the new GameObject in the Hierarchy
            Selection.activeGameObject = newGameObject;
        }
    }
}
