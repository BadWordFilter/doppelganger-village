# -*- coding: utf-8 -*-
"""
Blender 헤드리스 캐릭터 제작 스크립트 — 도플갱어 마을 탈출.
토로형 2등신 동물 9종 + 사람 플레이어 + 추격자를 로우폴리 플랫 셰이딩으로 모델링해
Assets/Models/Characters/*.glb 로 익스포트한다.

실행: blender --background --python tools/blender_make_characters.py

계층 규칙 (Unity 연출 훅과 호환):
  root(Empty) ├─ Body (팔·다리·배·꼬리 등은 Body의 자식)
              └─ Head (귀·주둥이는 Head의 자식, EyeL/EyeR도 Head의 자식 — 머리 회전 시 함께 돈다)
정면은 Blender -Y (glTF 익스포트 후 Unity에서 +Z 정면이 되도록 루트를 보정한다).
"""
import math
import random

import bpy

EXPORT_DIR = r"C:\Users\probo\EscapefromDoppelgangerVillage\Assets\Models\Characters" + "\\"

random.seed(42)

# ---------- 헬퍼 ----------

_materials = {}


def mat(name, rgb, emission=None, emission_strength=0.0):
    key = (name, rgb, emission, emission_strength)
    if key in _materials:
        return _materials[key]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    # sRGB 의도 색 → 리니어 변환 (glTF Base Color는 리니어 — 안 하면 색이 연하게 뜬다)
    linear = tuple(pow(c, 2.2) for c in rgb)
    bsdf.inputs["Base Color"].default_value = (*linear, 1.0)
    bsdf.inputs["Roughness"].default_value = 1.0
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
        bsdf.inputs["Emission Strength"].default_value = emission_strength
    _materials[key] = m
    return m


def _finish(obj, material, jitter):
    me = obj.data
    if jitter > 0:
        for v in me.vertices:
            v.co.x += random.uniform(-jitter, jitter)
            v.co.y += random.uniform(-jitter, jitter)
            v.co.z += random.uniform(-jitter, jitter)
    for p in me.polygons:
        p.use_smooth = False  # 플랫 셰이딩 = 로우폴리 파셋 룩
    me.materials.clear()
    me.materials.append(material)
    return obj


def sphere(name, loc, scale, material, segments=10, rings=7, jitter=0.008, parent=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=1.0, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    _finish(obj, material, jitter)
    if parent is not None:
        obj.parent = parent
        obj.matrix_parent_inverse = parent.matrix_world.inverted()
    return obj


def cone(name, loc, scale, material, rot=(0, 0, 0), verts=6, jitter=0.004, parent=None):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=1.0, radius2=0.15, depth=2.0, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.rotation_euler = [math.radians(a) for a in rot]
    _finish(obj, material, jitter)
    if parent is not None:
        obj.parent = parent
        obj.matrix_parent_inverse = parent.matrix_world.inverted()
    return obj


def box(name, loc, scale, material, rot=(0, 0, 0), bevel=0.02, jitter=0.004, parent=None):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.rotation_euler = [math.radians(a) for a in rot]
    if bevel > 0:
        mod = obj.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    _finish(obj, material, jitter)
    if parent is not None:
        obj.parent = parent
        obj.matrix_parent_inverse = parent.matrix_world.inverted()
    return obj


def empty(name):
    e = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(e)
    return e


def clear_scene():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)


def export(name):
    import os
    os.makedirs(EXPORT_DIR, exist_ok=True)
    bpy.ops.export_scene.gltf(
        filepath=EXPORT_DIR + name + ".glb",
        export_format="GLB",
        export_yup=True,
        export_apply=True,
    )
    print("[export] " + name + ".glb")


# ---------- 공통 색 ----------

EYE = (0.05, 0.045, 0.05)
BELLY = (0.96, 0.93, 0.87)

SPECIES_COLOR = {
    "dog": (0.62, 0.42, 0.26),
    "cat": (0.55, 0.55, 0.58),
    "rabbit": (0.94, 0.93, 0.89),
    "pig": (0.95, 0.70, 0.70),
    "bear": (0.40, 0.28, 0.20),
    "sheep": (0.93, 0.90, 0.84),
    "owl": (0.52, 0.40, 0.26),
    "bat": (0.30, 0.28, 0.34),
    "wolf": (0.47, 0.47, 0.52),
}
SIZE = {"bear": 1.45, "wolf": 1.25, "pig": 1.15, "sheep": 1.15}


