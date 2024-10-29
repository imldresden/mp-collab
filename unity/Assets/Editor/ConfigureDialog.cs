using IMLD.MixedReality.Avatars;
using IMLD.MixedReality.Network;
using IMLD.MixedReality.Utils;
using System;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ConfigureDialog : EditorWindow
{
    private Toggle _toggleNetworking, _toggleWorldAnchor, _toggleLogging, _toggleConfig;

    [SerializeField]
    private VisualTreeAsset _UXMLTree;

    private const string WARNING_SUFFIX = " <color=\"yellow\">\u26A0</color>";
    private StyleColor originalColor = Color.white;

    public static void ShowWindow()
    {
        ConfigureDialog wnd = GetWindow<ConfigureDialog>(true);
        wnd.titleContent = new GUIContent("ConfigureDialog");
    }

    public void CreateGUI()
    {
        // setup visual tree from uxml file
        VisualElement root = rootVisualElement;
        root.Add(_UXMLTree.Instantiate());

        // setup callback for template selection
        var dropdownTemplate = root.Q<DropdownField>("dropdownTemplate");
        dropdownTemplate.RegisterValueChangedCallback(DropdownTemplateChanged);

        // setup callback for button click
        var buttonConfirm = root.Q<Button>("buttonConfirm");
        buttonConfirm.clicked += ButtonConfirmClicked;

        // make only optional components active
        root.Q<Toggle>("toggleNetworking").SetEnabled(false);
        root.Q<Toggle>("toggleWorldAnchor").SetEnabled(false);
        root.Q<Toggle>("toggleAvatars").SetEnabled(false);
        root.Q<Toggle>("toggleRemoteKinect").SetEnabled(false);
        root.Q<Toggle>("toggleLocalKinect").SetEnabled(false);
        root.Q<Toggle>("toggleLogging").SetEnabled(true);
        root.Q<Toggle>("toggleConfig").SetEnabled(true);

        // set default values
        root.Q<Toggle>("toggleNetworking").value = true;
        root.Q<Toggle>("toggleWorldAnchor").value = true;
        root.Q<Toggle>("toggleAvatars").value = true;
        root.Q<Toggle>("toggleRemoteKinect").value = true;
        root.Q<Toggle>("toggleLocalKinect").value = false;
        root.Q<Toggle>("toggleLogging").value = true;
        root.Q<Toggle>("toggleConfig").value = true;
    }


    private void ButtonConfirmClicked()
    {
        VisualElement root = rootVisualElement;
        GameObject CoreGO, NetworkingGO, SceneOriginGO, AvatarsGO, KinectsGO, LoggingGO, ConfigGO;

        // spawn core GO
        CoreGO = (GameObject)PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Modules/Core/Prefabs/MixedPresenceToolkit.prefab", typeof(GameObject)));
        ServiceManager ServiceManager = CoreGO.GetComponent<ServiceManager>();

        // spawn networking service?
        if (root.Q<Toggle>("toggleNetworking").value == true)
        {
            NetworkingGO = (GameObject)PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Modules/Network/Prefabs/NetworkingService.prefab", typeof(GameObject)), CoreGO.transform);
            ServiceManager.NetworkServiceManager = NetworkingGO.GetComponent<NetworkServiceManager>();
        }

        // spawn world anchor service?
        if (root.Q<Toggle>("toggleWorldAnchor").value == true)
        {
            SceneOriginGO = (GameObject)PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Modules/Core/Prefabs/SceneOrigin.prefab", typeof(GameObject)));
        }

        // spawn avatars service?
        if (root.Q<Toggle>("toggleAvatars").value == true)
        {
            AvatarsGO = (GameObject)PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Modules/Avatars/Prefabs/AvatarService.prefab", typeof(GameObject)), CoreGO.transform);
            ServiceManager.StudyManager = AvatarsGO.GetComponent<StudyManager>();
        }

        // spawn kinect service?
        if (root.Q<Toggle>("toggleRemoteKinect").value == true || root.Q<Toggle>("toggleLocalKinect").value == true)
        {
            KinectsGO = (GameObject)PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Modules/Avatars/Prefabs/KinectService.prefab", typeof(GameObject)), CoreGO.transform);
            ServiceManager.KinectManager = KinectsGO.GetComponent<KinectManager>();

            if (root.Q<Toggle>("toggleLocalKinect").value == false)
            {
                KinectsGO.GetComponent<KinectTrackingController>().enabled = false;
            }
        }

        // spawn avatars service?
        if (root.Q<Toggle>("toggleAvatars").value == true)
        {
            LoggingGO = (GameObject)PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Modules/Core/Prefabs/LoggingService.prefab", typeof(GameObject)), CoreGO.transform);
            ServiceManager.Log = LoggingGO.GetComponent<AbstractLog>();
        }

        // spawn avatars service?
        if (root.Q<Toggle>("toggleAvatars").value == true)
        {
            ConfigGO = (GameObject)PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Modules/Core/Prefabs/ConfigService.prefab", typeof(GameObject)), CoreGO.transform);
            ServiceManager.Config = ConfigGO.GetComponent<Config>();
        }

        // close dialog
        Close();
    }

    //private void SetWarning(Toggle element, bool warning)
    //{
    //    if (element == null)
    //    {
    //        return;
    //    }
    //    if (warning)
    //    {
    //        if (!element.label.EndsWith(WARNING_SUFFIX))
    //        {
    //            element.label = element.label + WARNING_SUFFIX;
    //            originalColor = element.style.color;
    //            rootVisualElement.Q<HelpBox>().style.display = DisplayStyle.Flex;
    //        }
    //    }
    //    else
    //    {
    //        if (element.label.EndsWith(WARNING_SUFFIX))
    //        {
    //            element.label = element.label.Substring(0, element.label.Length - WARNING_SUFFIX.Length);
    //            rootVisualElement.Q<HelpBox>().style.display = DisplayStyle.None;
    //        }
    //    }

    //    //var csharpHelpBox = new HelpBox("Necessary dependencies selected.", HelpBoxMessageType.Warning);
    //    //rootVisualElement.Add(csharpHelpBox);
    //}

    private void DropdownTemplateChanged(ChangeEvent<string> evt)
    {
        VisualElement root = rootVisualElement;
        switch (evt.newValue)
        {
            case "HMD Client":
                root.Q<Toggle>("toggleNetworking").value = true;
                root.Q<Toggle>("toggleWorldAnchor").value = true;
                root.Q<Toggle>("toggleAvatars").value = true;
                root.Q<Toggle>("toggleRemoteKinect").value = true;
                root.Q<Toggle>("toggleLocalKinect").value = false;
                root.Q<Toggle>("toggleLogging").value = false;
                root.Q<Toggle>("toggleConfig").value = true;
                break;
            case "Camera Client":
                root.Q<Toggle>("toggleNetworking").value = true;
                root.Q<Toggle>("toggleWorldAnchor").value = false;
                root.Q<Toggle>("toggleAvatars").value = false;
                root.Q<Toggle>("toggleRemoteKinect").value = false;
                root.Q<Toggle>("toggleLocalKinect").value = true;
                root.Q<Toggle>("toggleLogging").value = false;
                root.Q<Toggle>("toggleConfig").value = false;
                break;
            case "Server":
                root.Q<Toggle>("toggleNetworking").value = true;
                root.Q<Toggle>("toggleWorldAnchor").value = false;
                root.Q<Toggle>("toggleAvatars").value = false;
                root.Q<Toggle>("toggleRemoteKinect").value = false;
                root.Q<Toggle>("toggleLocalKinect").value = true;
                root.Q<Toggle>("toggleLogging").value = false;
                root.Q<Toggle>("toggleConfig").value = false;
                break;
        }
    }

}
