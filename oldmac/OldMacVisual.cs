using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluidLove.OldMac
{
    // Doom-style sprite: a flat Ol' Mac that follows the monster's (hidden) body and turns to face
    // whichever camera is rendering, including the in-game video camera.
    public class OldMacVisual : MonoBehaviour
    {
        const float Height = 2.2f;
        static readonly List<OldMacVisual> Live = new List<OldMacVisual>();
        static bool hooked;
        static Mesh quadMesh;

        Bot bot;
        Renderer[] body;
        Transform quad;
        Material mat;
        Vector3 lastPos;
        Vector3 vel;
        bool faceLeft;
        bool walkSet;
        float animT;
        bool logged;

        void Start()
        {
            bot = GetComponentInChildren<Bot>(true);
            body = GetComponentsInChildren<Renderer>(true);

            if (quadMesh == null) quadMesh = MakeQuad();
            var go = new GameObject("OldMacSprite");
            go.AddComponent<MeshFilter>().sharedMesh = quadMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mat = MakeMat();
            mr.sharedMaterial = mat;
            quad = go.transform;
            lastPos = Anchor();

            Live.Add(this);
            if (!hooked) { hooked = true; RenderPipelineManager.beginCameraRendering += (ctx, cam) => { foreach (var v in Live) v.FaceCamera(cam); }; }
        }

        void OnDestroy()
        {
            Live.Remove(this);
            if (quad != null) Destroy(quad.gameObject);
            if (mat != null) Destroy(mat);
        }

        Vector3 Anchor()
        {
            if (bot != null && bot.groundTransform != null) return bot.groundTransform.position;
            if (bot != null && bot.centerTransform != null) return bot.centerTransform.position - Vector3.up * 1f;
            return transform.position;
        }

        void LateUpdate()
        {
            if (quad == null) return;
            foreach (var r in body) if (r != null && r.enabled) r.enabled = false; // the game re-enables culled bodies

            Vector3 p = Anchor();
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            vel = Vector3.Lerp(vel, (p - lastPos) / dt, 0.2f);
            lastPos = p;

            bool attacking = bot != null && (bot.attacking || bot.sinceAttack < 0.5f);
            float speed = new Vector3(vel.x, 0, vel.z).magnitude;
            bool moving = !attacking && speed > 0.4f;

            Texture2D[] set; float fps;
            if (attacking) { set = OldMacFactory.Stand; fps = 12f; }
            else if (moving) { set = OldMacFactory.Walk; fps = Mathf.Clamp(3f + speed * 2f, 4f, 12f); }
            else { set = OldMacFactory.Stand; fps = 3f; }
            walkSet = set == OldMacFactory.Walk;

            animT += dt * fps;
            var tex = set[(int)animT % set.Length];
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);

            float w = Height * tex.width / tex.height;
            quad.position = p + Vector3.up * (Height * 0.5f);
            quad.localScale = new Vector3(w, Height, 1f);

            if (!logged) { logged = true; OldMacPlugin.Log($"Ol' Mac spawned at {p}, bot {(bot != null ? "found" : "MISSING")}"); }
        }

        void FaceCamera(Camera cam)
        {
            if (quad == null || cam == null) return;
            Vector3 fwd = quad.position - cam.transform.position; fwd.y = 0;
            if (fwd.sqrMagnitude < 0.0001f) return;
            quad.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);

            // Walking frames face right; mirror when he moves left across this camera's view
            if (walkSet)
            {
                float side = Vector3.Dot(new Vector3(vel.x, 0, vel.z), cam.transform.right);
                if (Mathf.Abs(side) > 0.2f) faceLeft = side < 0;
            }
            var s = quad.localScale;
            s.x = Mathf.Abs(s.x) * (walkSet && faceLeft ? -1f : 1f);
            quad.localScale = s;
        }

        static Mesh MakeQuad()
        {
            var m = new Mesh { name = "OldMacQuad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateNormals(); m.RecalculateBounds();
            m.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            m.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return m;
        }

        static Material MakeMat()
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(sh) { name = "OldMacSprite" };
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", 0f);              // draw both sides
            m.SetFloat("_Smoothness", 0.1f);
            m.SetFloat("_Metallic", 0f);
            m.renderQueue = (int)RenderQueue.AlphaTest;
            return m;
        }
    }
}
