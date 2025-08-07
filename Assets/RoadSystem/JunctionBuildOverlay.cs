using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "Junction Builder", true)]
public class JunctionBuildOverlay : Overlay
{

    public static event Action OnChangeValueEvent;

    Label selectionInfoLabel;
    Button BuildJunctionButton;
    VisualElement SliderArea;
    
    public override VisualElement CreatePanelContent()
    {
        var root = new VisualElement();

        selectionInfoLabel = new Label("No selection");
        selectionInfoLabel.style.whiteSpace = WhiteSpace.Normal; // Allows multiline
        root.Add(selectionInfoLabel);

        // this Q<>() just finds the <VisualElement name="SliderArea"> in your UXML
        SliderArea = new VisualElement();
        root.Add(SliderArea);


        BuildJunctionButton = new Button(OnBuildJunction) { text = "Build Junction" };
        BuildJunctionButton.style.width = 120;
        BuildJunctionButton.style.height = 30;

        root.Add(BuildJunctionButton);


        // Subscribe once per overlay instance
        SplineSelection.changed += OnSelectionChanged;
        UpdateSelectionInfo(); // Call once on init, or hook up to events externally
        return root;
    }
    
    public void ShowIntersection(Intersection intersection)
    {
        selectionInfoLabel.text = "Selected Intersection";
        BuildJunctionButton.visible = false;

        SliderArea.Clear();

        for (int i = 0; i < intersection.innerCurveStrengthList.Count; i++)
        {
            int val = i;
            Slider innerSlider = new Slider($"Curve {i}", 0, 1, SliderDirection.Horizontal);
            innerSlider.labelElement.style.minWidth = 60;
            innerSlider.labelElement.style.maxWidth = 80;
            innerSlider.value = intersection.innerCurveStrengthList[i];
            innerSlider.RegisterValueChangedCallback((x) =>
            {
                Debug.Log(intersection.innerCurveStrengthList[val]);
                intersection.innerCurveStrengthList[val] = x.newValue;
                OnChangeValueEvent.Invoke();
            });

            Slider outerSlider = new Slider($"O Curve {i}", 0, 1, SliderDirection.Horizontal);
            outerSlider.labelElement.style.minWidth = 60;
            outerSlider.labelElement.style.maxWidth = 80;
            outerSlider.value = intersection.outerCurveStrengthList[i];
            outerSlider.RegisterValueChangedCallback((x) =>
            {
                Debug.Log(intersection.outerCurveStrengthList[val]);
                intersection.outerCurveStrengthList[val] = x.newValue;
                OnChangeValueEvent.Invoke();
            });

            SliderArea.Add(innerSlider);
            SliderArea.Add(outerSlider);

        }

        for (int i = 0; i < intersection.outerCurveStrengthList.Count; i++)
        {
            int val = i;
            
        }

    }


    private void OnBuildJunction()
    {
        List<SelectedSplineElementInfo> selection = SplineToolUtility.GetSelection();

        Intersection intersection = new Intersection();
        foreach (SelectedSplineElementInfo info in selection)
        {
            SplineContainer container = (SplineContainer) info.target;
            Spline spline = container.Splines[info.targetIndex];
            var knot = spline.Knots.ToList()[info.knotIndex];
            intersection.AddJunction(info.targetIndex, info.knotIndex, spline, knot);
        }
        Debug.Log(Selection.activeObject);
        //Debug.Log(Selection.activeObject.GetComponent<SplineRoad>());
        
        Selection.activeObject.GetComponent<AdvancedSplineRoad>().AddJunction(intersection);
    }

    private void OnSelectionChanged()
    {
        //Debug.Log("Here");
        UpdateSelectionInfo();
    }
    private void ClearSelectionInfo()
    {
        selectionInfoLabel.text = string.Empty; 
        SliderArea.Clear();
    }

    private void UpdateSelectionInfo()
    {
        ClearSelectionInfo();
        //Debug.Log("Get info....");
        List<SelectedSplineElementInfo> infos = SplineToolUtility.GetSelection();
        if (infos.Count > 0 && BuildJunctionButton.visible == false)
        {
            BuildJunctionButton.visible = true;
        }
        foreach (SelectedSplineElementInfo info in infos)
        {
            selectionInfoLabel.text += $"Spline {info.targetIndex}, Knot {info.knotIndex} \n";

            List<Intersection> intersections;
            if (Selection.activeObject.GetComponent<SplineRoad>() != null)
            {
                intersections = Selection.activeObject.GetComponent<SplineRoad>().intersections;
            } else
            {
                intersections = Selection.activeObject.GetComponent<AdvancedSplineRoad>().intersections;
            }



            for (int i = 0; i < intersections.Count; i++)
                {
                    for (int j = 0; j < intersections[i].junctions.Count; j++)
                    {
                        JunctionInfo juncInfo = intersections[i].junctions[j];
                        if (juncInfo.splineIndex == info.targetIndex && juncInfo.knotIndex == info.knotIndex)
                        {
                            Debug.Log("Calling ShowIntersection");
                            ShowIntersection(intersections[i]);
                            return;
                        }
                    }
                }
        }


    }
}

