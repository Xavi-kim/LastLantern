using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastLantern.EditorTools
{
    /// <summary>Набор сгенерированных спрайтов.</summary>
    public class GameArt
    {
        public Sprite player, runner, heavy, shooter, projectile, oil, bush, ground, ring, soft, white;
    }

    /// <summary>
    /// Рисует пиксель-арт 32x32 кодом и сохраняет как PNG-спрайты в Assets/Sprites.
    /// Бесплатно и без внешних паков; любой PNG потом можно заменить своим.
    /// </summary>
    public static class ArtGenerator
    {
        static readonly Color Clear = new Color(0f, 0f, 0f, 0f);
        static readonly Color Outline = new Color(0.03f, 0.03f, 0.07f, 1f);
        static readonly Color EyeRed = new Color(1f, 0.25f, 0.3f, 1f);

        public static GameArt CreateAll(string dir)
        {
            return new GameArt
            {
                player = Make(dir, "Player", 32, 32, PlayerPixel, 32, true, false, true),
                runner = Make(dir, "Enemy_Runner", 32, 32, RunnerPixel, 32, true, false, true),
                heavy = Make(dir, "Enemy_Heavy", 32, 32, HeavyPixel, 32, true, false, true),
                shooter = Make(dir, "Enemy_Shooter", 32, 32, ShooterPixel, 32, true, false, true),
                projectile = Make(dir, "Projectile", 16, 16, ProjectilePixel, 32, true, false, false),
                oil = Make(dir, "OilDrop", 16, 16, OilPixel, 32, true, false, true),
                bush = Make(dir, "Bush", 32, 32, BushPixel, 32, true, false, true),
                ground = Make(dir, "Ground", 64, 64, GroundPixel, 32, true, true, false),
                ring = Make(dir, "FlashRing", 128, 128, RingPixel, 128, false, false, false),
                soft = Make(dir, "SoftCircle", 32, 32, SoftPixel, 32, false, false, false),
                white = Make(dir, "White", 4, 4, (x, y) => Color.white, 4, true, false, false),
            };
        }

        static Sprite Make(string dir, string name, int w, int h, Func<int, int, Color> pixel, int ppu,
            bool pointFilter, bool repeat, bool outline)
        {
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = pixel(x, y);

            if (outline) AddOutline(pixels, w, h);

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            string path = $"{dir}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = pointFilter ? FilterMode.Point : FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // нужно для Tiled-режима земли
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Тёмная обводка в 1 пиксель вокруг непрозрачных пикселей.</summary>
        static void AddOutline(Color[] px, int w, int h)
        {
            var src = (Color[])px.Clone();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (src[y * w + x].a > 0.5f) continue;
                    bool near = (x > 0 && src[y * w + x - 1].a > 0.5f) ||
                                (x < w - 1 && src[y * w + x + 1].a > 0.5f) ||
                                (y > 0 && src[(y - 1) * w + x].a > 0.5f) ||
                                (y < h - 1 && src[(y + 1) * w + x].a > 0.5f);
                    if (near) px[y * w + x] = Outline;
                }
            }
        }

        static float Dist(float x, float y, float cx, float cy)
        {
            float dx = x - cx, dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        // ---------- Хранитель фонаря ----------
        static Color PlayerPixel(int x, int y)
        {
            // Фонарь в руке
            float dl = Dist(x, y, 25f, 9f);
            if (dl <= 2.6f) return dl < 1.3f ? new Color(1f, 0.97f, 0.8f) : new Color(1f, 0.8f, 0.35f);
            var handle = new Color(0.4f, 0.33f, 0.25f);
            if (x == 25 && y >= 12 && y <= 14) return handle;
            if (y == 14 && x >= 21 && x <= 25) return handle;

            // Капюшон и лицо в тени
            if (Dist(x, y, 15.5f, 21.5f) <= 6.5f)
            {
                if (Dist(x, y, 15.5f, 20.5f) <= 3.6f)
                {
                    if ((x == 13 || x == 18) && y == 21) return new Color(1f, 0.85f, 0.4f); // глаза
                    return new Color(0.04f, 0.04f, 0.08f);
                }
                return new Color(0.22f, 0.29f, 0.52f);
            }

            // Плащ-трапеция
            if (y >= 2 && y <= 16)
            {
                float half = 4f + (16 - y) * 0.45f;
                if (Mathf.Abs(x - 15.5f) <= half)
                    return Color.Lerp(new Color(0.12f, 0.16f, 0.34f), new Color(0.2f, 0.26f, 0.48f), (y - 2) / 14f);
            }
            return Clear;
        }

        // ---------- Тень-бегун: узкий «язык пламени» ----------
        static Color RunnerPixel(int x, int y)
        {
            if (y < 3 || y > 28) return Clear;
            float t = (y - 3) / 25f;
            float half = 10f * Mathf.Pow(1f - t, 0.7f) + Mathf.Sin(y * 0.9f) * 0.8f;
            if (Mathf.Abs(x - 15.5f) > half) return Clear;
            if (y < 6 && x % 3 == 0) return Clear; // рваный низ
            if (y >= 12 && y <= 13 && (x == 12 || x == 13 || x == 18 || x == 19)) return EyeRed;
            return Color.Lerp(new Color(0.26f, 0.09f, 0.36f), new Color(0.1f, 0.03f, 0.16f), t);
        }

        // ---------- Тень-тяжеловес: большой ком со щупальцами ----------
        static Color HeavyPixel(int x, int y)
        {
            bool body = Dist(x, y, 15.5f, 16f) <= 13.5f && y >= 4;
            bool tendril = y >= 1 && y < 6 && Mathf.Abs(x - 15.5f) < 12f && (x + y % 2) % 4 != 0;
            if (!body && !tendril) return Clear;
            if (y >= 17 && y <= 18 && ((x >= 10 && x <= 12) || (x >= 19 && x <= 21))) return EyeRed;
            if (y == 11 && x >= 12 && x <= 19) return new Color(0.45f, 0.06f, 0.12f);
            return Color.Lerp(new Color(0.07f, 0.02f, 0.11f), new Color(0.17f, 0.06f, 0.24f), y / 31f);
        }

        // ---------- Тень-стрелок: ромб со светящимся ядром ----------
        static Color ShooterPixel(int x, int y)
        {
            float manhattan = Mathf.Abs(x - 15.5f) + Mathf.Abs(y - 15.5f);
            if (manhattan > 14f) return Clear;
            float d = Dist(x, y, 15.5f, 15.5f);
            if (d <= 2f) return new Color(1f, 0.9f, 1f);
            if (d <= 4.5f) return new Color(0.85f, 0.3f, 1f);
            if (manhattan >= 9f && manhattan <= 10f) return new Color(0.36f, 0.12f, 0.5f);
            return new Color(0.2f, 0.08f, 0.32f);
        }

        // ---------- Тёмный шар ----------
        static Color ProjectilePixel(int x, int y)
        {
            float d = Dist(x, y, 7.5f, 7.5f);
            if (d <= 2.5f) return new Color(1f, 0.85f, 1f);
            if (d <= 5.5f) return new Color(0.8f, 0.3f, 1f);
            if (d <= 6.8f) return new Color(0.45f, 0.12f, 0.6f, 0.85f);
            return Clear;
        }

        // ---------- Капля масла ----------
        static Color OilPixel(int x, int y)
        {
            bool bottom = Dist(x, y, 7.5f, 5.5f) <= 5f;
            bool top = y >= 6 && y <= 14 && Mathf.Abs(x - 7.5f) <= (14 - y) * 0.55f;
            if (!bottom && !top) return Clear;
            if ((x == 5 && (y == 6 || y == 7)) || (x == 6 && y == 8)) return new Color(1f, 0.96f, 0.75f);
            return Color.Lerp(new Color(0.85f, 0.45f, 0.08f), new Color(1f, 0.78f, 0.25f), y / 15f);
        }

        // ---------- Куст ----------
        static Color BushPixel(int x, int y)
        {
            bool inside = Dist(x, y, 11f, 12f) <= 8f || Dist(x, y, 20.5f, 13f) <= 9f || Dist(x, y, 15.5f, 20f) <= 8f;
            if (!inside) return Clear;
            float h = Hash(x, y, 11);
            if (h > 0.85f) return new Color(0.1f, 0.2f, 0.15f);
            return Color.Lerp(new Color(0.04f, 0.09f, 0.08f), new Color(0.06f, 0.13f, 0.1f), h);
        }

        // ---------- Земля (тайлится) ----------
        static Color GroundPixel(int x, int y)
        {
            float h = Hash(x, y, 3);
            float h2 = Hash(x / 4, y / 4, 5);
            var c = Color.Lerp(new Color(0.05f, 0.08f, 0.09f), new Color(0.07f, 0.11f, 0.11f), h2 * 0.7f + h * 0.3f);
            if (h > 0.94f) c = new Color(0.1f, 0.17f, 0.14f);
            return c;
        }

        // ---------- Кольцо вспышки ----------
        static Color RingPixel(int x, int y)
        {
            float d = Dist(x, y, 63.5f, 63.5f);
            float band = Mathf.Clamp01(1f - Mathf.Abs(d - 58f) / 5f);
            float glow = d < 58f ? Mathf.Clamp01(1f - d / 58f) * 0.08f : 0f;
            return new Color(1f, 1f, 1f, Mathf.Max(band, glow));
        }

        // ---------- Мягкий круг для частиц ----------
        static Color SoftPixel(int x, int y)
        {
            float a = Mathf.Clamp01(1f - Dist(x, y, 15.5f, 15.5f) / 15.5f);
            return new Color(1f, 1f, 1f, Mathf.Pow(a, 1.5f));
        }
    }
}
