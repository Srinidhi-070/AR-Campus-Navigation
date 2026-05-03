using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Generates simple icon sprites for the Campus Navigation UI.
/// Run via: Tools → Generate UI Icons
/// </summary>
public class IconGenerator : EditorWindow
{
    [MenuItem("Tools/Generate UI Icons")]
    public static void GenerateIcons()
    {
        string iconPath = "Assets/ProjectCore/Resources/Icons";
        if (!Directory.Exists(iconPath))
            Directory.CreateDirectory(iconPath);

        // Generate menu icon (3 horizontal lines)
        GenerateMenuIcon(Path.Combine(iconPath, "menu.png"));
        
        // Generate QR icon (grid pattern)
        GenerateQRIcon(Path.Combine(iconPath, "qr.png"));
        
        // Generate close icon (X)
        GenerateCloseIcon(Path.Combine(iconPath, "close.png"));
        
        // Generate send icon (arrow)
        GenerateSendIcon(Path.Combine(iconPath, "send.png"));

        AssetDatabase.Refresh();
        
        // Set import settings for all icons
        SetIconImportSettings(Path.Combine(iconPath, "menu.png"));
        SetIconImportSettings(Path.Combine(iconPath, "qr.png"));
        SetIconImportSettings(Path.Combine(iconPath, "close.png"));
        SetIconImportSettings(Path.Combine(iconPath, "send.png"));

        Debug.Log("[IconGenerator] Generated 4 UI icons in Resources/Icons/");
        EditorUtility.DisplayDialog("Icons Generated", 
            "Generated 4 UI icons:\n• menu.png\n• qr.png\n• close.png\n• send.png\n\nCheck Resources/Icons/", 
            "OK");
    }

    static void GenerateMenuIcon(string path)
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        
        // Transparent background
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 0);

        // Draw 3 horizontal lines
        int lineHeight = 12;
        int lineSpacing = 20;
        int startY = (size - (3 * lineHeight + 2 * lineSpacing)) / 2;
        int margin = 20;

        for (int line = 0; line < 3; line++)
        {
            int y = startY + line * (lineHeight + lineSpacing);
            for (int dy = 0; dy < lineHeight; dy++)
            {
                for (int x = margin; x < size - margin; x++)
                {
                    pixels[(y + dy) * size + x] = new Color32(255, 255, 255, 255);
                }
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
    }

    static void GenerateQRIcon(string path)
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        
        // Transparent background
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 0);

        // Draw a cleaner QR-like pattern
        int margin = 20;
        int innerSize = size - 2 * margin; // 88px
        int cellSize = 11; // 8x8 grid
        
        // Draw outer border (frame)
        DrawRect(pixels, size, margin, margin, innerSize, innerSize, 4);
        
        // Draw 3 corner squares (QR code positioning markers)
        int cornerSize = 24;
        int cornerInner = 16;
        int cornerOffset = margin + 4;
        
        // Top-left corner
        DrawRect(pixels, size, cornerOffset, cornerOffset, cornerSize, cornerSize, 3);
        DrawFilledRect(pixels, size, cornerOffset + 6, cornerOffset + 6, cornerInner - 6, cornerInner - 6);
        
        // Top-right corner
        DrawRect(pixels, size, size - cornerOffset - cornerSize, cornerOffset, cornerSize, cornerSize, 3);
        DrawFilledRect(pixels, size, size - cornerOffset - cornerInner - 3, cornerOffset + 6, cornerInner - 6, cornerInner - 6);
        
        // Bottom-left corner
        DrawRect(pixels, size, cornerOffset, size - cornerOffset - cornerSize, cornerSize, cornerSize, 3);
        DrawFilledRect(pixels, size, cornerOffset + 6, size - cornerOffset - cornerInner - 3, cornerInner - 6, cornerInner - 6);
        
        // Draw some random-looking data cells in the middle
        int[] dataPattern = { 1, 0, 1, 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 1, 0 };
        int dataX = margin + 40;
        int dataY = margin + 40;
        int dataCell = 8;
        
        for (int i = 0; i < dataPattern.Length; i++)
        {
            if (dataPattern[i] == 1)
            {
                int x = dataX + (i % 4) * (dataCell + 2);
                int y = dataY + (i / 4) * (dataCell + 2);
                DrawFilledRect(pixels, size, x, y, dataCell, dataCell);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
    }
    
    // Helper method to draw a rectangle outline
    static void DrawRect(Color32[] pixels, int texSize, int x, int y, int width, int height, int thickness)
    {
        // Top
        DrawFilledRect(pixels, texSize, x, y, width, thickness);
        // Bottom
        DrawFilledRect(pixels, texSize, x, y + height - thickness, width, thickness);
        // Left
        DrawFilledRect(pixels, texSize, x, y, thickness, height);
        // Right
        DrawFilledRect(pixels, texSize, x + width - thickness, y, thickness, height);
    }
    
    // Helper method to draw a filled rectangle
    static void DrawFilledRect(Color32[] pixels, int texSize, int x, int y, int width, int height)
    {
        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                int px = x + dx;
                int py = y + dy;
                if (px >= 0 && px < texSize && py >= 0 && py < texSize)
                {
                    pixels[py * texSize + px] = new Color32(255, 255, 255, 255);
                }
            }
        }
    }

    static void GenerateCloseIcon(string path)
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        
        // Transparent background
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 0);

        // Draw X
        int thickness = 12;
        int margin = 24;

        for (int i = margin; i < size - margin; i++)
        {
            for (int t = -thickness / 2; t < thickness / 2; t++)
            {
                // Diagonal \
                int y1 = i + t;
                int x1 = i;
                if (x1 >= 0 && x1 < size && y1 >= 0 && y1 < size)
                    pixels[y1 * size + x1] = new Color32(255, 255, 255, 255);

                // Diagonal /
                int y2 = (size - 1 - i) + t;
                int x2 = i;
                if (x2 >= 0 && x2 < size && y2 >= 0 && y2 < size)
                    pixels[y2 * size + x2] = new Color32(255, 255, 255, 255);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
    }

    static void GenerateSendIcon(string path)
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        
        // Transparent background
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 0);

        // Draw arrow pointing right
        int centerY = size / 2;
        int startX = 20;
        int endX = size - 20;
        int thickness = 10;

        // Arrow shaft
        for (int x = startX; x < endX - 20; x++)
        {
            for (int t = -thickness / 2; t < thickness / 2; t++)
            {
                int y = centerY + t;
                if (y >= 0 && y < size)
                    pixels[y * size + x] = new Color32(255, 255, 255, 255);
            }
        }

        // Arrow head
        int headSize = 30;
        for (int i = 0; i < headSize; i++)
        {
            for (int t = -thickness / 2; t < thickness / 2; t++)
            {
                // Upper diagonal
                int x1 = endX - i;
                int y1 = centerY - i + t;
                if (x1 >= 0 && x1 < size && y1 >= 0 && y1 < size)
                    pixels[y1 * size + x1] = new Color32(255, 255, 255, 255);

                // Lower diagonal
                int x2 = endX - i;
                int y2 = centerY + i + t;
                if (x2 >= 0 && x2 < size && y2 >= 0 && y2 < size)
                    pixels[y2 * size + x2] = new Color32(255, 255, 255, 255);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
    }

    static void SetIconImportSettings(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();
        }
    }
}
