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


def _finish(obj, material, jitter, smooth=False):
    me = obj.data
    if jitter > 0:
        for v in me.vertices:
            v.co.x += random.uniform(-jitter, jitter)
            v.co.y += random.uniform(-jitter, jitter)
            v.co.z += random.uniform(-jitter, jitter)
    for p in me.polygons:
        p.use_smooth = smooth  # 구형 파트는 스무스 — "각진 게 안 보이게" (사용자 피드백)
    me.materials.clear()
    me.materials.append(material)
    return obj


def sphere(name, loc, scale, material, segments=10, rings=7, jitter=0.008, parent=None, smooth=True):
    # 사용자 피드백 반영: 세그먼트 하한을 올리고 지터를 줄여 훨씬 둥글게
    segments = max(segments, 20)
    rings = max(rings, 14)
    jitter *= 0.35
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=1.0, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    _finish(obj, material, jitter, smooth)
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
    head_mat = mat("sheep_face", (0.52, 0.43, 0.36)) if species == "sheep" else body_mat  # 검은 눈이 읽히게 밝은 탠
    head = sphere("Head", (0, -0.012 * s, 0.86 * s), (0.315 * s, 0.29 * s, 0.28 * s), head_mat, segments=12, rings=8)
    head.parent = root

    # ---- 얼굴 (Animal Hospital 레퍼런스 문법: 큰 검은 눈 + 흰 하이라이트 + 작은 ㅅ자 입, 납작한 얼굴) ----
    shine_mat = mat("eye_shine", (0.98, 0.98, 0.98))
    sphere("EyeL", (-0.105 * s, -0.272 * s, 0.90 * s), (0.075 * s, 0.032 * s, 0.10 * s), eye_mat, jitter=0, parent=head)
    sphere("EyeR", (0.105 * s, -0.272 * s, 0.90 * s), (0.075 * s, 0.032 * s, 0.10 * s), eye_mat, jitter=0, parent=head)
    sphere("EyeShineL", (-0.135 * s, -0.296 * s, 0.945 * s), (0.021 * s, 0.013 * s, 0.027 * s), shine_mat, jitter=0, parent=head)
    sphere("EyeShineR", (0.075 * s, -0.296 * s, 0.945 * s), (0.021 * s, 0.013 * s, 0.027 * s), shine_mat, jitter=0, parent=head)
    if species not in ("pig", "owl", "bat"):
        box("MouthL", (-0.033 * s, -0.292 * s, 0.795 * s), (0.055 * s, 0.013 * s, 0.015 * s), eye_mat, rot=(0, 0, 25), parent=head)
        box("MouthR", (0.033 * s, -0.292 * s, 0.795 * s), (0.055 * s, 0.013 * s, 0.015 * s), eye_mat, rot=(0, 0, -25), parent=head)

    # ---- 종별 얼굴 디테일 ----
    if species == "pig":
        sphere("Snout", (0, -0.30 * s, 0.83 * s), (0.085 * s, 0.05 * s, 0.062 * s), mat("pig_snout", (0.85, 0.55, 0.55)), segments=8, rings=5, parent=head)
    elif species == "owl":
        cone("Beak", (0, -0.30 * s, 0.845 * s), (0.036 * s, 0.036 * s, 0.055 * s), mat("beak", (0.92, 0.75, 0.25)), rot=(-100, 0, 0), verts=5, parent=head)

    # ---- 종별 귀·날개·꼬리 (사용자 피드백: 양·강아지·야행성 특징 강화) ----
    if species == "dog":
        # 늘어진 귀 + 한쪽 눈 갈색 반점 + 큰 꼬리 — 확실한 강아지 실루엣
        patch_mat = mat("dog_patch", (0.40, 0.26, 0.15))
        box("EarL", (-0.27 * s, 0.0, 0.90 * s), (0.10 * s, 0.14 * s, 0.30 * s), patch_mat, rot=(0, 38, 0), parent=head)
        box("EarR", (0.27 * s, 0.0, 0.90 * s), (0.10 * s, 0.14 * s, 0.30 * s), patch_mat, rot=(0, -38, 0), parent=head)
        sphere("Patch", (-0.125 * s, -0.248 * s, 0.94 * s), (0.09 * s, 0.06 * s, 0.09 * s), patch_mat, segments=10, rings=7, parent=head)
        cone("Tail", (0, 0.32 * s, 0.44 * s), (0.06 * s, 0.06 * s, 0.15 * s), body_mat, rot=(55, 0, 0), parent=body)
    elif species == "cat":
        cone("EarL", (-0.17 * s, 0.02 * s, 1.13 * s), (0.075 * s, 0.055 * s, 0.10 * s), body_mat, rot=(0, -12, 0), verts=4, parent=head)
        cone("EarR", (0.17 * s, 0.02 * s, 1.13 * s), (0.075 * s, 0.055 * s, 0.10 * s), body_mat, rot=(0, 12, 0), verts=4, parent=head)
        cone("Tail", (0, 0.33 * s, 0.35 * s), (0.05 * s, 0.05 * s, 0.17 * s), body_mat, rot=(75, 0, 0), parent=body)
    elif species == "wolf":
        # 큰 뾰족귀 + 짙은 갈기 + 굵은 꼬리 + 치켜올라간 눈썹 — 사나운 실루엣
        mane_mat = mat("wolf_mane", (0.30, 0.30, 0.36))
        cone("EarL", (-0.19 * s, 0.02 * s, 1.17 * s), (0.10 * s, 0.07 * s, 0.15 * s), mane_mat, rot=(0, -14, 0), verts=4, parent=head)
        cone("EarR", (0.19 * s, 0.02 * s, 1.17 * s), (0.10 * s, 0.07 * s, 0.15 * s), mane_mat, rot=(0, 14, 0), verts=4, parent=head)
        sphere("Mane", (0, 0.14 * s, 0.52 * s), (0.24 * s, 0.17 * s, 0.26 * s), mane_mat, segments=12, rings=8, jitter=0.02, parent=body)
        cone("Tail", (0, 0.36 * s, 0.32 * s), (0.085 * s, 0.085 * s, 0.22 * s), mane_mat, rot=(80, 0, 0), parent=body)
        box("BrowL", (-0.115 * s, -0.282 * s, 0.975 * s), (0.075 * s, 0.02 * s, 0.025 * s), mane_mat, rot=(0, 0, -18), parent=head)
        box("BrowR", (0.115 * s, -0.282 * s, 0.975 * s), (0.075 * s, 0.02 * s, 0.025 * s), mane_mat, rot=(0, 0, 18), parent=head)
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
        # 얼굴만 까맣고 온몸이 양털 뭉치 — 멀리서도 양
        wool_mat = mat("sheep_wool", (0.97, 0.95, 0.89))
        sphere("Wool", (0, 0.02 * s, 1.07 * s), (0.30 * s, 0.28 * s, 0.19 * s), wool_mat, jitter=0.03, parent=head)
        puffs = [(-0.23, -0.10, 1.00), (0.23, -0.10, 1.00), (-0.17, 0.15, 1.10), (0.17, 0.15, 1.10), (0.0, 0.21, 1.05)]
        for pi, (wx, wy, wz) in enumerate(puffs):
            sphere("WoolPuff" + str(pi), (wx * s, wy * s, wz * s), (0.095 * s, 0.095 * s, 0.085 * s), wool_mat, jitter=0.02, parent=head)
        sphere("BodyWool", (0, 0.02 * s, 0.37 * s), (0.33 * s, 0.30 * s, 0.28 * s), wool_mat, jitter=0.03, parent=body)
        box("EarL", (-0.28 * s, -0.02 * s, 0.86 * s), (0.13 * s, 0.06 * s, 0.05 * s), head_mat, rot=(0, 70, 0), parent=head)
        box("EarR", (0.28 * s, -0.02 * s, 0.86 * s), (0.13 * s, 0.06 * s, 0.05 * s), head_mat, rot=(0, -70, 0), parent=head)
    elif species == "owl":
        # 눈 뒤의 노란 눈테 + 귀깃 + 날개 — 한눈에 올빼미
        ring_mat = mat("owl_ring", (0.95, 0.82, 0.35))
        sphere("BigEyeL", (-0.105 * s, -0.258 * s, 0.90 * s), (0.10 * s, 0.026 * s, 0.128 * s), ring_mat, segments=12, rings=8, parent=head)
        sphere("BigEyeR", (0.105 * s, -0.258 * s, 0.90 * s), (0.10 * s, 0.026 * s, 0.128 * s), ring_mat, segments=12, rings=8, parent=head)
        cone("TuftL", (-0.18 * s, 0.02 * s, 1.14 * s), (0.05 * s, 0.05 * s, 0.11 * s), body_mat, rot=(0, -20, 0), verts=4, parent=head)
        cone("TuftR", (0.18 * s, 0.02 * s, 1.14 * s), (0.05 * s, 0.05 * s, 0.11 * s), body_mat, rot=(0, 20, 0), verts=4, parent=head)
        sphere("WingL", (-0.28 * s, 0.05 * s, 0.38 * s), (0.065 * s, 0.14 * s, 0.19 * s), mat("owl_wing", (0.40, 0.30, 0.19)), segments=10, rings=7, parent=body)
        sphere("WingR", (0.28 * s, 0.05 * s, 0.38 * s), (0.065 * s, 0.14 * s, 0.19 * s), mat("owl_wing", (0.40, 0.30, 0.19)), segments=10, rings=7, parent=body)
    elif species == "bat":
        # 큰 날개막 + 송곳니 + 큰 귀 — 한눈에 박쥐
        wing_mat = mat("bat_wing", (0.20, 0.17, 0.26))
        fang_mat = mat("fang", (0.97, 0.96, 0.92))
        box("WingL", (-0.46 * s, 0.03 * s, 0.44 * s), (0.42 * s, 0.035 * s, 0.28 * s), wing_mat, rot=(0, 30, 0), bevel=0.01, parent=body)
        box("WingR", (0.46 * s, 0.03 * s, 0.44 * s), (0.42 * s, 0.035 * s, 0.28 * s), wing_mat, rot=(0, -30, 0), bevel=0.01, parent=body)
        cone("EarL", (-0.14 * s, 0.02 * s, 1.16 * s), (0.065 * s, 0.05 * s, 0.14 * s), body_mat, verts=4, parent=head)
        cone("EarR", (0.14 * s, 0.02 * s, 1.16 * s), (0.065 * s, 0.05 * s, 0.14 * s), body_mat, verts=4, parent=head)
        cone("FangL", (-0.05 * s, -0.27 * s, 0.79 * s), (0.014 * s, 0.014 * s, 0.03 * s), fang_mat, rot=(180, 0, 0), verts=4, parent=head)
        cone("FangR", (0.05 * s, -0.27 * s, 0.79 * s), (0.014 * s, 0.014 * s, 0.03 * s), fang_mat, rot=(180, 0, 0), verts=4, parent=head)

    export(species)


