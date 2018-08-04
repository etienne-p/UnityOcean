using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class OceanDisplacement : MonoBehaviour
{
    [System.Serializable]
    struct WaveParams
    {
        public float orbitRadius;
        public float waveNumber;
        public float angularSpeed;
        public float maxStretch;
        public float maxDisplacement;
        public float displacementFactor;
        public Vector2 center;
    };

    [SerializeField] int resolution;
    [SerializeField] Material displacementMaterial;
    [SerializeField] int lookupSize;
    [SerializeField] WaveParams[] waveParams;
    [SerializeField] bool drawTexture;

    public RenderTexture positionTexture { get; private set;  }
    public RenderTexture normalTexture { get; private set; }
    Texture2D lookupTex;

    void OnEnable()
    {
        CheckRenderTextures();
        CheckLookupTables();
    }

    void OnValidate()
    {
        CheckRenderTextures();
        CheckLookupTables();
    }

    void OnDisable()
    {
        ReleaseRenderTextures();
        ReleaseLookupTables();
    }

    void Update()
    {
        UpdateSimulation();
    }

    void OnGUI()
    {
        if (drawTexture && positionTexture != null)
        {
            GUI.DrawTexture(new Rect(64, 64, positionTexture.width, positionTexture.height), positionTexture);
            GUI.DrawTexture(new Rect(64 + positionTexture.width, 64, normalTexture.width, normalTexture.height), normalTexture);
        }
    }

    void CheckRenderTextures()
    {
        if (positionTexture == null || positionTexture.width != resolution)
        {
            ReleaseRenderTextures();
            positionTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
            positionTexture.hideFlags = HideFlags.DontSave;
            positionTexture.useMipMap = true;
            normalTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
            normalTexture.hideFlags = HideFlags.DontSave;
        }
    }

    void ReleaseRenderTextures()
    {
        if (positionTexture != null)
        {
            positionTexture.Release();
            positionTexture = null;
        }
        if (normalTexture != null)
        {
            normalTexture.Release();
            normalTexture = null;
        }
    }

    void UpdateSimulation()
    {
        if (waveParams == null || waveParams.Length == 0)
        {
            return;
        }

        // zero out buffers
        Graphics.SetRenderTarget(positionTexture);
        // position will only hold positive values
        GL.Clear(true, true, Color.black);
        // normal may hold negative values
        Graphics.SetRenderTarget(normalTexture);
        GL.Clear(true, true, Color.white * 0.5f);

        displacementMaterial.SetTexture("_LookupTex", lookupTex);
        displacementMaterial.SetFloat("_Mix", 1.0f / (float)waveParams.Length);
        // render waves additively
        for (int i = 0; i != waveParams.Length; ++i)
        {
            UpdateWaveParams(displacementMaterial, waveParams[i]);

            Blit(new RenderBuffer[] {
                positionTexture.colorBuffer,
                normalTexture.colorBuffer }, 
                positionTexture.depthBuffer, displacementMaterial);
        }
    }

    static void Blit(RenderBuffer[] colorBuffers, RenderBuffer depthBuffer, Material material, int pass = 0)
    {
        Graphics.SetRenderTarget(colorBuffers, depthBuffer);

        GL.PushMatrix();

        material.SetPass(pass);

        GL.LoadOrtho();

        GL.Begin(GL.QUADS);

        GL.TexCoord(new Vector3(0, 0, 0));
        GL.Vertex3(0, 0, 0);

        GL.TexCoord(new Vector3(1, 0, 0));
        GL.Vertex3(1, 0, 0);

        GL.TexCoord(new Vector3(1, 1, 0));
        GL.Vertex3(1, 1, 0);

        GL.TexCoord(new Vector3(0, 1, 0));
        GL.Vertex3(0, 1, 0);

        GL.End();

        GL.PopMatrix();
    }

    static void UpdateWaveParams(Material material, WaveParams parms)
    {
        material.SetFloat("_OrbitRadius", parms.orbitRadius);
        material.SetFloat("_WaveNumber", parms.waveNumber);
        material.SetFloat("_AngularSpeed", parms.angularSpeed);
        material.SetFloat("_MaxStretch", parms.maxStretch);
        material.SetFloat("_MaxDisplacement", parms.maxDisplacement);
        material.SetFloat("_DisplacementFactor", parms.displacementFactor);
        material.SetVector("_Center", new Vector3(parms.center.x, 0, parms.center.y));
    }

    void CheckLookupTables()
    {
        if (lookupSize < 2)
        {
            return;
        }

        // orientation and displacement make use of linear interpolators that we implement using lookup textures
        if (lookupTex == null || lookupTex.width != lookupSize)
        {
            ReleaseLookupTables();

            float[] interpX = { -Mathf.PI, -Mathf.PI / 3.0f, 7 * Mathf.PI / 8.0f, Mathf.PI };
            float[] interpY = { 1, 0, 0, 1 };
            
            var pixels = new Color[lookupSize];

            for (int i = 0; i != lookupSize; ++i)
            {
                float t = i / (float)(lookupSize - 1);
                float phaseT = Mathf.Lerp(-Mathf.PI, Mathf.PI, t);
                float sample = Util.LinInterp(phaseT, interpX, interpY);
                pixels[i] = Color.white * sample;
            }

            lookupTex = new Texture2D(lookupSize, 1, TextureFormat.Alpha8, false);
            lookupTex.SetPixels(pixels);
        }
    }

    void ReleaseLookupTables()
    {
        if (lookupTex != null)
        {
            Texture2D.DestroyImmediate(lookupTex);
            lookupTex = null;
        }
    }
}
