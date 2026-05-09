# ⚠️ COMPILATION ERRORS - MANUAL FIX REQUIRED

## 🎯 THE PROBLEM:
CampusRuntimeUI.cs still has old scanner code that references deleted properties.

## ✅ SIMPLE FIX (2 minutes):

### In Unity Editor:

1. **Open the file**:
   - Project panel → Assets → ProjectCore → Scripts → UI
   - Double-click: `CampusRuntimeUI.cs`

2. **Delete these 3 methods** (find and delete entire methods):
   - `BuildScannerPanel()` (around line 354)
   - `CreateCornerBrackets()` (around line 411)
   - `CreateBracketCorner()` (around line 425)
   - `CreateBorderLine()` (around line 445)

3. **Save the file** (Ctrl+S)

4. **Return to Unity** - it will recompile

5. **Build again**:
   - File → Build Settings → Build
   - Save as: ARCampusNav.apk

---

## 🔧 OR USE THIS REPLACEMENT:

If you want, I can provide a clean CampusRuntimeUI.cs file without the scanner code.

Just delete the entire CampusRuntimeUI.cs file and I'll create a new clean one.

---

## ⚡ QUICK FIX IN QRScannerUI.cs:

Also fix line 405 in QRScannerUI.cs:

**Find**:
```csharp
try
{
    Color32[] pixels = m_WebCam.GetPixels32();
    // ... code ...
    yield return new WaitForSeconds(0.25f);
}
catch (System.Exception ex)
{
    Debug.LogWarning($"[QRScannerUI] Scan error: {ex.Message}");
}
```

**Change to**:
```csharp
Color32[] pixels = null;
try
{
    pixels = m_WebCam.GetPixels32();
}
catch (System.Exception ex)
{
    Debug.LogWarning($"[QRScannerUI] Scan error: {ex.Message}");
    yield return new WaitForSeconds(0.25f);
    continue;
}

if (pixels == null || pixels.Length == 0)
{
    yield return new WaitForSeconds(0.25f);
    continue;
}

// ... rest of code ...
yield return new WaitForSeconds(0.25f);
```

---

**Just delete those 4 methods from CampusRuntimeUI.cs and fix the try-catch in QRScannerUI.cs, then rebuild!**
