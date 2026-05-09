using UnityEngine;

#if ZXING_ENABLED
using ZXing;

/// <summary>
/// Optimized luminance source for Unity's Color32 array from WebCamTexture.
/// Converts Color32 pixels to grayscale for QR code detection.
/// </summary>
public class Color32LuminanceSource : LuminanceSource
{
    private readonly byte[] luminances;

    public Color32LuminanceSource(Color32[] pixels, int width, int height)
        : base(width, height)
    {
        luminances = new byte[width * height];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 c = pixels[i];
            // Standard RGB to grayscale conversion
            luminances[i] = (byte)((c.r * 0.299f) + (c.g * 0.587f) + (c.b * 0.114f));
        }
    }

    public override byte[] Matrix => luminances;

    public override byte[] getRow(int y, byte[] row)
    {
        if (y < 0 || y >= Height)
            throw new System.ArgumentException("y must be between 0 and " + Height);

        if (row == null || row.Length < Width)
            row = new byte[Width];

        System.Array.Copy(luminances, y * Width, row, 0, Width);
        return row;
    }
}
#endif
