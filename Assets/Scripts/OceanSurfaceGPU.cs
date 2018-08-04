using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(OceanDisplacement))]
public class OceanSurfaceGPU : MonoBehaviour
{
    // no need for UV at the moment as we use normalized position
    // no normals as they'll be read from a texture
    struct Point
    {
        public Vector3 position;
    }

    [SerializeField] int resolution;
    [SerializeField] Material renderMaterial;

    ComputeBuffer geometryBuffer;
    OceanDisplacement displacement;
    int resolution_ = -1;

    void OnEnable()
    {
        displacement = GetComponent<OceanDisplacement>();
        CheckGeometry();
    }

    void OnValidate()
    {
        CheckGeometry();
    }

    void OnDisable()
    {
        ReleaseGeometry();
    }

    void OnRenderObject()
    {
        if (geometryBuffer != null && renderMaterial != null)
        {
            renderMaterial.SetBuffer("_Points", geometryBuffer);
            renderMaterial.SetMatrix("_ModelMatrix", transform.localToWorldMatrix);
            renderMaterial.SetTexture("_PositionTex", displacement.positionTexture);
            renderMaterial.SetTexture("_NormalTex", displacement.normalTexture);
            renderMaterial.SetPass(0);
            Graphics.DrawProcedural(MeshTopology.Triangles, geometryBuffer.count, 1);
        }
    }

    void CheckGeometry()
    {
        if (resolution < 2 || resolution == resolution_)
        {
            return;
        }

        if (geometryBuffer != null)
        {
            ReleaseGeometry();
        }
        resolution_ = resolution;
        geometryBuffer = MakePlanarGeometry(resolution);
    }

    void ReleaseGeometry()
    {
        resolution_ = -1;
        if (geometryBuffer != null)
        {
            geometryBuffer.Dispose();
            geometryBuffer = null;
        }
    }

    static ComputeBuffer MakePlanarGeometry(int resolution)
    {
        Assert.IsTrue(resolution > 1, "resolution must at least be 2");

        var numPoints = (resolution - 1) * (resolution - 1) * 6; // 2 triangles = 6 points per cell
        var points = new Point[numPoints];

        int index = 0;
        var dx = Vector3.right / (float)(resolution - 1);
        var dy = Vector3.forward / (float)(resolution - 1);

        for (int y = 0; y != resolution - 1; ++y)
        {
            for (int x = 0; x != resolution - 1; ++x)
            {
                // triangle 1
                points[index++].position = dx * x + dy * y;
                points[index++].position = dx * x + dy * (y + 1);
                points[index++].position = dx * (x + 1) + dy * (y + 1);
                // triangle 2
                points[index++].position = dx * x + dy * y;
                points[index++].position = dx * (x + 1) + dy * (y + 1);
                points[index++].position = dx * (x + 1) + dy * y;
            }
        }

        var buf = new ComputeBuffer(numPoints, Marshal.SizeOf(typeof(Point)));
        buf.SetData(points);
        return buf;
    }
}