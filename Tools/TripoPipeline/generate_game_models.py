#!/usr/bin/env python3
"""
Procedural Stylized 3D Model Generator for Project Survival
Generates 10 game-ready low-poly 3D .glb models (characters, zombies, vehicles, props)
with clean topology, distinct silhouettes, and vertex colors for Unity 6 (URP).
"""

import os
import math
import struct
import json
from pathlib import Path


class MeshBuilder:
    def __init__(self):
        self.positions = []
        self.indices = []
        self.colors = []

    def add_box(self, center, size, color):
        """Adds an axis-aligned box with given center, size (dx,dy,dz), and RGBA color."""
        cx, cy, cz = center
        dx, dy, dz = size[0] * 0.5, size[1] * 0.5, size[2] * 0.5
        r, g, b, a = color

        base_idx = len(self.positions)

        # 8 corners
        corners = [
            (cx - dx, cy - dy, cz + dz),
            (cx + dx, cy - dy, cz + dz),
            (cx + dx, cy + dy, cz + dz),
            (cx - dx, cy + dy, cz + dz),
            (cx - dx, cy - dy, cz - dz),
            (cx + dx, cy - dy, cz - dz),
            (cx + dx, cy + dy, cz - dz),
            (cx - dx, cy + dy, cz - dz),
        ]

        # 6 faces (each with 4 vertices for flat shading colors/normals)
        faces = [
            (0, 1, 2, 3), # front
            (5, 4, 7, 6), # back
            (4, 0, 3, 7), # left
            (1, 5, 6, 2), # right
            (3, 2, 6, 7), # top
            (4, 5, 1, 0), # bottom
        ]

        # Lighting shading tints per face for instant 3D readability
        shades = [1.0, 0.7, 0.8, 0.85, 1.15, 0.5]

        for face_idx, (v0, v1, v2, v3) in enumerate(faces):
            start = len(self.positions)
            shade = shades[face_idx]
            cr = min(1.0, r * shade)
            cg = min(1.0, g * shade)
            cb = min(1.0, b * shade)

            for ci in (v0, v1, v2, v3):
                self.positions.append(corners[ci])
                self.colors.append((cr, cg, cb, a))

            self.indices.extend([start, start + 1, start + 2, start, start + 2, start + 3])

    def add_cylinder(self, center, radius, height, color, segments=8):
        """Adds a vertical cylinder with caps."""
        cx, cy, cz = center
        h2 = height * 0.5
        r, g, b, a = color

        top_center_idx = len(self.positions)
        self.positions.append((cx, cy + h2, cz))
        self.colors.append((min(1.0, r * 1.1), min(1.0, g * 1.1), min(1.0, b * 1.1), a))

        bot_center_idx = len(self.positions)
        self.positions.append((cx, cy - h2, cz))
        self.colors.append((r * 0.6, g * 0.6, b * 0.6, a))

        ring_start = len(self.positions)
        for i in range(segments):
            angle = 2.0 * math.pi * i / segments
            x = cx + radius * math.cos(angle)
            z = cz + radius * math.sin(angle)
            shade = 0.8 + 0.2 * math.cos(angle)
            cr, cg, cb = min(1.0, r * shade), min(1.0, g * shade), min(1.0, b * shade)

            # top ring vertex
            self.positions.append((x, cy + h2, z))
            self.colors.append((cr, cg, cb, a))
            # bottom ring vertex
            self.positions.append((x, cy - h2, z))
            self.colors.append((cr * 0.8, cg * 0.8, cb * 0.8, a))

        for i in range(segments):
            next_i = (i + 1) % segments
            t1 = ring_start + i * 2
            b1 = t1 + 1
            t2 = ring_start + next_i * 2
            b2 = t2 + 1

            # side quad
            self.indices.extend([t1, b1, b2, t1, b2, t2])
            # top cap
            self.indices.extend([top_center_idx, t1, t2])
            # bottom cap
            self.indices.extend([bot_center_idx, b2, b1])

    def export_glb(self, filepath: Path):
        """Packs mesh into standard glTF 2.0 binary (.glb)."""
        filepath.parent.mkdir(parents=True, exist_ok=True)

        min_pos = [min(p[0] for p in self.positions), min(p[1] for p in self.positions), min(p[2] for p in self.positions)]
        max_pos = [max(p[0] for p in self.positions), max(p[1] for p in self.positions), max(p[2] for p in self.positions)]

        pos_bytes = bytearray()
        for p in self.positions:
            pos_bytes += struct.pack('<3f', p[0], p[1], p[2])

        col_bytes = bytearray()
        for c in self.colors:
            col_bytes += struct.pack('<4f', c[0], c[1], c[2], c[3])

        idx_bytes = bytearray()
        for idx in self.indices:
            idx_bytes += struct.pack('<H', idx)

        while len(pos_bytes) % 4 != 0: pos_bytes += b'\x00'
        while len(col_bytes) % 4 != 0: col_bytes += b'\x00'
        while len(idx_bytes) % 4 != 0: idx_bytes += b'\x00'

        bin_data = bytes(pos_bytes + col_bytes + idx_bytes)

        pos_offset = 0
        pos_len = len(pos_bytes)
        col_offset = pos_len
        col_len = len(col_bytes)
        idx_offset = col_offset + col_len
        idx_len = len(idx_bytes)

        buffer_views = [
            {'buffer': 0, 'byteOffset': pos_offset, 'byteLength': len(self.positions) * 12, 'target': 34962},
            {'buffer': 0, 'byteOffset': col_offset, 'byteLength': len(self.colors) * 16, 'target': 34962},
            {'buffer': 0, 'byteOffset': idx_offset, 'byteLength': len(self.indices) * 2, 'target': 34963}
        ]
        accessors = [
            {'bufferView': 0, 'byteOffset': 0, 'componentType': 5126, 'count': len(self.positions), 'type': 'VEC3', 'max': max_pos, 'min': min_pos},
            {'bufferView': 1, 'byteOffset': 0, 'componentType': 5126, 'count': len(self.colors), 'type': 'VEC4'},
            {'bufferView': 2, 'byteOffset': 0, 'componentType': 5123, 'count': len(self.indices), 'type': 'SCALAR', 'max': [max(self.indices)], 'min': [min(self.indices)]}
        ]

        gltf = {
            'asset': {'version': '2.0', 'generator': 'ProjectSurvival 3D Asset Generator'},
            'scene': 0,
            'scenes': [{'nodes': [0]}],
            'nodes': [{'mesh': 0}],
            'materials': [{
                'name': 'Mat_StylizedVertexLit',
                'pbrMetallicRoughness': {'metallicFactor': 0.1, 'roughnessFactor': 0.8}
            }],
            'meshes': [{
                'primitives': [{
                    'attributes': {'POSITION': 0, 'COLOR_0': 1},
                    'indices': 2,
                    'material': 0
                }]
            }],
            'buffers': [{'byteLength': len(bin_data)}],
            'bufferViews': buffer_views,
            'accessors': accessors
        }

        json_str = json.dumps(gltf, separators=(',', ':'))
        json_bytes = json_str.encode('utf-8')
        while len(json_bytes) % 4 != 0: json_bytes += b' '

        total_len = 12 + 8 + len(json_bytes) + 8 + len(bin_data)
        header = struct.pack('<4sII', b'glTF', 2, total_len)
        chunk0 = struct.pack('<II', len(json_bytes), 0x4E4F534A) + json_bytes
        chunk1 = struct.pack('<II', len(bin_data), 0x004E4942) + bin_data

        with open(filepath, 'wb') as f:
            f.write(header)
            f.write(chunk0)
            f.write(chunk1)


