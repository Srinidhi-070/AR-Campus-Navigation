# QUICK FIX - Remove BuildScannerPanel from CampusRuntimeUI.cs

## The Problem:
CampusRuntimeUI.cs still has the old BuildScannerPanel() method and references to ScannerPanel, ScannerPreview, etc.

## Solution:
Delete lines 354-410 (BuildScannerPanel method and CreateCornerBrackets helper)

## Manual Fix:
1. Open: Assets/ProjectCore/Scripts/UI/CampusRuntimeUI.cs
2. Find line 354: `private void BuildScannerPanel(Transform parent)`
3. Delete from line 354 to line 410 (entire BuildScannerPanel method)
4. Find line 411: `private void CreateCornerBrackets(Transform parent, Color color)`
5. Delete from line 411 to line 440 (entire CreateCornerBrackets method and CreateBracketCorner)
6. Save file

## OR Use Find/Replace:
Search for: Everything from `private void BuildScannerPanel` to the end of `CreateBracketCorner` method
Replace with: (nothing - delete it)

The QR scanner now builds its own UI in QRScannerUI.cs, so this old code is not needed.
