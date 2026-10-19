using UnityEngine;
using UnityEditor;
using System.IO;

namespace CyberpunkSubwaySurfer.Editor
{
    public static class CyberAssetGenerator
    {
        private const string TEXTURE_DIR = "Assets/CyberpunkSubwaySurfer/Textures";
        private const string MATERIAL_DIR = "Assets/CyberpunkSubwaySurfer/Materials";

        [MenuItem("Cyberpunk/1. Generate Textures and Materials")]
        public static void GenerateAll()
        {
            EnsureDirectories();
            GenerateTextures();
            AssetDatabase.Refresh();
            GenerateMaterials();
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#00f0ff><b>[CyberAssetGenerator]</b> Successfully generated Cyberpunk textures and materials!</color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(TEXTURE_DIR)) Directory.CreateDirectory(TEXTURE_DIR);
            if (!Directory.Exists(MATERIAL_DIR)) Directory.CreateDirectory(MATERIAL_DIR);
        }

        public static void GenerateTextures()
        {
            EnsureDirectories();
            CreateHazardTexture(Path.Combine(TEXTURE_DIR, "tex_hazard_stripes.png"));
            CreateNeonGridTexture(Path.Combine(TEXTURE_DIR, "tex_neon_grid.png"));
            CreateSkyscraperWindowsTexture(Path.Combine(TEXTURE_DIR, "tex_skyscraper_windows.png"), false);
            CreateSkyscraperWindowsTexture(Path.Combine(TEXTURE_DIR, "tex_skyscraper_windows_emission.png"), true);
            CreateBillboardTexture1(Path.Combine(TEXTURE_DIR, "tex_billboard_cyber.png"));
            CreateBillboardTexture2(Path.Combine(TEXTURE_DIR, "tex_billboard_kanji.png"));
            CreateTrainSideTexture(Path.Combine(TEXTURE_DIR, "tex_train_stripes.png"));
            AssetDatabase.Refresh();
        }

        private static void CreateHazardTexture(string path)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color dark = new Color(0.1f, 0.1f, 0.12f, 1f);
            Color yellow = new Color(1.0f, 0.82f, 0.0f, 1f);

            int stripeWidth = 32;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int diag = (x + y) / stripeWidth;
                    tex.SetPixel(x, y, (diag % 2 == 0) ? yellow : dark);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateNeonGridTexture(string path)
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color bg = new Color(0.06f, 0.07f, 0.09f, 1f);
            Color gridLine = new Color(0.0f, 0.95f, 1.0f, 1f);
            Color subLine = new Color(0.12f, 0.25f, 0.35f, 1f);

            int mainStep = 64;
            int subStep = 16;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isMainLine = (x % mainStep == 0 || y % mainStep == 0 || x % mainStep == mainStep - 1 || y % mainStep == mainStep - 1);
                    bool isSubLine = (x % subStep == 0 || y % subStep == 0);