# ==========================================
# 3D MODEL DESIGNS (ALL 10 ASSETS)
# ==========================================

def build_survivor(path: Path):
    m = MeshBuilder()
    skin = (0.95, 0.80, 0.68, 1.0)
    hair = (0.20, 0.15, 0.12, 1.0)
    hoodie = (0.20, 0.35, 0.45, 1.0) # blue-grey tactical hoodie
    backpack = (0.42, 0.30, 0.20, 1.0) # leather brown pack
    pants = (0.28, 0.32, 0.30, 1.0) # olive cargo
    boots = (0.12, 0.12, 0.12, 1.0)

    # Head & Hair
    m.add_box((0, 1.62, 0), (0.24, 0.26, 0.24), skin)
    m.add_box((0, 1.74, -0.02), (0.26, 0.10, 0.26), hair) # cap/hair
    m.add_box((0, 1.62, 0.13), (0.18, 0.04, 0.06), (0.15, 0.15, 0.15, 1.0)) # tactical visor

    # Torso
    m.add_box((0, 1.25, 0), (0.44, 0.50, 0.26), hoodie)
    # Tactical vest detail
    m.add_box((0, 1.25, 0.08), (0.36, 0.42, 0.12), (0.15, 0.25, 0.18, 1.0))

    # Backpack
    m.add_box((0, 1.25, -0.22), (0.34, 0.42, 0.20), backpack)
    m.add_box((0, 1.48, -0.20), (0.28, 0.10, 0.16), (0.32, 0.24, 0.16, 1.0)) # rolled sleeping mat

    # Arms (T-Pose / A-Pose)
    m.add_box((-0.35, 1.28, 0), (0.26, 0.16, 0.16), hoodie) # L upper
    m.add_box((-0.58, 1.28, 0), (0.22, 0.14, 0.14), skin)   # L forearm
    m.add_box((0.35, 1.28, 0), (0.26, 0.16, 0.16), hoodie)  # R upper
    m.add_box((0.58, 1.28, 0), (0.22, 0.14, 0.14), skin)    # R forearm

    # Legs
    m.add_box((-0.13, 0.65, 0), (0.18, 0.65, 0.20), pants)
    m.add_box((0.13, 0.65, 0), (0.18, 0.65, 0.20), pants)
    # Cargo pockets
    m.add_box((-0.23, 0.70, 0), (0.05, 0.18, 0.14), pants)
    m.add_box((0.23, 0.70, 0), (0.05, 0.18, 0.14), pants)

    # Boots
    m.add_box((-0.13, 0.12, 0.04), (0.19, 0.24, 0.26), boots)
    m.add_box((0.13, 0.12, 0.04), (0.19, 0.24, 0.26), boots)

    m.export_glb(path)