def build_player():
    # 플레이어 = 고양이 (사용자 요청). 커스터마이징이 전신 색을 바꾸므로
    # 몸 전체를 한 머티리얼로, 눈·주둥이·코·귀 안쪽만 별도(틴트 제외 부위와 이름 일치).
    clear_scene()
    s = 1.05
    fur = mat("player_fur", (0.90, 0.53, 0.24))  # 기본 주황 — 런타임에 팔레트 색으로 교체
    muzzle_mat = mat("player_muzzle", (0.97, 0.95, 0.90))
    inner_mat = mat("player_inner_ear", (0.95, 0.62, 0.62))
    eye_mat = mat("eye", EYE)
    root = empty("root")

    body = sphere("Body", (0, 0, 0.36 * s), (0.27 * s, 0.24 * s, 0.28 * s), fur)
    body.parent = root
    sphere("FootL", (-0.11 * s, -0.02 * s, 0.055 * s), (0.095 * s, 0.125 * s, 0.06 * s), fur, parent=body)
    sphere("FootR", (0.11 * s, -0.02 * s, 0.055 * s), (0.095 * s, 0.125 * s, 0.06 * s), fur, parent=body)
    sphere("ArmL", (-0.25 * s, 0, 0.41 * s), (0.06 * s, 0.072 * s, 0.112 * s), fur, parent=body)
    sphere("ArmR", (0.25 * s, 0, 0.41 * s), (0.06 * s, 0.072 * s, 0.112 * s), fur, parent=body)
    cone("Tail", (0, 0.34 * s, 0.34 * s), (0.055 * s, 0.055 * s, 0.19 * s), fur, rot=(78, 0, 0), parent=body)

    head = sphere("Head", (0, -0.012 * s, 0.87 * s), (0.315 * s, 0.29 * s, 0.285 * s), fur)
    head.parent = root
    # 얼굴: Animal Hospital 문법 — 큰 검은 눈 + 하이라이트 + ㅅ자 입 (납작한 얼굴)
    shine = mat("eye_shine", (0.98, 0.98, 0.98))
    sphere("EyeL", (-0.105 * s, -0.272 * s, 0.90 * s), (0.075 * s, 0.032 * s, 0.10 * s), eye_mat, jitter=0, parent=head)
    sphere("EyeR", (0.105 * s, -0.272 * s, 0.90 * s), (0.075 * s, 0.032 * s, 0.10 * s), eye_mat, jitter=0, parent=head)
    sphere("EyeShineL", (-0.135 * s, -0.296 * s, 0.945 * s), (0.021 * s, 0.013 * s, 0.027 * s), shine, jitter=0, parent=head)
    sphere("EyeShineR", (0.075 * s, -0.296 * s, 0.945 * s), (0.021 * s, 0.013 * s, 0.027 * s), shine, jitter=0, parent=head)
    box("MouthL", (-0.033 * s, -0.292 * s, 0.795 * s), (0.055 * s, 0.013 * s, 0.015 * s), eye_mat, rot=(0, 0, 25), parent=head)
    box("MouthR", (0.033 * s, -0.292 * s, 0.795 * s), (0.055 * s, 0.013 * s, 0.015 * s), eye_mat, rot=(0, 0, -25), parent=head)
    cone("EarL", (-0.17 * s, 0.02 * s, 1.14 * s), (0.08 * s, 0.058 * s, 0.11 * s), fur, rot=(0, -12, 0), verts=4, parent=head)
    cone("EarR", (0.17 * s, 0.02 * s, 1.14 * s), (0.08 * s, 0.058 * s, 0.11 * s), fur, rot=(0, 12, 0), verts=4, parent=head)
    cone("InnerEarL", (-0.165 * s, -0.015 * s, 1.12 * s), (0.045 * s, 0.03 * s, 0.065 * s), inner_mat, rot=(0, -12, 0), verts=4, parent=head)
    cone("InnerEarR", (0.165 * s, -0.015 * s, 1.12 * s), (0.045 * s, 0.03 * s, 0.065 * s), inner_mat, rot=(0, 12, 0), verts=4, parent=head)

    export("player")


def build_chaser():
    clear_scene()
    flesh = mat("chaser_flesh", (0.24, 0.045, 0.05))
    eye = mat("chaser_eye", (1.0, 0.1, 0.05), emission=(1.0, 0.08, 0.03), emission_strength=3.0)
    root = empty("root")

    # 야위고 뒤틀린 직립체 — 귀여운 마을과 대비되는 실루엣 (의도적으로 각지고 거친 플랫 셰이딩 유지)
    body = sphere("Body", (0, 0, 0.95), (0.26, 0.20, 0.62), flesh, segments=9, rings=8, jitter=0.09, smooth=False)
    body.parent = root
    sphere("ArmL", (-0.30, -0.05, 0.85), (0.055, 0.06, 0.42), flesh, segments=7, rings=6, jitter=0.075, smooth=False, parent=body)
    sphere("ArmR", (0.30, -0.05, 0.85), (0.055, 0.06, 0.42), flesh, segments=7, rings=6, jitter=0.075, smooth=False, parent=body)

    head = sphere("Head", (0, -0.03, 1.72), (0.20, 0.19, 0.24), flesh, segments=10, rings=7, jitter=0.09, smooth=False)
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
