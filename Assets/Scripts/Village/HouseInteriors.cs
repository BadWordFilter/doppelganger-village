using System.Collections.Generic;
using UnityEngine;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 집 내부 룸 (어몽어스식 출입): 집 문 앞에서 E → 내부 진입, 내부 출구에서 E → 집 앞 복귀.
    /// 마을 주민 동물은 전부 집 안에 살고, 만나려면 집에 들어가야 한다.
    /// 룸은 마을 밖 좌표에 결정적으로 생성되어 전 클라이언트 동일 — 네트워크 동기화 불필요.
    /// </summary>
    public static class HouseInteriors
    {
        private const float RoomW = 11f, RoomD = 9f, WallH = 3.2f;

        private class Room
        {
            public Vector3 Base;         // 룸 중심 (바닥 y=0)
            public Vector3 ExteriorDoor; // 마을 쪽 집 문 앞 위치
        }

        private static readonly List<Room> _rooms = new();
        private static readonly Dictionary<int, int> _residentRoom = new(); // citizenId → room index

        private static Material _wallMat, _floorMat, _doorMat, _rugMat;

        public static void BuildAll(List<Transform> houses)
        {
            _rooms.Clear();
            _residentRoom.Clear();
            var old = GameObject.Find("HouseInteriors");
            if (old != null) Object.Destroy(old);
            var root = new GameObject("HouseInteriors").transform;
            EnsureMats();

            for (int i = 0; i < houses.Count; i++)
            {
                var basePos = new Vector3(240f + (i % 5) * 26f, 0f, 260f + (i / 5) * 26f);
                _rooms.Add(new Room
                {
                    Base = basePos,
                    ExteriorDoor = houses[i].position + houses[i].forward * 3.4f,
                });
                BuildRoom(root, basePos, i);
            }
        }

        private static void EnsureMats()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            Material Make(Color c)
            {
                var m = new Material(lit);
                m.SetColor("_BaseColor", c);
                m.SetFloat("_Smoothness", 0.1f);
                return m;
            }
            if (_wallMat == null) _wallMat = Make(new Color(0.82f, 0.74f, 0.60f));
            if (_floorMat == null) _floorMat = Make(new Color(0.52f, 0.38f, 0.24f));
            if (_doorMat == null) _doorMat = Make(new Color(0.28f, 0.19f, 0.11f));
            if (_rugMat == null) _rugMat = Make(new Color(0.62f, 0.32f, 0.28f));
        }

        private static void BuildRoom(Transform root, Vector3 basePos, int index)
        {
            var room = new GameObject($"HouseRoom_{index}").transform;
            room.SetParent(root, false);
            room.position = basePos;

            GameObject Box(string name, Vector3 localPos, Vector3 scale, Material mat)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(room, false);
                go.transform.localPosition = localPos;
                go.transform.localScale = scale;
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
                return go;
            }

            Box("Floor", new Vector3(0f, -0.15f, 0f), new Vector3(RoomW, 0.3f, RoomD), _floorMat);
            Box("Ceiling", new Vector3(0f, WallH + 0.15f, 0f), new Vector3(RoomW, 0.3f, RoomD), _wallMat);
            Box("WallN", new Vector3(0f, WallH * 0.5f, RoomD * 0.5f), new Vector3(RoomW, WallH, 0.3f), _wallMat);
            Box("WallS", new Vector3(0f, WallH * 0.5f, -RoomD * 0.5f), new Vector3(RoomW, WallH, 0.3f), _wallMat);
            Box("WallE", new Vector3(RoomW * 0.5f, WallH * 0.5f, 0f), new Vector3(0.3f, WallH, RoomD), _wallMat);
            Box("WallW", new Vector3(-RoomW * 0.5f, WallH * 0.5f, 0f), new Vector3(0.3f, WallH, RoomD), _wallMat);

            // 출구 문(남쪽 벽 안쪽 시각 표식) + 러그·가구
            var door = Box("DoorMark", new Vector3(0f, 1.15f, -RoomD * 0.5f + 0.2f), new Vector3(1.6f, 2.3f, 0.1f), _doorMat);
            Object.Destroy(door.GetComponent<Collider>()); // 통과 가능한 표식
            Box("Rug", new Vector3(0f, 0.02f, 0.4f), new Vector3(3.6f, 0.05f, 2.6f), _rugMat);
            Box("Bed", new Vector3(-RoomW * 0.5f + 1.6f, 0.3f, RoomD * 0.5f - 1.5f), new Vector3(2.6f, 0.6f, 1.6f), _rugMat);
            Box("Table", new Vector3(RoomW * 0.5f - 1.8f, 0.45f, RoomD * 0.5f - 1.6f), new Vector3(1.6f, 0.9f, 1.2f), _doorMat);

            var lampGo = new GameObject("Lamp");
            lampGo.transform.SetParent(room, false);
            lampGo.transform.localPosition = new Vector3(0f, WallH - 0.5f, 0f);
            var light = lampGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 10f;
            light.intensity = 1.7f;
            light.color = new Color(1f, 0.88f, 0.66f);
        }

        /// <summary>시민을 룸 안에 입주시키고 실내 위치를 돌려준다 (slot 0=주인, 1=동거).</summary>
        public static Vector3 AssignResident(int citizenId, int roomIndex, int slot)
        {
            if (roomIndex < 0 || roomIndex >= _rooms.Count) return Vector3.zero;
            _residentRoom[citizenId] = roomIndex;
            Vector3 offset = slot == 0 ? new Vector3(1.6f, 0f, 2.0f) : new Vector3(-2.0f, 0f, 1.7f);
            return _rooms[roomIndex].Base + offset;
        }

        /// <summary>이 위치가 어느 집 내부인가.</summary>
        public static bool Contains(Vector3 p)
        {
            foreach (var r in _rooms)
                if (Mathf.Abs(p.x - r.Base.x) < RoomW * 0.5f + 2f && Mathf.Abs(p.z - r.Base.z) < RoomD * 0.5f + 2f
                    && p.y > -2f && p.y < WallH + 2f)
                    return true;
            return false;
        }

        /// <summary>마을에서 가장 가까운 집 문 앞인가 → 내부 진입 지점.</summary>
        public static bool TryNearestDoor(Vector3 playerPos, out Vector3 interiorSpawn)
        {
            interiorSpawn = Vector3.zero;
            float best = 3.0f * 3.0f;
            int bestRoom = -1;
            for (int i = 0; i < _rooms.Count; i++)
            {
                float d = (playerPos - _rooms[i].ExteriorDoor).sqrMagnitude;
                if (d < best) { best = d; bestRoom = i; }
            }
            if (bestRoom < 0) return false;
            interiorSpawn = _rooms[bestRoom].Base + new Vector3(0f, 0.35f, -RoomD * 0.5f + 1.3f);
            return true;
        }

        /// <summary>내부 출구(문 표식) 근처인가 → 집 앞 복귀 지점.</summary>
        public static bool TryExit(Vector3 playerPos, out Vector3 exteriorPos)
        {
            exteriorPos = Vector3.zero;
            foreach (var r in _rooms)
            {
                Vector3 doorPos = r.Base + new Vector3(0f, 0f, -RoomD * 0.5f + 0.6f);
                if (Mathf.Abs(playerPos.x - r.Base.x) < RoomW * 0.5f + 1f
                    && (playerPos - doorPos).sqrMagnitude < 2.4f * 2.4f)
                {
                    exteriorPos = r.ExteriorDoor + Vector3.up * 0.35f;
                    return true;
                }
            }
            return false;
        }

        /// <summary>구출·도주·추격자 스폰용: 이 시민의 집 앞 위치 (실내 → 마을 좌표 변환).</summary>
        public static Vector3 ResidentExteriorDoor(int citizenId, Vector3 fallback)
        {
            if (_residentRoom.TryGetValue(citizenId, out int ri) && ri >= 0 && ri < _rooms.Count)
                return _rooms[ri].ExteriorDoor;
            return fallback;
        }
    }
}