def build_zombie_standard(path: Path):
    m = MeshBuilder()
    skin = (0.35, 0.52, 0.25, 1.0) # pale decayed green
    shirt = (0.50, 0.45, 0.40, 1.0) # dirty torn shirt
    jeans = (0.20, 0.25, 0.32, 1.0) # torn denim
    eyes = (0.95, 0.95, 0.20, 1.0) # sickly yellow

    # Head
    m.add_box((0, 1.62, 0.02), (0.25, 0.26, 0.25), skin)
    m.add_box((-0.06, 1.65, 0.15), (0.05, 0.05, 0.02), eyes) # left eye
    m.add_box((0.06, 1.65, 0.15), (0.05, 0.05, 0.02), eyes)  # right eye
    m.add_box((0, 1.54, 0.14), (0.14, 0.06, 0.04), (0.15, 0.05, 0.05, 1.0)) # open mouth

    # Torso
    m.add_box((0, 1.25, 0), (0.42, 0.48, 0.26), shirt)
    # Exposed decaying ribs
    m.add_box((0.08, 1.22, 0.13), (0.14, 0.18, 0.02), skin)

    # Arms (reaching forward aggressively)
    m.add_box((-0.34, 1.30, 0.15), (0.16, 0.15, 0.42), shirt)
    m.add_box((-0.34, 1.30, 0.42), (0.14, 0.14, 0.20), skin)
    m.add_box((0.34, 1.30, 0.15), (0.16, 0.15, 0.42), shirt)
    m.add_box((0.34, 1.30, 0.42), (0.14, 0.14, 0.20), skin)

    # Legs
    m.add_box((-0.13, 0.65, 0), (0.18, 0.65, 0.20), jeans)
    m.add_box((0.13, 0.65, 0), (0.18, 0.65, 0.20), jeans)
    # Bare decayed feet
    m.add_box((-0.13, 0.08, 0.05), (0.17, 0.16, 0.24), skin)
    m.add_box((0.13, 0.08, 0.05), (0.17, 0.16, 0.24), skin)

    m.export_glb(path)