def build_animal(species):
    clear_scene()
    s = SIZE.get(species, 1.0)
    body_mat = mat(species + "_body", SPECIES_COLOR[species])
    belly_mat = mat("belly", BELLY)
    eye_mat = mat("eye", EYE)

    root = empty("root")

    # ---- 몸통 (직립, 작음) + 배 + 발 + 팔 ----
    body = sphere("Body", (0, 0, 0.34 * s), (0.29 * s, 0.26 * s, 0.30 * s), body_mat, segments=10, rings=7)
    body.parent = root
    sphere("Belly", (0, -0.115 * s, 0.325 * s), (0.185 * s, 0.13 * s, 0.20 * s), belly_mat, segments=8, rings=6, parent=body)
    sphere("FootL", (-0.115 * s, -0.02 * s, 0.055 * s), (0.10 * s, 0.13 * s, 0.062 * s), body_mat, segments=8, rings=5, parent=body)
    sphere("FootR", (0.115 * s, -0.02 * s, 0.055 * s), (0.10 * s, 0.13 * s, 0.062 * s), body_mat, segments=8, rings=5, parent=body)
    sphere("ArmL", (-0.245 * s, 0, 0.40 * s), (0.062 * s, 0.075 * s, 0.115 * s), body_mat, segments=8, rings=5, parent=body)
    sphere("ArmR", (0.245 * s, 0, 0.40 * s), (0.062 * s, 0.075 * s, 0.115 * s), body_mat, segments=8, rings=5, parent=body)

    # ---- 큰 머리 (2등신) ----
    head_mat = mat("sheep_face", (0.25, 0.22, 0.20)) if species == "sheep" else body_mat
    head = sphere("Head", (0, -0.012 * s, 0.86 * s), (0.315 * s, 0.29 * s, 0.28 * s), head_mat, segments=12, rings=8)
    head.parent = root

    # ---- 눈 (Head의 자식 — 머리와 함께 돈다) ----
    sphere("EyeL", (-0.115 * s, -0.245 * s, 0.905 * s), (0.034 * s, 0.02 * s, 0.048 * s), eye_mat, segments=8, rings=5, jitter=0, parent=head)
    sphere("EyeR", (0.115 * s, -0.245 * s, 0.905 * s), (0.034 * s, 0.02 * s, 0.048 * s), eye_mat, segments=8, rings=5, jitter=0, parent=head)

    # ---- 종별 얼굴 ----
    if species == "pig":
        sphere("Snout", (0, -0.28 * s, 0.83 * s), (0.085 * s, 0.05 * s, 0.062 * s), mat("pig_snout", (0.85, 0.55, 0.55)), segments=8, rings=5, parent=head)
    elif species in ("dog", "bear", "wolf"):
        sphere("Muzzle", (0, -0.26 * s, 0.80 * s), (0.105 * s, 0.075 * s, 0.075 * s), belly_mat, segments=8, rings=5, parent=head)
        sphere("NoseTip", (0, -0.325 * s, 0.835 * s), (0.036 * s, 0.026 * s, 0.026 * s), eye_mat, segments=6, rings=4, jitter=0, parent=head)
    elif species == "owl":
        cone("Beak", (0, -0.29 * s, 0.86 * s), (0.032 * s, 0.032 * s, 0.05 * s), mat("beak", (0.92, 0.75, 0.25)), rot=(-100, 0, 0), verts=5, parent=head)
        sphere("BigEyeL", (-0.115 * s, -0.25 * s, 0.905 * s), (0.062 * s, 0.024 * s, 0.062 * s), mat("owl_ring", (0.95, 0.82, 0.35)), segments=8, rings=5, parent=head)
        sphere("BigEyeR", (0.115 * s, -0.25 * s, 0.905 * s), (0.062 * s, 0.024 * s, 0.062 * s), mat("owl_ring", (0.95, 0.82, 0.35)), segments=8, rings=5, parent=head)
    else:
        sphere("NoseTip", (0, -0.29 * s, 0.855 * s), (0.026 * s, 0.02 * s, 0.02 * s), eye_mat, segments=6, rings=4, jitter=0, parent=head)

    # ---- 종별 귀·날개·꼬리 ----
    if species == "dog":
        box("EarL", (-0.26 * s, 0.01 * s, 1.02 * s), (0.09 * s, 0.13 * s, 0.24 * s), body_mat, rot=(0, 25, 0), parent=head)
        box("EarR", (0.26 * s, 0.01 * s, 1.02 * s), (0.09 * s, 0.13 * s, 0.24 * s), body_mat, rot=(0, -25, 0), parent=head)
        cone("Tail", (0, 0.30 * s, 0.42 * s), (0.045 * s, 0.045 * s, 0.11 * s), body_mat, rot=(60, 0, 0), parent=body)
    elif species in ("cat", "wolf"):
        cone("EarL", (-0.17 * s, 0.02 * s, 1.13 * s), (0.075 * s, 0.055 * s, 0.10 * s), body_mat, rot=(0, -12, 0), verts=4, parent=head)
        cone("EarR", (0.17 * s, 0.02 * s, 1.13 * s), (0.075 * s, 0.055 * s, 0.10 * s), body_mat, rot=(0, 12, 0), verts=4, parent=head)
        cone("Tail", (0, 0.33 * s, 0.35 * s), (0.05 * s, 0.05 * s, 0.17 * s), body_mat, rot=(75, 0, 0), parent=body)
    elif species == "rabbit":
        sphere("EarL", (-0.115 * s, 0.02 * s, 1.30 * s), (0.05 * s, 0.035 * s, 0.20 * s), body_mat, segments=8, rings=6, parent=head)
        sphere("EarR", (0.115 * s, 0.02 * s, 1.30 * s), (0.05 * s, 0.035 * s, 0.20 * s), body_mat, segments=8, rings=6, parent=head)
        sphere("Tail", (0, 0.28 * s, 0.30 * s), (0.055 * s, 0.055 * s, 0.055 * s), belly_mat, segments=6, rings=5, parent=body)
    elif species == "pig":
        box("EarL", (-0.21 * s, 0.02 * s, 1.07 * s), (0.10 * s, 0.05 * s, 0.11 * s), body_mat, rot=(0, 40, 0), parent=head)
        box("EarR", (0.21 * s, 0.02 * s, 1.07 * s), (0.10 * s, 0.05 * s, 0.11 * s), body_mat, rot=(0, -40, 0), parent=head)
    elif species == "bear":
        sphere("EarL", (-0.20 * s, 0.02 * s, 1.09 * s), (0.075 * s, 0.045 * s, 0.075 * s), body_mat, segments=8, rings=5, parent=head)
        sphere("EarR", (0.20 * s, 0.02 * s, 1.09 * s), (0.075 * s, 0.045 * s, 0.075 * s), body_mat, segments=8, rings=5, parent=head)
    elif species == "sheep":
        sphere("Wool", (0, 0.015 * s, 1.05 * s), (0.26 * s, 0.24 * s, 0.15 * s), mat("sheep_wool", SPECIES_COLOR["sheep"]), segments=9, rings=6, jitter=0.02, parent=head)
        box("EarL", (-0.28 * s, -0.02 * s, 0.86 * s), (0.13 * s, 0.06 * s, 0.05 * s), head_mat, rot=(0, 70, 0), parent=head)
        box("EarR", (0.28 * s, -0.02 * s, 0.86 * s), (0.13 * s, 0.06 * s, 0.05 * s), head_mat, rot=(0, -70, 0), parent=head)
    elif species == "owl":
        cone("TuftL", (-0.17 * s, 0.02 * s, 1.12 * s), (0.045 * s, 0.045 * s, 0.09 * s), body_mat, rot=(0, -20, 0), verts=4, parent=head)
        cone("TuftR", (0.17 * s, 0.02 * s, 1.12 * s), (0.045 * s, 0.045 * s, 0.09 * s), body_mat, rot=(0, 20, 0), verts=4, parent=head)
        sphere("WingL", (-0.27 * s, 0.05 * s, 0.38 * s), (0.055 * s, 0.13 * s, 0.17 * s), body_mat, segments=8, rings=6, parent=body)
        sphere("WingR", (0.27 * s, 0.05 * s, 0.38 * s), (0.055 * s, 0.13 * s, 0.17 * s), body_mat, segments=8, rings=6, parent=body)
    elif species == "bat":
        box("WingL", (-0.38 * s, 0.03 * s, 0.42 * s), (0.30 * s, 0.03 * s, 0.20 * s), body_mat, rot=(0, 28, 0), bevel=0.01, parent=body)
        box("WingR", (0.38 * s, 0.03 * s, 0.42 * s), (0.30 * s, 0.03 * s, 0.20 * s), body_mat, rot=(0, -28, 0), bevel=0.01, parent=body)
        cone("EarL", (-0.13 * s, 0.02 * s, 1.13 * s), (0.05 * s, 0.04 * s, 0.10 * s), body_mat, verts=4, parent=head)
        cone("EarR", (0.13 * s, 0.02 * s, 1.13 * s), (0.05 * s, 0.04 * s, 0.10 * s), body_mat, verts=4, parent=head)

    export(species)


