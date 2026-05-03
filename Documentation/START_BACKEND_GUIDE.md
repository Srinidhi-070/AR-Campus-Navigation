# How to Start the Backend Server

## The Problem
Your app shows: **"Could not load campus data. Cannot connect to destination host"**

This is because the Python backend server is not running.

---

## Quick Fix - Start the Backend Server

### Method 1: Double-Click the Batch File (EASIEST)

1. Open File Explorer
2. Navigate to: `d:\AR_Spatial_Client\ARBackend\`
3. Find the file: **start_backend.bat**
4. **Double-click** it
5. A black command window will open showing the server is running
6. **Keep this window open!** Don't close it
7. You should see messages like:
   ```
   * Running on http://192.168.1.7:8000
   * Running on http://127.0.0.1:8000
   ```

### Method 2: Manual Start

1. Open Command Prompt (cmd)
2. Type: `cd d:\AR_Spatial_Client\ARBackend`
3. Press Enter
4. Type: `python main.py`
5. Press Enter
6. Keep the window open

---

## After Starting the Server

1. **Restart your app** on the phone (close and reopen it)
2. The error should disappear
3. The app should load campus data successfully

---

## Important Notes

- **Keep the command window open** while using the app
- Your phone and computer must be on the **same WiFi network**
- The server runs on: `http://192.168.1.7:8000`
- If you close the command window, the server stops

---

## If It Still Doesn't Work

### Check 1: Are you on the same WiFi?
- Your phone and computer must be connected to the same WiFi network

### Check 2: Check the IP address
- The app is configured to connect to: `192.168.1.7`
- This must be your computer's IP address
- To check your computer's IP:
  1. Open Command Prompt
  2. Type: `ipconfig`
  3. Look for "IPv4 Address" under your WiFi adapter
  4. If it's different from 192.168.1.7, we need to update the app

### Check 3: Firewall
- Windows Firewall might be blocking the connection
- When you start the server, Windows may ask to allow access
- Click "Allow access"

---

## What the Backend Server Does

- Provides campus location data (buildings, floors, rooms)
- Handles navigation path requests
- Processes AI chat messages
- Serves QR code data

Without the backend server running, the app cannot function!
