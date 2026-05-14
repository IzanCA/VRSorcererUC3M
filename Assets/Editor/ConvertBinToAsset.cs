using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ConvertBinToAsset
{
    [MenuItem("Tools/XR Hands/Convert .handsbin To .asset")]
    public static void ConvertLatestHandsBin()
    {
        string folder = "Assets/HandRecordings";

        if (!Directory.Exists(folder))
        {
            Debug.LogError("XRHandRecordings folder not found.");
            return;
        }

        string[] files = Directory.GetFiles(
            Path.GetFullPath(folder),
            "*.handsbin"
        );

        if (files.Length == 0)
        {
            Debug.LogError("No .handsbin files found.");
            return;
        }

        // Buscar el más reciente
        string latestFile = files[0];
        DateTime latestTime = File.GetLastWriteTime(latestFile);

        foreach (string file in files)
        {
            DateTime t = File.GetLastWriteTime(file);

            if (t > latestTime)
            {
                latestTime = t;
                latestFile = file;
            }
        }

        Debug.Log("Using file:");
        Debug.Log(latestFile);

        // -----------------------------------------
        // REFLECTION -> XRHandRecordingBlob
        // -----------------------------------------

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        Type blobType = null;

        foreach (Assembly asm in assemblies)
        {
            blobType = asm.GetType(
                "UnityEngine.XR.Hands.Capture.Recording.XRHandRecordingBlob"
            );

            if (blobType != null)
                break;
        }

        if (blobType == null)
        {
            Debug.LogError(
                "Could not find XRHandRecordingBlob type.\n" +
                "Make sure XR Hands package is installed."
            );

            return;
        }

        Debug.Log("Found type:");
        Debug.Log(blobType.FullName);

        // Buscar método internal/static
        MethodInfo readMethod =
            blobType.GetMethod(
                "TryReadCaptureSequenceFromDisk",
                BindingFlags.Static |
                BindingFlags.NonPublic |
                BindingFlags.Public
            );

        if (readMethod == null)
        {
            Debug.LogError(
                "Could not find TryReadCaptureSequenceFromDisk method."
            );

            return;
        }

        Debug.Log("Found method.");

        // -----------------------------------------
        // Invocar método internal
        // -----------------------------------------

        object[] args = new object[]
        {
            latestFile,
            null
        };

        bool success = (bool)readMethod.Invoke(null, args);

        if (!success)
        {
            Debug.LogError("Failed to read recording.");
            return;
        }

        UnityEngine.Object captureSequence =
            args[1] as UnityEngine.Object;

        if (captureSequence == null)
        {
            Debug.LogError("Capture sequence is null.");
            return;
        }

        // -----------------------------------------
        // Crear asset
        // -----------------------------------------

        string saveFolder =
            "Assets/ImportedHandRecordings";

        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }

        string assetPath =
            saveFolder + "/" +
            Path.GetFileNameWithoutExtension(latestFile) +
            ".asset";

        AssetDatabase.CreateAsset(
            captureSequence,
            assetPath
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("SUCCESS!");
        Debug.Log(assetPath);

        EditorGUIUtility.PingObject(
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath)
        );
    }
}