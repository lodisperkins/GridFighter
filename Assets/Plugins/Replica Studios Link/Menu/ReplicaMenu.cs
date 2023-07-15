#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.VersionControl;

using System.Security.Cryptography;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System;
using System.Threading.Tasks;

using UnityEditor;
using UnityEditor.UI;

namespace Replica { 
    public class ReplicaMenuSetup : EditorWindow
    {
        [MenuItem("Replica/Documentation")]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://help.replicastudios.com/unity-editor-integration-setup");
        }
    }
}
#endif