using UnityEngine;
using UnityEngine.Rendering;

// ของใช้ร่วมของระบบบรรยากาศ (ควัน ฝุ่น เงา): สร้างวัสดุโปร่งใส + หากล้องที่กำลังแสดงภาพอยู่
public static class AtmosUtil
{
    /// <summary>วัสดุโปร่งใสแบบไม่รับแสง (URP Particles/Unlit) รองรับหมอกของฉาก</summary>
    /// <param name="shaderSource">วัสดุต้นแบบในโปรเจค (ทำให้ตอน build ไม่โดนตัด shader ทิ้ง)</param>
    public static Material TransparentMat(Texture tex, bool additive = false, bool lit = false, Material shaderSource = null)
    {
        Shader sh = shaderSource ? shaderSource.shader : null;
        if (!sh && lit) sh = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
        if (!sh) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (!sh) sh = Shader.Find("Sprites/Default");
        var m = new Material(sh) { name = "Atmos_" + (tex ? tex.name : "mat") };
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (additive) m.EnableKeyword("_BLENDMODE_ADD"); else m.DisableKeyword("_BLENDMODE_ADD");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
        return m;
    }

    public static void SetColor(Renderer r, MaterialPropertyBlock mpb, Color c)
    {
        r.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", c);
        mpb.SetColor("_Color", c);
        r.SetPropertyBlock(mpb);
    }

    /// <summary>กล้องที่กำลังแสดงภาพ (ตอนคัตซีนจะเป็นกล้องคัตซีน)</summary>
    public static Camera ActiveCamera()
    {
        Camera best = null;
        foreach (var c in Camera.allCameras)
            if (c.isActiveAndEnabled && c.targetTexture == null && (!best || c.depth > best.depth)) best = c;
        return best ? best : Camera.main;
    }

    static Mesh quad;
    /// <summary>สี่เหลี่ยม 1x1 จุดหมุนตรงกลางล่าง (ตั้งบนพื้นได้)</summary>
    public static Mesh BottomQuad()
    {
        if (quad) return quad;
        quad = new Mesh { name = "AtmosQuad" };
        quad.vertices = new[] { new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(-0.5f, 1, 0), new Vector3(0.5f, 1, 0) };
        quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        quad.RecalculateNormals(); quad.RecalculateBounds();
        return quad;
    }

    /// <summary>หันหน้าเข้ากล้องเฉพาะแกนนอน (ตั้งตรงเสมอ)</summary>
    public static void FaceYaw(Transform t, Vector3 camPos)
    {
        Vector3 d = t.position - camPos; d.y = 0f;
        if (d.sqrMagnitude > 0.0001f) t.rotation = Quaternion.LookRotation(d);
    }
}