def build_player():
    # 2등신 치비 플레이어 (동물들과 같은 비율 문법 — 사용자 요청)
    clear_scene()
    pants = mat("pants", (0.30, 0.34, 0.45))
    shirt = mat("shirt", (0.90, 0.53, 0.24))
    skin = mat("skin", (0.98, 0.87, 0.73))
    hair = mat("hair", (0.28, 0.20, 0.14))
    root = empty("root")

    body = sphere("Body", (0, 0, 0.38), (0.24, 0.21, 0.24), shirt, segments=10, rings=7)
    body.parent = root
    sphere("FootL", (-0.10, -0.02, 0.06), (0.09, 0.12, 0.055), pants, segments=8, rings=5, parent=body)
    sphere("FootR", (0.10, -0.02, 0.06), (0.09, 0.12, 0.055), pants, segments=8, rings=5, parent=body)
    sphere("ArmL", (-0.245, 0, 0.42), (0.06, 0.07, 0.11), shirt, segments=8, rings=5, parent=body)
    sphere("ArmR", (0.245, 0, 0.42), (0.06, 0.07, 0.11), shirt, segments=8, rings=5, parent=body)
    sphere("HandL", (-0.245, 0, 0.30), (0.05, 0.05, 0.05), skin, segments=6, rings=5, parent=body)
    sphere("HandR", (0.245, 0, 0.30), (0.05, 0.05, 0.05), skin, segments=6, rings=5, parent=body)

    head = sphere("Head", (0, -0.01, 0.88), (0.315, 0.29, 0.29), skin, segments=12, rings=8)
    head.parent = root
    sphere("Hair", (0, 0.03, 1.02), (0.325, 0.30, 0.20), hair, segments=10, rings=6, parent=head)
    sphere("EyeL", (-0.11, -0.25, 0.92), (0.032, 0.018, 0.045), mat("eye", EYE), segments=6, rings=4, jitter=0, parent=head)
    sphere("EyeR", (0.11, -0.25, 0.92), (0.032, 0.018, 0.045), mat("eye", EYE), segments=6, rings=4, jitter=0, parent=head)

    export("player")


