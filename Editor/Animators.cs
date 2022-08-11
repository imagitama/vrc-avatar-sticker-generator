using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace PeanutTools_VRC_Avatar_Sticker_Generator_Editor {
    public class Animators {
        // this performs a deep clone (remember transitions/conditions/etc. are not copied and reference originals)
        public static AnimatorController CopyAnimatorController(AnimatorController animatorControllerToCopy, string newFullPath) {
            string currentPath = AssetDatabase.GetAssetPath(animatorControllerToCopy);

            string newFullPathAbsolute = Application.dataPath + "/" + newFullPath;

            string dirName = Path.GetDirectoryName(newFullPath);

            Directory.CreateDirectory(dirName);

            Debug.Log("Copying animator controller to " + newFullPath + " (" + dirName + ")...");

            bool result = AssetDatabase.CopyAsset(currentPath, newFullPath);

            if (!result) {
                throw new System.Exception("Failed to copy animator to " + newFullPath);
            }

            return (AnimatorController)AssetDatabase.LoadAssetAtPath(newFullPath, typeof(AnimatorController));
        }

        public static void DeleteAnimatorController(AnimatorController animatorControllerToDelete) {
            string pathToAnimator = AssetDatabase.GetAssetPath(animatorControllerToDelete);
            
            bool result = AssetDatabase.DeleteAsset(pathToAnimator);

            if (!result) {
                throw new System.Exception("Failed to delete animator at " + pathToAnimator);
            }
        }
    }
}