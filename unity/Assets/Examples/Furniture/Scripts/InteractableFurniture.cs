using IMLD.MixedReality.Core;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.XR.CoreUtils;
using UnityEngine;

public class InteractableFurniture : InteractableObject
{
    InteractableManager fpManager;
    GameObject ground; 
    float distance;
    Material material;
    bool closePlaneProximity = false;
    float floorDistance = 0;
    Vector3 pivotOffset = Vector3.zero; 
    Transform origin;
    float height;
    float center;

    void Start()
    {
        // find FloorplanManager

        //fpManager = FindObjectOfType<InteractableManager>();
        //ground = fpManager.floorPlan;
        //distance = fpManager.snappingDistance;      
        //material = fpManager.furnitureMaterial;
        //origin = fpManager.InteractableParentTransform;

        prepareGameObject();

        //var pointerHandler = this.transform.gameObject.AddComponent<PointerHandler>();

        
        //    pointerHandler.OnPointerDragged.AddListener((e) =>
        //    {
        //        if (e.Pointer is ShellHandRayPointer SpherePointer)
        //        {

        //            // No Y values lower than the plane 
        //            if (!closePlaneProximity)
        //            {
        //                this.transform.parent = ((SpherePointer)(e.Pointer)).transform;
        //            }
        //        }
        //    });
        //    pointerHandler.OnPointerUp.AddListener((e) =>
        //    {
        //        if (e.Pointer is SpherePointer)
        //        {
        //            this.transform.parent = origin;
        //        }
        //    });
    }

    void prepareGameObject()
    {
        this.transform.gameObject.AddComponent<Rigidbody>().useGravity = false;
        this.transform.gameObject.GetComponent<Rigidbody>().isKinematic = true;
        this.transform.gameObject.AddComponent<MeshRenderer>().material = material;

        combineMeshes();

        Mesh mesh = this.transform.gameObject.GetComponent<MeshFilter>().mesh;


        this.transform.gameObject.AddComponent<BoxCollider>().size = mesh.bounds.size;
        this.transform.gameObject.GetComponent<BoxCollider>().transform.position = this.transform.position;

        Vector3 colliderLocalPos = this.transform.gameObject.GetComponent<BoxCollider>().center;

        pivotOffset = new Vector3(colliderLocalPos.x * this.transform.localScale.x, colliderLocalPos.y * this.transform.localScale.y, colliderLocalPos.z * this.transform.localScale.z);


        height = mesh.bounds.size.y * this.transform.localScale.y;
        center = height / 2;
        this.transform.gameObject.AddComponent<NearInteractionGrabbable>();
    }

    void combineMeshes()
    {
        MeshFilter[] meshFilters = this.transform.gameObject.GetComponentsInChildren<MeshFilter>();
        //Debug.Log(this.transform.gameObject.name + " meshfilters: + " + meshFilters.Length);
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        //this.transform.gameObject.GetComponent<MeshRenderer>().
        int i = 0;
        while (i < meshFilters.Length)
        {
            //Debug.Log("i: " + i + ", GO: "+ this.transform.gameObject.name + " meshfilters-names: + " + meshFilters[i].transform.gameObject.name);
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
            i++;
        }

        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combine);
        this.transform.gameObject.AddComponent<MeshFilter>();
        this.transform.GetComponent<MeshFilter>().sharedMesh = mesh;
        this.transform.gameObject.SetActive(true);
    }
}