def build_zombie_runner(path: Path):
    m = MeshBuilder()
    skin = (0.75, 0.18, 0.15, 1.0) # mutated reddish skin
    shorts = (0.18, 0.18, 0.22, 1.0) # tattered black athletic shorts
    eyes = (1.0, 0.9, 0.1, 1.0)

    # Lean Head
    m.add_box((0, 1.58, 0.05), (0.20, 0.24, 0.22), skin)
    m.add_box((-0.05, 1.62, 0.16), (0.04, 0.04, 0.02), eyes)
    m.add_box((0.05, 1.62, 0.16), (0.04, 0.04, 0.02), eyes)

    # Slender muscular torso
    m.add_box((0, 1.22, 0), (0.34, 0.46, 0.20), skin)

    # Long agile arms
    m.add_box((-0.28, 1.20, 0.10), (0.12, 0.12, 0.48), skin)
    m.add_box((0.28, 1.20, 0.10), (0.12, 0.12, 0.48), skin)

    # Athletic legs
    m.add_box((-0.11, 0.90, 0), (0.14, 0.22, 0.16), shorts)
    m.add_box((0.11, 0.90, 0), (0.14, 0.22, 0.16), shorts)
    m.add_box((-0.11, 0.48, 0), (0.12, 0.65, 0.14), skin)
    m.add_box((0.11, 0.48, 0), (0.12, 0.65, 0.14), skin)
    # Feet
    m.add_box((-0.11, 0.06, 0.04), (0.13, 0.12, 0.22), skin)
    m.add_box((0.11, 0.06, 0.04), (0.13, 0.12, 0.22), skin)

    m.export_glb(path)


def build_zombie_screamer(path: Path):
    m = MeshBuilder()
    skin = (0.42, 0.38, 0.45, 1.0) # pale grey-purple
    purple_vein = (0.65, 0.15, 0.85, 1.0) # glowing purple mutated veins
    rags = (0.25, 0.22, 0.28, 1.0)

    # Distorted head with giant scream jaw
    m.add_box((0, 1.64, 0), (0.24, 0.28, 0.24), skin)
    # Huge open jaw
    m.add_box((0, 1.50, 0.14), (0.18, 0.14, 0.14), (0.25, 0.05, 0.15, 1.0))
    # Glowing throat
    m.add_box((0, 1.50, 0.18), (0.12, 0.08, 0.04), purple_vein)

    # Torso with glowing mutated vein cluster
    m.add_box((0, 1.24, 0), (0.38, 0.48, 0.22), rags)
    m.add_box((0, 1.28, 0.12), (0.24, 0.20, 0.03), purple_vein)

    # Shivering spindly arms
    m.add_box((-0.30, 1.25, 0), (0.12, 0.45, 0.12), skin)
    m.add_box((0.30, 1.25, 0), (0.12, 0.45, 0.12), skin)

    # Spindly legs
    m.add_box((-0.12, 0.58, 0), (0.14, 0.70, 0.16), rags)
    m.add_box((0.12, 0.58, 0), (0.14, 0.70, 0.16), rags)

    m.export_glb(path)


