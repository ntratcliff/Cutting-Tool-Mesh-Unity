﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class CuttingTool : MonoBehaviour
{
    [SerializeField]
    Material mat;

    private SlicePlane slp;
    private Vector3 lastMousePos;

    private Vector3 center;
    private Vector3 u;
    private Vector3 v;
    private Vector3 n;

    List<Vector3> points;
    List<Vector3> leftpoints;
    List<Vector3> rightpoints;

    private bool hasClicked;
    private int lftBtn;
    private float distance;

    // Use this for initialization
    void Start()
    {
        slp = new SlicePlane();
        points = new List<Vector3>();
        leftpoints = new List<Vector3>();
        rightpoints = new List<Vector3>();

        hasClicked = false;
        lastMousePos = Vector2.zero;
        lftBtn = 0;
        distance = 100.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasClicked && Input.GetMouseButtonDown(lftBtn))
        {
            hasClicked = true;
            lastMousePos = Input.mousePosition;
        }

        if (hasClicked && Input.GetMouseButtonUp(lftBtn))
        {
            hasClicked = false;
            Vector3 sliceCenter = (lastMousePos + Input.mousePosition) / 2.0f;

            RaycastHit hit, unusedHit;
            Ray rayCenter = Camera.main.ScreenPointToRay(sliceCenter);
            Ray rayP1 = Camera.main.ScreenPointToRay(lastMousePos);
            Ray rayP2 = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(rayCenter, out hit)
                && !Physics.Raycast(rayP1, out unusedHit)
                && !Physics.Raycast(rayP2, out unusedHit))
            {
                points.Clear();
                leftpoints.Clear();
                rightpoints.Clear();
                slp.slVectors.Clear();

                center = hit.point;
                slp.setPoints(
                    Camera.main.ScreenToWorldPoint(new Vector3(lastMousePos.x, lastMousePos.y, 1.0f)),
                    Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1.0f)),
                    Camera.main.ScreenToWorldPoint(new Vector3(lastMousePos.x, lastMousePos.y, 1.0f) + Camera.main.transform.forward));

                MeshFilter mf = hit.transform.GetComponent<MeshFilter>();
                float x1 = slp.plane.normal.x;
                float y1 = slp.plane.normal.y;
                float z1 = slp.plane.normal.z;
                float d1 = -(x1 * slp.a.x + y1 * slp.a.y + z1 * slp.a.z);

                Vector3 finalPoint1;
                Vector3 finalPoint2;
                Vector3 finalPoint;
                for (int i = 0; i < mf.mesh.triangles.Length; i += 3)
                {

                    Vector3 normalTriangle = hit.transform.TransformDirection(mf.mesh.normals[mf.mesh.triangles[i]]);
                    Vector3 sliceDir = Vector3.Cross(normalTriangle, slp.plane.normal);
                    Debug.Log(sliceDir);
                    Vector3 V1 = hit.transform.TransformPoint(mf.mesh.vertices[mf.mesh.triangles[i]]);
                    Vector3 V2 = hit.transform.TransformPoint(mf.mesh.vertices[mf.mesh.triangles[i + 1]]);
                    Vector3 V3 = hit.transform.TransformPoint(mf.mesh.vertices[mf.mesh.triangles[i + 2]]);

                    Vector3 V2V1 = V1 - V2;
                    Vector3 V2V3 = V3 - V2;
                    Vector3 V1V3 = V3 - V1;

                    float x2 = normalTriangle.x;
                    float y2 = normalTriangle.y;
                    float z2 = normalTriangle.z;
                    float d2 = -(x2 * V1.x + y2 * V1.y + z2 * V1.z);

                    //equation plane N1x(x - xA) + N1y(y - yA) + N1z(z - zA) + d1 = 0 | A e plane
                    //equation plane N2x(x - xB) + N2y(y - yB) + N2z(z - zB) + d2 = 0 | B e plane
                    //where X = 0.0f
                    //find intersection point p with two planes
                    float x = 0.0f;
                    float z = ((y2 / y1) * d1 - d2) / (z2 - z1 * y2 / y1);
                    float y = (-z1 * z - d1) / y1;

                    /*slp.AddNewSlVector(V2, V1 - V2);
                    slp.AddNewSlVector(V2, V3 - V2);
                    slp.AddNewSlVector(V1, V3 - V1);*/

                    Vector3 pointOnSliceVec = new Vector3(x, y, z);
                    if(Mathf.Abs(Vector3.Dot(normalTriangle, slp.plane.normal))<0.1f)
                    {
                        Debug.Log("    PB      ");
                    }
                    else slp.AddNewSlVector(Vector3.zero, sliceDir);

                    if (inter(out finalPoint, V2, V1, pointOnSliceVec, sliceDir))
                    {
                        slp.AddNewSlVector(finalPoint, Vector3.zero);
                        Debug.Log("f1 " + finalPoint);
                    }
                    if (inter(out finalPoint, V3, V2, pointOnSliceVec, sliceDir))
                    {
                        slp.AddNewSlVector(finalPoint, Vector3.zero);
                        Debug.Log("f2 " + finalPoint);
                    }
                    if (inter(out finalPoint, V1, V3, pointOnSliceVec, sliceDir))
                    {
                        slp.AddNewSlVector(finalPoint, Vector3.zero);
                        Debug.Log("f3 " + finalPoint);

                    }

                }

                for (int i = 0; i < mf.mesh.vertexCount; i++)
                {

                    Vector3 vertex = hit.transform.TransformPoint(mf.mesh.vertices[i]);
                    if (slp.plane.GetSide(vertex))
                    {
                        if (!leftpoints.Contains(vertex))
                            leftpoints.Add(vertex);
                    }
                    else
                    {
                        if (!rightpoints.Contains(vertex))
                            rightpoints.Add(vertex);
                    }
                }
            }

        }

    }
    void OnPostRender()
    {
        if (hasClicked)
        {
            drawGLLine();
        }
    }
    void drawGLLine()
    {
        if (!mat)
        {
            Debug.LogError("Please Assign a material on the inspector");
            return;
        }
        GL.PushMatrix();
        mat.SetPass(0);
        GL.LoadOrtho();

        GL.Begin(GL.LINES);
        GL.Color(Color.red);
        GL.Vertex(new Vector3(lastMousePos.x / Screen.width, lastMousePos.y / Screen.height, 0));
        GL.Vertex(new Vector3(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height, 0));
        GL.End();

        GL.PopMatrix();
    }
    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(center, 0.1f);

        Gizmos.color = Color.magenta;
        foreach (Vector3 p in points)
        {
            Gizmos.DrawSphere(p, 0.025f);
        }
        Gizmos.color = Color.red;
        foreach (Vector3 p in leftpoints)
        {
            Gizmos.DrawSphere(p, 0.025f);
        }
        Gizmos.color = Color.blue;
        foreach (Vector3 p in rightpoints)
        {
            Gizmos.DrawSphere(p, 0.025f);
        }

        if (slp != null)
            slp.drawOnGizmos();
    }

    public bool inter(out Vector3 ptIntersection, Vector3 A1, Vector3 A2, Vector3 B1, Vector3 vB)
    {
        Vector3 vA = A2 - A1;

        float u = (vA.x * (B1.y - A1.y) + vA.y * (A1.x - B1.x)) / (vA.y * vB.x - vA.x * vB.y);

        ptIntersection.x = B1.x + u * vB.x;
        ptIntersection.y = B1.y + u * vB.y;
        ptIntersection.z = B1.z + u * vB.z;

        float dist = vA.sqrMagnitude;
        float dist1 = (A1 - ptIntersection).sqrMagnitude;
        float dist2 = (A2 - ptIntersection).sqrMagnitude;

        if (dist1 < dist && dist2 < dist)
            return true;
        else
            return false;
        /*pA.x+t*vA.x = pB.x+u*vB.x
          pA.y+t*vA.y = pB.y+u*vB.y
          pA.z+t*vA.z = pB.z+u*vB.z

        t = (pB.x+u*vB.x - pA.x)/vA.x
        vA.y*(pB.x+u*vB.x - pA.x)/vA.x- u*vB.y = pB.y - pA.y
        vA.y*(pB.x+u*vB.x - pA.x) - vA.x*u*vB.y = vA.x*(pB.y - pA.y)
        vA.y*pB.x + vA.y*u*vB.x - vA.y*pA.x - vA.x*u*vB.y = vA.x*(pB.y - pA.y)
        vA.y*u*vB.x - vA.x*u*vB.y = vA.x*(pB.y - pA.y) + vA.y (pA.x - pB.x)
        u*(vA.y*vB.x -vA.x*vB.y) = ""
        u = (vA.x*(pB.y - pA.y) + vA.y (pA.x - pB.x))/(vA.y*vB.x - vA.x*vB.y)*/
    }



}
