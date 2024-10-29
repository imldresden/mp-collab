using UnityEngine;
using UnityEditor;

namespace IMLD.MixedReality.Network
{
    [CustomPropertyDrawer(typeof(ConnectionInfo))]
    public class ConnectionInfoDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);
            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate rects
            var statusRect = new Rect(position.x, position.y, 90, position.height);
            var lastSeenRect = new Rect(position.x + 65, position.y, position.width - 95, position.height);

            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            GUI.enabled = false;
            EditorGUI.PropertyField(statusRect, property.FindPropertyRelative("Status"), GUIContent.none);
            EditorGUI.PropertyField(lastSeenRect, property.FindPropertyRelative("LastSeen"), GUIContent.none);
            GUI.enabled = true;

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}