def build_zombie_tank(path: Path):
    m = MeshBuilder()
    skin = (0.22, 0.25, 0.28, 1.0) # dark armoured slate skin
    armour = (0.15, 0.15, 0.18, 1.0) # heavy metal/chitin plates
    pants = (0.12, 0.12, 0.14, 1.0)

    # Heavy broad head
    m.add_box((0, 2.05, 0.06), (0.34, 0.32, 0.34), skin)
    m.add_box((0, 2.18, 0.04), (0.36, 0.10, 0.36), armour) # armored skull cap

    # Massive muscular torso (2.3m tall brute)
    m.add_box((0, 1.55, 0), (0.80, 0.65, 0.48), skin)
    # Armored shoulder pads
    m.add_box((-0.55, 1.82, 0), (0.32, 0.25, 0.38), armour)
    m.add_box((0.55, 1.82, 0), (0.32, 0.25, 0.38), armour)

    # Huge boulder arms
    m.add_box((-0.55, 1.30, 0.05), (0.28, 0.65, 0.30), skin)
    m.add_box((-0.55, 0.85, 0.15), (0.32, 0.35, 0.35), armour) # heavy stone-fist
    m.add_box((0.55, 1.30, 0.05), (0.28, 0.65, 0.30), skin)
    m.add_box((0.55, 0.85, 0.15), (0.32, 0.35, 0.35), armour)  # heavy stone-fist

    # Heavy legs
    m.add_box((-0.24, 0.60, 0), (0.32, 0.75, 0.36), pants)
    m.add_box((0.24, 0.60, 0), (0.32, 0.75, 0.36), pants)
    # Huge feet
    m.add_box((-0.24, 0.12, 0.08), (0.34, 0.24, 0.44), skin)
    m.add_box((0.24, 0.12, 0.08), (0.34, 0.24, 0.44), skin)

    m.export_glb(path)


def build_dr_aras(path: Path):
    m = MeshBuilder()
    skin = (0.92, 0.78, 0.65, 1.0)
    coat = (0.92, 0.94, 0.95, 1.0) # lab coat
    scrubs = (0.16, 0.48, 0.72, 1.0) # blue hospital scrubs
    stain = (0.55, 0.10, 0.10, 1.0) # emergency red stain

    # Head & Short hair
    m.add_box((0, 1.62, 0), (0.24, 0.26, 0.24), skin)
    m.add_box((0, 1.74, 0), (0.26, 0.08, 0.26), (0.30, 0.25, 0.20, 1.0)) # hair
    m.add_box((0, 1.63, 0.13), (0.18, 0.04, 0.03), (0.20, 0.20, 0.20, 1.0)) # glasses frame

    # Torso (Coat over Scrubs)
    m.add_box((0, 1.25, 0), (0.44, 0.52, 0.26), coat)
    m.add_box((0, 1.30, 0.135), (0.18, 0.38, 0.01), scrubs) # V-neck scrubs opening
    m.add_box((-0.08, 1.15, 0.14), (0.10, 0.14, 0.01), stain)  # blood stain from surgeries

    # Coat Flaps (lower body)
    m.add_box((0, 0.88, 0), (0.42, 0.26, 0.25), coat)

    # Arms
    m.add_box((-0.32, 1.28, 0), (0.20, 0.15, 0.16), coat)
    m.add_box((-0.50, 1.28, 0), (0.20, 0.13, 0.13), skin)
    m.add_box((0.32, 1.28, 0), (0.20, 0.15, 0.16), coat)
    m.add_box((0.50, 1.28, 0), (0.20, 0.13, 0.13), skin)

    # Legs & Shoes
    m.add_box((-0.12, 0.45, 0), (0.16, 0.60, 0.18), scrubs)
    m.add_box((0.12, 0.45, 0), (0.16, 0.60, 0.18), scrubs)
    m.add_box((-0.12, 0.08, 0.04), (0.17, 0.16, 0.24), (0.15, 0.15, 0.15, 1.0))
    m.add_box((0.12, 0.08, 0.04), (0.17, 0.16, 0.24), (0.15, 0.15, 0.15, 1.0))

    m.export_glb(path)


