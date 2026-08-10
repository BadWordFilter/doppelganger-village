using UnityEngine;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 캐릭터 룩 마감 처리: 카툰 아웃라인(뒤집힌 헐) + 바닥 원형 그림자.
    /// 절차 조형 로우폴리 특유의 '날것' 인상을 지우는 후처리 — 스폰 시 1회 적용.
    /// </summary>
    public static class CharacterStyler
    {
        private static Material _outlineMat, _blobMat;

        public static void Apply(Transform root, float outlineScale = 1.045f)
        {
            if (_outlineMat == null) _outlineMat = Resources.Load<Material>("OutlineMat");
            if (_blobMat == null) _blobMat = Resources.Load<Material>("BlobShadowMat");
            if (root.Find("BlobShadow") != null || StageDirectionActor.FindDeep(root, "Outline") != null)
                return; // 이미 처리됨

            // 그림자 반경: 실제 렌더 바운드 기준
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            float radius = 0.5f;
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.15f;
            }

            if (_outlineMat != null)
            {
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.name == "Outline") continue;
                    var go = new GameObject("Outline", typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(mf.transform, false);
                    go.transform.localScale = Vector3.one * outlineScale;
                    go.GetComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                    var r = go.GetComponent<MeshRenderer>();
                    r.sharedMaterial = _outlineMat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            if (_blobMat != null)
            {
                var blob = GameObject.CreatePrimitive(PrimitiveType.Quad);
                blob.name = "BlobShadow";
                Object.Destroy(blob.GetComponent<Collider>());
                blob.transform.SetParent(root, false);
                blob.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                blob.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                blob.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
                var br = blob.GetComponent<MeshRenderer>();
                br.sharedMaterial = _blobMat;
                br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
