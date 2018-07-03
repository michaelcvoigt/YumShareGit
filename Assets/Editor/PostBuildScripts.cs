using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;
using UnityEditor.iOS.Xcode;
using System.IO;

public class PostBuildScripts {

[PostProcessBuild]
    public static void AddPhotoLibraryUsageKey(BuildTarget buildTarget, string pathToBuiltProject) {

        if (buildTarget == BuildTarget.iOS) {

            // Get plist
            string plistPath = pathToBuiltProject + "/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            // Get root
            PlistElementDict rootDict = plist.root;

            // Ensure that photo usage description is always added
            var buildKey = "NSPhotoLibraryAddUsageDescription";
            rootDict.SetString(buildKey, "Photo library access needed to save screenshots");

            // Write to file
            File.WriteAllText(plistPath, plist.WriteToString());
        }
    }
}