def build_loot_crate(path: Path):
    m = MeshBuilder()
    wood = (0.55, 0.40, 0.25, 1.0)
    metal = (0.25, 0.28, 0.32, 1.0)
    latch = (0.80, 0.70, 0.20, 1.0)

    # Main wooden box body
    m.add_box((0, 0.45, 0), (1.10, 0.80, 0.80), wood)

    # Metal corner brackets (4 vertical corners)
    m.add_box((-0.53, 0.45, -0.38), (0.08, 0.82, 0.08), metal)
    m.add_box((0.53, 0.45, -0.38), (0.08, 0.82, 0.08), metal)
    m.add_box((-0.53, 0.45, 0.38), (0.08, 0.82, 0.08), metal)
    m.add_box((0.53, 0.45, 0.38), (0.08, 0.82, 0.08), metal)

    # Reinforcing horizontal bands
    m.add_box((0, 0.82, 0), (1.14, 0.08, 0.84), metal)
    m.add_box((0, 0.08, 0), (1.14, 0.08, 0.84), metal)

    # Center Dual Golden/Bronze Latches
    m.add_box((-0.25, 0.55, 0.41), (0.08, 0.16, 0.04), latch)
    m.add_box((0.25, 0.55, 0.41), (0.08, 0.16, 0.04), latch)

    # Side Carry Handles
    m.add_box((-0.57, 0.50, 0), (0.04, 0.10, 0.24), metal)
    m.add_box((0.57, 0.50, 0), (0.04, 0.10, 0.24), metal)

    m.export_glb(path)


def build_ambulance(path: Path):
    m = MeshBuilder()
    white = (0.92, 0.94, 0.95, 1.0)
    red = (0.85, 0.15, 0.15, 1.0)
    blue = (0.15, 0.40, 0.85, 1.0)
    glass = (0.20, 0.25, 0.30, 1.0)
    wheel = (0.12, 0.12, 0.12, 1.0)
    headlight = (1.0, 0.95, 0.40, 1.0)

    # Main Van Chassis (Length 3.8m, Height 1.8m, Width 1.8m)
    m.add_box((0, 1.15, -0.2), (1.70, 1.40, 3.20), white) # back cargo
    m.add_box((0, 0.95, 1.45), (1.68, 1.00, 1.00), white) # front hood

    # Windshield and Side Windows
    m.add_box((0, 1.35, 1.05), (1.50, 0.55, 0.10), glass)
    m.add_box((-0.86, 1.35, 0.0), (0.02, 0.45, 1.80), glass) # L window
    m.add_box((0.86, 1.35, 0.0), (0.02, 0.45, 1.80), glass)  # R window

    # Red Cross insignia on sides
    m.add_box((-0.87, 1.25, -0.3), (0.02, 0.45, 0.16), red) # L vertical
    m.add_box((-0.87, 1.25, -0.3), (0.02, 0.16, 0.45), red) # L horizontal
    m.add_box((0.87, 1.25, -0.3), (0.02, 0.45, 0.16), red)  # R vertical
    m.add_box((0.87, 1.25, -0.3), (0.02, 0.16, 0.45), red)  # R horizontal

    # Roof Emergency Siren Lightbar
    m.add_box((-0.35, 1.95, 0.90), (0.35, 0.14, 0.22), red)
    m.add_box((0.35, 1.95, 0.90), (0.35, 0.14, 0.22), blue)

    # Front Grille & Headlights
    m.add_box((0, 0.70, 1.96), (1.20, 0.25, 0.04), (0.15, 0.15, 0.15, 1.0))
    m.add_box((-0.60, 0.85, 1.96), (0.22, 0.16, 0.04), headlight)
    m.add_box((0.60, 0.85, 1.96), (0.22, 0.16, 0.04), headlight)

    # 4 Heavy Wheels
    m.add_cylinder((-0.82, 0.42, 1.10), 0.42, 0.28, wheel)
    m.add_cylinder((0.82, 0.42, 1.10), 0.42, 0.28, wheel)
    m.add_cylinder((-0.82, 0.42, -1.10), 0.42, 0.28, wheel)
    m.add_cylinder((0.82, 0.42, -1.10), 0.42, 0.28, wheel)

    m.export_glb(path)