                    if (isMainLine)
                    {
                        tex.SetPixel(x, y, gridLine);
                    }
                    else if (isSubLine)
                    {
                        tex.SetPixel(x, y, subLine);
                    }
                    else
                    {
                        tex.SetPixel(x, y, bg);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateSkyscraperWindowsTexture(string path, bool emissionOnly)
        {
            int width = 512;
            int height = 512;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color wallBase = emissionOnly ? Color.black : new Color(0.08f, 0.09f, 0.12f, 1f);

            // Seed deterministic random
            System.Random rnd = new System.Random(42);

            int cols = 16;
            int rows = 32;
            int cellW = width / cols;
            int cellH = height / rows;

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, wallBase);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    // 65% chance window is on
                    bool isLit = rnd.NextDouble() < 0.65;
                    if (!isLit) continue;

                    Color winColor;
                    double colorRoll = rnd.NextDouble();
                    if (colorRoll < 0.50)
                    {
                        // Cyan neon window
                        winColor = new Color(0.0f, 0.85f, 1.0f, 1.0f);
                    }
                    else if (colorRoll < 0.80)
                    {
                        // Magenta / Purple window
                        winColor = new Color(1.0f, 0.1f, 0.6f, 1.0f);
                    }
                    else
                    {
                        // Amber / Orange window
                        winColor = new Color(1.0f, 0.75f, 0.2f, 1.0f);
                    }

                    int startX = c * cellW + 6;
                    int endX = (c + 1) * cellW - 6;
                    int startY = r * cellH + 4;
                    int endY = (r + 1) * cellH - 4;

                    for (int py = startY; py < endY; py++)
                    {
                        for (int px = startX; px < endX; px++)
                        {
                            tex.SetPixel(px, py, winColor);
                        }
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateBillboardTexture1(string path)
        {
            int width = 512;
            int height = 256;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color bg = new Color(0.05f, 0.03f, 0.08f, 1f);
            Color borderCyan = new Color(0.0f, 0.95f, 1.0f, 1f);
            Color pink = new Color(1.0f, 0.05f, 0.55f, 1f);
            Color yellow = new Color(1.0f, 0.9f, 0.1f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Scanline pattern
                    float scanline = (y % 4 == 0) ? 0.85f : 1.0f;
                    tex.SetPixel(x, y, bg * scanline);

                    // Glowing neon outer frame
                    if (x < 10 || x >= width - 10 || y < 10 || y >= height - 10)
                    {
                        tex.SetPixel(x, y, borderCyan);
                    }
                    // Corner accents
                    if ((x < 30 || x > width - 30) && (y < 30 || y > height - 30))
                    {
                        tex.SetPixel(x, y, pink);
                    }
                }
            }

            // Draw stylized futuristic logo lines / glyph bars
            DrawBlock(tex, 60, 100, 392, 50, pink);
            DrawBlock(tex, 70, 110, 372, 30, yellow);
            // Digital glitch stripes
            DrawBlock(tex, 100, 180, 120, 15, borderCyan);
            DrawBlock(tex, 250, 180, 180, 15, borderCyan);
            DrawBlock(tex, 140, 60, 240, 12, borderCyan);

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateBillboardTexture2(string path)
        {
            int width = 512;
            int height = 256;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color bg = new Color(0.03f, 0.05f, 0.09f, 1f);
            Color cyan = new Color(0.0f, 1.0f, 0.9f, 1f);
            Color purple = new Color(0.7f, 0.1f, 1.0f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, bg);
                    if (x < 8 || x >= width - 8 || y < 8 || y >= height - 8)
                        tex.SetPixel(x, y, purple);
                }
            }

            // Draw holographic arrow signs pointing forward (>>>)
            DrawChevron(tex, 140, 128, 40, cyan);
            DrawChevron(tex, 256, 128, 40, cyan);
            DrawChevron(tex, 372, 128, 40, cyan);

            // Sub-bars
            DrawBlock(tex, 80, 40, 352, 14, purple);
            DrawBlock(tex, 80, 202, 352, 14, purple);

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateTrainSideTexture(string path)
        {
            int width = 512;
            int height = 128;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color hull = new Color(0.12f, 0.14f, 0.18f, 1f);
            Color neonCyan = new Color(0.0f, 0.95f, 1.0f, 1f);
            Color darkGlass = new Color(0.05f, 0.08f, 0.12f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, hull);
                }
            }

            // Continuous speed stripe
            DrawBlock(tex, 0, 30, width, 12, neonCyan);

