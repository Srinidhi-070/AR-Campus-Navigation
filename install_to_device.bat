@echo off
echo ========================================
echo AR Campus Navigation - Build and Install
echo ========================================
echo.

REM Check if device is connected
echo [1/5] Checking device connection...
adb devices | findstr "device" >nul
if errorlevel 1 (
    echo ERROR: No device connected!
    echo Please connect your Android device via USB and enable USB debugging.
    pause
    exit /b 1
)
echo ✓ Device connected

REM Uninstall old version
echo.
echo [2/5] Uninstalling old version...
adb uninstall com.srinidhi.arcampusnav >nul 2>&1
echo ✓ Old version removed

REM Check if APK exists
echo.
echo [3/5] Looking for APK...
if not exist "Builds\ARCampusNav.apk" (
    echo ERROR: APK not found at Builds\ARCampusNav.apk
    echo Please build the project in Unity first:
    echo   File → Build Settings → Build
    pause
    exit /b 1
)
echo ✓ APK found

REM Install APK
echo.
echo [4/5] Installing APK...
adb install -r "Builds\ARCampusNav.apk"
if errorlevel 1 (
    echo ERROR: Installation failed!
    echo Check the error message above.
    pause
    exit /b 1
)
echo ✓ APK installed

REM Launch app
echo.
echo [5/5] Launching app...
adb shell am start -n com.srinidhi.arcampusnav/com.unity3d.player.UnityPlayerActivity
if errorlevel 1 (
    echo WARNING: Could not launch app automatically.
    echo Please launch it manually from your device.
) else (
    echo ✓ App launched
)

echo.
echo ========================================
echo ✓ BUILD AND INSTALL COMPLETE!
echo ========================================
echo.
echo The app should now be running on your device.
echo.
echo To view logs:
echo   adb logcat -s Unity
echo.
pause
