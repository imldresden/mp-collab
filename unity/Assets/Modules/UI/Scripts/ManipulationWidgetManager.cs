using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.UI
{
    public class ManipulationWidgetManager : MonoBehaviour
    {
        List<GameObject> _widgetList = new List<GameObject>();
        public GameObject WidgetPrefab;
        public bool WidgetsEnabled = false;

        public GameObject CreateManipulationWidget(Transform targetTransform)
        {
            var widget = Instantiate(WidgetPrefab, transform);
            var widgetController = widget.GetComponent<ManipulationWidgetController>();

            if (widgetController != null)
            {
                widgetController.TargetTransform = targetTransform;
            }

            AddWidget(widget);
            return widget;
        }

        private void AddWidget(GameObject widget)
        {
            _widgetList.Add(widget);
        }

        public void DeleteWidget(GameObject widget)
        {
            _widgetList.Remove(widget);
            Destroy(widget);
        }

        public void ToggleWidgets()
        {
            // clear up
            foreach (GameObject widget in _widgetList)
            {
                if (widget != null)
                {
                    Destroy(widget);
                }
            }

            _widgetList.Clear();

            // activate/deactive
            WidgetsEnabled = !WidgetsEnabled;
        }
    }
}