def build_chaser():
    clear_scene()
    flesh = mat("chaser_flesh", (0.24, 0.045, 0.05))
    eye = mat("chaser_eye", (1.0, 0.1, 0.05), emission=(1.0, 0.08, 0.03), emission_strength=3.0)
    root = empty("root")

    # 야위고 뒤틀린 직립체 — 귀여운 마을과 대비되는 실루엣
    body = sphere("Body", (0, 0, 0.95), (0.26, 0.20, 0.62), flesh, segments=9, rings=8, jitter=0.03)
    body.parent = root
    sphere("ArmL", (-0.30, -0.05, 0.85), (0.055, 0.06, 0.42), flesh, segments=7, rings=6, jitter=0.025, parent=body)
    sphere("ArmR", (0.30, -0.05, 0.85), (0.055, 0.06, 0.42), flesh, segments=7, rings=6, jitter=0.025, parent=body)

    head = sphere("Head", (0, -0.03, 1.72), (0.20, 0.19, 0.24), flesh, segments=10, rings=7, jitter=0.03)
    head.parent = root
    head.rotation_euler = (math.radians(-9), 0, math.radians(7))  # 목이 어긋난 듯한 기울임
    cone("HornL", (-0.14, 0.02, 1.97), (0.05, 0.05, 0.14), flesh, rot=(0, -28, 0), verts=5, parent=head)
    cone("HornR", (0.14, 0.02, 1.97), (0.05, 0.05, 0.14), flesh, rot=(0, 28, 0), verts=5, parent=head)
    sphere("EyeL", (-0.075, -0.165, 1.76), (0.038, 0.02, 0.05), eye, segments=6, rings=4, jitter=0, parent=head)
    sphere("EyeR", (0.075, -0.165, 1.74), (0.05, 0.02, 0.062), eye, segments=6, rings=4, jitter=0, parent=head)  # 비대칭 눈

    export("chaser")


for sp in SPECIES_COLOR:
    build_animal(sp)
build_player()
build_chaser()
print("ALL CHARACTERS EXPORTED")
