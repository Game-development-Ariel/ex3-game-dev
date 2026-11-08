using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Loads a sprite file, and if it doesn't exist yet, draws a simple stand-in pixel by pixel and saves it there.
/// Later the stand-in is replaced by the real (AI-made) image with the same file name,
/// and everything that uses the sprite updates by itself.
/// </summary>
public static class PlaceholderSprites
{
    // Same as the tunnel sprites: one tile = 32 pixels.
    public const int PixelsPerTile = 32;

    /// <param name="path">Where the sprite file is, for example "Assets/Sprites/Items/WaterBottle.png".</param>
    /// <param name="pixelColor">Gives the color of pixel (x, y). Used only when the file doesn't exist.</param>
    public static Sprite LoadOrCreate(string path, Func<int, int, Color> pixelColor)
    {
        if (!File.Exists(path))
        {
            var texture = new Texture2D(PixelsPerTile, PixelsPerTile, TextureFormat.RGBA32, false);
            for (int x = 0; x < PixelsPerTile; x++)
            {
                for (int y = 0; y < PixelsPerTile; y++)
                {
                    texture.SetPixel(x, y, pixelColor(x, y));
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
        }

        // Pixel art settings: sharp pixels (no blur), no compression, transparent background.
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerTile;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>True if (x, y) is inside the rectangle from (left, bottom) to (right, top), edges included.</summary>
    public static bool InRect(int x, int y, int left, int bottom, int right, int top)
    {
        return x >= left && x <= right && y >= bottom && y <= top;
    }
}
