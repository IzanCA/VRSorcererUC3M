using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

public class GestureRecordingExporter : MonoBehaviour
{
    [Header("Gesture Name")]
    public TMP_InputField gestureNameInputField;

    [Header("Export Folder")]
    [Tooltip("Editor: Assets/HandRecordings\nBuild: Any writable folder")]
    public string exportFolderPath = "Assets/HandRecordings";

    private string sourceFolder;

    private FileSystemWatcher watcher;

    // Pendientes para main thread
    private static string pendingHandsBinPath;
    private static string pendingExportFolder;

    private void Reset()
    {
#if UNITY_EDITOR
        exportFolderPath = "Assets/HandRecordings";
#else
        exportFolderPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments),
            "XRHandRecordings"
        );
#endif
    }

    private void Start()
    {
        sourceFolder = Path.Combine(
            Application.persistentDataPath,
            "RecordedHandData"
        );

        if (!Directory.Exists(sourceFolder))
        {
            Debug.LogError("RecordedHandData folder not found:");
            Debug.LogError(sourceFolder);
            return;
        }

        watcher = new FileSystemWatcher(
            sourceFolder,
            "*.handsbin"
        );

        watcher.Created += OnNewRecordingCreated;

        watcher.EnableRaisingEvents = true;

        Debug.Log("Watching folder:");
        Debug.Log(sourceFolder);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(pendingHandsBinPath))
        {
            ConvertHandsBinToAsset(
                pendingHandsBinPath,
                pendingExportFolder
            );

            pendingHandsBinPath = null;
            pendingExportFolder = null;
        }
#endif
    }

    private void OnDestroy()
    {
        if (watcher != null)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
    }

    private void OnNewRecordingCreated(
        object sender,
        FileSystemEventArgs e
    )
    {
        try
        {
            System.Threading.Thread.Sleep(1500);

            string gestureName = "Gesture";

            if (gestureNameInputField != null &&
                !string.IsNullOrWhiteSpace(
                    gestureNameInputField.text))
            {
                gestureName = gestureNameInputField.text;
            }

            string outputFolder = exportFolderPath;

            // Assets/... → ruta absoluta
            if (outputFolder.StartsWith("Assets"))
            {
                outputFolder =
                    Path.GetFullPath(outputFolder);
            }

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            string newFileName =
                $"{gestureName}_{DateTime.Now:yyyyMMdd_HHmmss}.handsbin";

            string destinationPath =
                Path.Combine(outputFolder, newFileName);

            File.Copy(e.FullPath, destinationPath, true);

            Debug.Log("Copied handsbin:");
            Debug.Log(destinationPath);

#if UNITY_EDITOR
            // Solo convertir en editor
            if (exportFolderPath.StartsWith("Assets"))
            {
                pendingHandsBinPath = destinationPath;
                pendingExportFolder = exportFolderPath;
            }
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

#if UNITY_EDITOR
    private static void ConvertHandsBinToAsset(
        string handsBinPath,
        string exportFolderPath
    )
    {
        try
        {
            Debug.Log("Converting:");
            Debug.Log(handsBinPath);

            Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();

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
                    "XRHandRecordingBlob type not found."
                );

                return;
            }

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
                    "TryReadCaptureSequenceFromDisk method not found."
                );

                return;
            }

            object[] args = new object[]
            {
                handsBinPath,
                null
            };

            bool success =
                (bool)readMethod.Invoke(null, args);

            if (!success)
            {
                Debug.LogError(
                    "Failed to parse handsbin."
                );

                return;
            }

            UnityEngine.Object captureSequence =
                args[1] as UnityEngine.Object;

            if (captureSequence == null)
            {
                Debug.LogError(
                    "Capture sequence is null."
                );

                return;
            }

            string relativeFolder =
                exportFolderPath.Replace("\\", "/");

            string assetPath =
                relativeFolder + "/" +
                Path.GetFileNameWithoutExtension(
                    handsBinPath
                ) +
                ".asset";

            assetPath = assetPath.Replace("\\", "/");

            AssetDatabase.CreateAsset(
                captureSequence,
                assetPath
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("ASSET CREATED:");
            Debug.Log(assetPath);

            EditorGUIUtility.PingObject(
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    assetPath
                )
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }
#endif
}