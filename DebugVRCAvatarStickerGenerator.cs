using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Avatars.Components;

class DebugVRCAvatarStickerGenerator : MonoBehaviour {
    void OnDrawGizmos() {
        DrawDebug();
    }

    void DrawDebug() {
        SkinnedMeshRenderer [] skinnedMeshRenderers = GameObject.FindObjectsOfType<SkinnedMeshRenderer>();

        Vector3 center = new Vector3(0, 0, 0);

        Quaternion newRotation = new Quaternion();
        newRotation.eulerAngles = new Vector3(-90, 0, 0);

        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers) {
            Mesh mesh = new Mesh(); 

            skinnedMeshRenderer.BakeMesh(mesh);

            Vector3[] vertices = mesh.vertices;

            for (int i = 0; i < vertices.Length; i++) {
                Vector3 verticePosition = newRotation * (vertices[i] - center) + center;

                // Gizmos.color = Color.yellow;
                // Gizmos.DrawSphere(verticePosition, 0.001f);

                min = Vector3.Min(min, verticePosition);
                max = Vector3.Max(max, verticePosition);
            }

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}