def build_car_wreck(path: Path):
    m = MeshBuilder()
    rust = (0.55, 0.32, 0.20, 1.0) # rusted metal body
    dark_metal = (0.20, 0.18, 0.16, 1.0)
    wheel = (0.10, 0.10, 0.10, 1.0)

    # Smashed car body
    m.add_box((0, 0.55, 0), (1.60, 0.55, 3.20), rust)
    # Dented Cabin Roof
    m.add_box((0, 1.05, -0.2), (1.35, 0.50, 1.50), rust)

    # Smashed/Bent Front Hood
    m.add_box((0.08, 0.80, 1.15), (1.45, 0.15, 0.90), dark_metal)

    # Broken Flat Wheels
    m.add_cylinder((-0.80, 0.28, 0.90), 0.32, 0.26, wheel)
    m.add_cylinder((0.80, 0.28, 0.90), 0.32, 0.26, wheel)
    m.add_cylinder((-0.80, 0.28, -0.90), 0.32, 0.26, wheel)
    m.add_cylinder((0.80, 0.28, -0.90), 0.32, 0.26, wheel)

    m.export_glb(path)


def build_barricade(path: Path):
    m = MeshBuilder()
    red_barrel = (0.75, 0.22, 0.20, 1.0)
    blue_barrel = (0.18, 0.45, 0.70, 1.0)
    plank = (0.52, 0.38, 0.24, 1.0)
    sandbag = (0.78, 0.72, 0.55, 1.0)
    metal = (0.35, 0.38, 0.42, 1.0)

    # 2 Oil Barrels (Left and Right)
    m.add_cylinder((-0.75, 0.50, 0), 0.32, 1.00, red_barrel, segments=10)
    m.add_cylinder((0.75, 0.50, 0), 0.32, 1.00, blue_barrel, segments=10)

    # Crossed Wooden Heavy Planks across the barrels
    m.add_box((0, 0.75, 0.18), (2.10, 0.22, 0.08), plank)
    m.add_box((0, 0.35, 0.18), (2.10, 0.22, 0.08), plank)
    # Diagonal brace
    m.add_box((0, 0.55, 0.22), (1.40, 0.18, 0.06), (0.45, 0.32, 0.20, 1.0))

    # Sandbags piled in the center and base
    m.add_box((0, 0.12, 0.28), (0.90, 0.22, 0.35), sandbag)
    m.add_box((0, 0.30, 0.25), (0.75, 0.18, 0.30), sandbag)

    # Barbed Wire Top Rail
    m.add_box((0, 1.05, 0), (2.00, 0.05, 0.05), metal)

    m.export_glb(path)


def generate_all_models(output_dir: Path):
    output_dir.mkdir(parents=True, exist_ok=True)
    builders = [
        ("Survivor.glb", build_survivor),
        ("Zombie_Standard.glb", build_zombie_standard),
        ("Zombie_Runner.glb", build_zombie_runner),
        ("Zombie_Screamer.glb", build_zombie_screamer),
        ("Zombie_Tank.glb", build_zombie_tank),
        ("Dr_Aras.glb", build_dr_aras),
        ("Loot_Crate.glb", build_loot_crate),
        ("Ambulance.glb", build_ambulance),
        ("Car_Wreck.glb", build_car_wreck),
        ("Barricade.glb", build_barricade),
    ]

    print(f"\n=======================================================")
    print(f"🛠️ 10 TEMEL OYUN VARLIĞI 3D (.GLB) ÜRETİLİYOR...")
    print(f"   Hedef: {output_dir}")
    print(f"=======================================================\n")

    for idx, (filename, fn) in enumerate(builders, 1):
        target_path = output_dir / filename
        fn(target_path)
        size_kb = target_path.stat().st_size / 1024
        print(f"  [{idx}/10] ✓ {filename:<22} ({size_kb:.1f} KB)")

    print(f"\n✅ Tüm 10 model başarıyla oluşturuldu!\n")


if __name__ == "__main__":
    import sys
    out = Path("Assets/_Project/Models/TripoGenerated")
    if len(sys.argv) > 1:
        out = Path(sys.argv[1])
    generate_all_models(out)