            // Passenger window strip
            DrawBlock(tex, 40, 60, width - 80, 36, darkGlass);
            for (int w = 60; w < width - 100; w += 50)
            {
                DrawBlock(tex, w, 66, 32, 24, new Color(0.0f, 0.8f, 1.0f, 0.9f));
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void DrawBlock(Texture2D tex, int startX, int startY, int w, int h, Color col)
        {
            for (int y = startY; y < startY + h && y < tex.height; y++)
            {
                for (int x = startX; x < startX + w && x < tex.width; x++)
                {
                    if (x >= 0 && y >= 0)
                        tex.SetPixel(x, y, col);
                }
            }
        }

        private static void DrawChevron(Texture2D tex, int centerX, int centerY, int size, Color col)
        {
            int thickness = 10;
            for (int i = -size; i <= size; i++)
            {
                int x = centerX + (size - Mathf.Abs(i));
                int y = centerY + i;
                for (int t = -thickness / 2; t <= thickness / 2; t++)
                {
                    int px = x + t;
                    if (px >= 0 && px < tex.width && y >= 0 && y < tex.height)
                        tex.SetPixel(px, y, col);
                }
            }
        }

        public static void GenerateMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
            {
                Debug.LogError("Could not find Universal Render Pipeline/Lit shader!");
                return;
            }

            // 1. Dark Asphalt / Base Road
            CreateOrUpdateMaterial("Cyber_Track_Asphalt", litShader, mat =>
            {
                mat.SetColor("_BaseColor", new Color(0.08f, 0.09f, 0.12f, 1f));
                mat.SetFloat("_Metallic", 0.7f);
                mat.SetFloat("_Smoothness", 0.5f);
            });

            // 2. Track Grid
            CreateOrUpdateMaterial("Cyber_Track_Grid", litShader, mat =>
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(TEXTURE_DIR, "tex_neon_grid.png"));
                if (tex != null)
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetTextureScale("_BaseMap", new Vector2(4, 20));
                }
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Metallic", 0.8f);
                mat.SetFloat("_Smoothness", 0.6f);
            });

            // 3. Neon Cyan (HDR Emission)
            CreateOrUpdateMaterial("Cyber_Neon_Cyan", litShader, mat =>
            {
                Color cyan = new Color(0.0f, 0.95f, 1.0f, 1f);
                mat.SetColor("_BaseColor", cyan);
                SetEmission(mat, cyan * 3.5f);
            });

            // 4. Neon Magenta / Pink (HDR Emission)
            CreateOrUpdateMaterial("Cyber_Neon_Magenta", litShader, mat =>
            {
                Color pink = new Color(1.0f, 0.05f, 0.65f, 1f);
                mat.SetColor("_BaseColor", pink);
                SetEmission(mat, pink * 3.5f);
            });

            // 5. Neon Yellow / Caution
            CreateOrUpdateMaterial("Cyber_Neon_Yellow", litShader, mat =>
            {
                Color yellow = new Color(1.0f, 0.85f, 0.0f, 1f);
                mat.SetColor("_BaseColor", yellow);
                SetEmission(mat, yellow * 3.0f);
            });

            // 6. Laser Red / Barrier
            CreateOrUpdateMaterial("Cyber_Laser_Red", litShader, mat =>
            {
                Color red = new Color(1.0f, 0.05f, 0.25f, 1f);
                mat.SetColor("_BaseColor", red);
                SetEmission(mat, red * 4.0f);
            });

            // 7. Hazard Stripes
            CreateOrUpdateMaterial("Cyber_Hazard", litShader, mat =>
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(TEXTURE_DIR, "tex_hazard_stripes.png"));
                if (tex != null)
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetTextureScale("_BaseMap", new Vector2(4, 1));
                }
                mat.SetColor("_BaseColor", Color.white);
                SetEmission(mat, new Color(0.8f, 0.65f, 0.0f, 1f) * 1.5f);
            });

            // 8. Skyscraper facade with window emission
            CreateOrUpdateMaterial("Cyber_Skyscraper", litShader, mat =>
            {
                Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(TEXTURE_DIR, "tex_skyscraper_windows.png"));
                Texture2D emitTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(TEXTURE_DIR, "tex_skyscraper_windows_emission.png"));
                if (baseTex != null) mat.SetTexture("_BaseMap", baseTex);
                if (emitTex != null)
                {
                    mat.SetTexture("_EmissionMap", emitTex);
                    SetEmission(mat, Color.white * 2.2f);
                }
                mat.SetFloat("_Metallic", 0.85f);
                mat.SetFloat("_Smoothness", 0.65f);
            });

            // 9. Billboard Cyber
            CreateOrUpdateMaterial("Cyber_Billboard_A", litShader, mat =>
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(TEXTURE_DIR, "tex_billboard_cyber.png"));
                if (tex != null)
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetTexture("_EmissionMap", tex);
                }
                SetEmission(mat, Color.white * 2.5f);
            });

            // 10. Billboard Kanji
            CreateOrUpdateMaterial("Cyber_Billboard_B", litShader, mat =>
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(TEXTURE_DIR, "tex_billboard_kanji.png"));
                if (tex != null)
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetTexture("_EmissionMap", tex);
                }
                SetEmission(mat, Color.white * 2.5f);
            });

            // 11. Train Dark Hull
            CreateOrUpdateMaterial("Cyber_Train_Hull", litShader, mat =>
            {
                mat.SetColor("_BaseColor", new Color(0.12f, 0.14f, 0.18f, 1f));
                mat.SetFloat("_Metallic", 0.9f);
                mat.SetFloat("_Smoothness", 0.8f);
            });

            // 12. Train White Shell
            CreateOrUpdateMaterial("Cyber_Train_White", litShader, mat =>
            {
                mat.SetColor("_BaseColor", new Color(0.85f, 0.88f, 0.95f, 1f));
                mat.SetFloat("_Metallic", 0.5f);
                mat.SetFloat("_Smoothness", 0.85f);
            });

            // 13. Train Headlight
            CreateOrUpdateMaterial("Cyber_Train_Headlight", litShader, mat =>
            {
                Color lightCol = new Color(0.8f, 0.95f, 1.0f, 1f);
                mat.SetColor("_BaseColor", lightCol);
                SetEmission(mat, lightCol * 5.0f);
            });

            // 14. Train Side Decal
            CreateOrUpdateMaterial("Cyber_Train_Side", litShader, mat =>
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(TEXTURE_DIR, "tex_train_stripes.png"));
                if (tex != null) mat.SetTexture("_BaseMap", tex);
                mat.SetFloat("_Metallic", 0.8f);
                mat.SetFloat("_Smoothness", 0.75f);
            });

            // 15. Cyber Coin Gold
            CreateOrUpdateMaterial("Cyber_Coin_Gold", litShader, mat =>
            {
                Color gold = new Color(1.0f, 0.82f, 0.05f, 1f);
                mat.SetColor("_BaseColor", gold);
                mat.SetFloat("_Metallic", 0.95f);
                mat.SetFloat("_Smoothness", 0.85f);
                SetEmission(mat, gold * 2.8f);
            });

            // 16. Player Cyber Suit
            CreateOrUpdateMaterial("Cyber_Player_Suit", litShader, mat =>
            {
                mat.SetColor("_BaseColor", new Color(0.14f, 0.16f, 0.22f, 1f));
                mat.SetFloat("_Metallic", 0.75f);
                mat.SetFloat("_Smoothness", 0.65f);
            });

            // 17. Player Visor / Thrusters
            CreateOrUpdateMaterial("Cyber_Player_Visor", litShader, mat =>
            {
                Color cyan = new Color(0.0f, 1.0f, 0.95f, 1f);
                mat.SetColor("_BaseColor", cyan);
                SetEmission(mat, cyan * 4.0f);
            });

            // 18. Overhead Gantry Dark Metal
            CreateOrUpdateMaterial("Cyber_Metal_Dark", litShader, mat =>
            {
                mat.SetColor("_BaseColor", new Color(0.15f, 0.17f, 0.22f, 1f));
                mat.SetFloat("_Metallic", 0.85f);
                mat.SetFloat("_Smoothness", 0.5f);
            });
        }

        private static void SetEmission(Material mat, Color emissionColor)
        {
            mat.shaderKeywords = new string[] { "_EMISSION" };
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", emissionColor);
        }

        private static void CreateOrUpdateMaterial(string matName, Shader shader, System.Action<Material> configure)
        {
            string path = Path.Combine(MATERIAL_DIR, matName + ".mat");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.name = matName;
                configure(mat);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
                configure(mat);
                EditorUtility.SetDirty(mat);
            }
        }
    }
}
