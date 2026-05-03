"""
QR Code Generator for AR Campus Navigation
==========================================
Reads nodes.json and generates one QR code per location.
Each QR encodes structured JSON:
  { "building": "Main Block", "floor": 0, "node_id": "ENTRANCE" }

Usage:
  pip install qrcode[pil] pillow
  python generate_qr.py

Output:
  qr_codes/ENTRANCE.png
  qr_codes/LOBBY.png
  ... (one per node)
"""

import json
import os
import sys
import qrcode
from PIL import Image, ImageDraw

# Fix Windows terminal encoding
sys.stdout.reconfigure(encoding="utf-8")

# ── Config ────────────────────────────────────────────────────────────────────
NODES_FILE   = "../ARSpatialClient/Assets/ProjectCore/Resources/nodes.json"
OUTPUT_DIR   = "qr_codes"
QR_SIZE      = 400
LABEL_HEIGHT = 60

# ── Load nodes ────────────────────────────────────────────────────────────────
with open(NODES_FILE, "r") as f:
    data = json.load(f)

nodes = data["nodes"]
os.makedirs(OUTPUT_DIR, exist_ok=True)

print(f"Generating {len(nodes)} QR codes...\n")

# ── Generate ──────────────────────────────────────────────────────────────────
for node in nodes:
    payload = json.dumps({
        "building": node["building"],
        "floor":    node["floor"],
        "node_id":  node["id"]
    }, separators=(",", ":"))

    qr = qrcode.QRCode(
        version=1,
        error_correction=qrcode.constants.ERROR_CORRECT_M,
        box_size=10,
        border=4,
    )
    qr.add_data(payload)
    qr.make(fit=True)

    qr_img = qr.make_image(fill_color="black", back_color="white").convert("RGB")
    qr_img = qr_img.resize((QR_SIZE, QR_SIZE), Image.NEAREST)

    final_img = Image.new("RGB", (QR_SIZE, QR_SIZE + LABEL_HEIGHT), "white")
    final_img.paste(qr_img, (0, 0))

    draw = ImageDraw.Draw(final_img)

    floor_label = "Ground Floor" if node["floor"] == 0 else f"Floor {node['floor']}"
    label = f"{node['id']}  |  {node['displayName']}  |  {floor_label}"

    draw.rectangle([(0, QR_SIZE), (QR_SIZE, QR_SIZE + LABEL_HEIGHT)], fill="#1a1a2e")
    draw.text((10, QR_SIZE + 15), label, fill="white")

    filename = os.path.join(OUTPUT_DIR, f"{node['id']}.png")
    final_img.save(filename)
    print(f"  OK  {filename}  ->  {payload}")

print(f"\nDone! {len(nodes)} QR codes saved to '{OUTPUT_DIR}/'")
print("Print these and place them at the matching physical locations on campus.")
print("Each QR should be placed AT the location it represents.")
