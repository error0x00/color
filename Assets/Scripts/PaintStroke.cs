using UnityEngine;
using System.Collections.Generic;

// 물감 한 줄기: 드래그 경로를 따라 튜브형 메시를 생성
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PaintStroke : MonoBehaviour
{
    // 튜브 단면의 꼭짓점 수 (많을수록 부드러움)
    private int sides = 12;

    // 물감 굵기
    private float radius = 0.08f;

    // 경로 포인트 목록
    private List<Vector3> points = new List<Vector3>();

    // 메시 데이터
    private List<Vector3> verts = new List<Vector3>();
    private List<int> tris = new List<int>();
    private List<Vector3> normals = new List<Vector3>();

    private Mesh mesh;
    private Color paintColor;

    // 초기화: 색과 굵기 설정
    public void Setup(Color color, float size)
    {
        paintColor = color;
        radius = size;

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // 머티리얼에 색 적용
        MeshRenderer mr = GetComponent<MeshRenderer>();
        mr.material.color = paintColor;
    }

    // 새 경로 포인트 추가
    public void AddPoint(Vector3 worldPos)
    {
        // 너무 가까운 포인트는 무시
        if (points.Count > 0)
        {
            float dist = Vector3.Distance(worldPos, points[points.Count - 1]);
            if (dist < radius * 0.3f) return;
        }

        points.Add(worldPos);

        if (points.Count >= 2)
        {
            RebuildMesh();
        }
        else
        {
            // 첫 포인트: 반구 캡만 생성
            BuildStartCap(worldPos);
        }
    }

    // 한 자리에 머무를 때 굵기 증가
    public void Grow(float amount)
    {
        radius += amount;
        if (points.Count >= 1)
        {
            RebuildMesh();
        }
    }

    // 전체 메시를 재구성
    void RebuildMesh()
    {
        verts.Clear();
        tris.Clear();
        normals.Clear();

        // 각 포인트마다 원형 단면 생성
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 forward;
            if (i < points.Count - 1)
                forward = (points[i + 1] - points[i]).normalized;
            else if (i > 0)
                forward = (points[i] - points[i - 1]).normalized;
            else
                forward = Vector3.forward;

            // forward가 up과 평행하면 right 계산이 깨지므로 보정
            Vector3 up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.99f)
                up = Vector3.forward;

            Vector3 right = Vector3.Cross(up, forward).normalized;
            Vector3 localUp = Vector3.Cross(forward, right).normalized;

            // 원형 단면 꼭짓점
            for (int s = 0; s <= sides; s++)
            {
                float angle = (float)s / sides * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                // 아래쪽 반원만 종이에 닿고 위쪽은 둥글게
                Vector3 dir = right * cos + localUp * sin;
                Vector3 pos = points[i] + dir * radius;

                // 종이 아래로 내려가지 않도록 제한
                if (pos.y < 0.001f) pos.y = 0.001f;

                verts.Add(pos);
                normals.Add(dir);
            }
        }

        // 단면 사이를 삼각형으로 연결
        int ring = sides + 1;
        for (int i = 0; i < points.Count - 1; i++)
        {
            for (int s = 0; s < sides; s++)
            {
                int cur = i * ring + s;
                int next = cur + ring;

                tris.Add(cur);
                tris.Add(next);
                tris.Add(cur + 1);

                tris.Add(cur + 1);
                tris.Add(next);
                tris.Add(next + 1);
            }
        }

        // 시작 캡 (반구)
        AddCap(0, -1);

        // 끝 캡 (반구)
        AddCap(points.Count - 1, 1);

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
    }

    // 반구형 캡 추가
    void AddCap(int pointIdx, int direction)
    {
        Vector3 center = points[pointIdx];

        Vector3 forward;
        if (direction < 0 && points.Count > 1)
            forward = (points[0] - points[1]).normalized;
        else if (direction > 0 && points.Count > 1)
            forward = (points[points.Count - 1] - points[points.Count - 2]).normalized;
        else
            forward = Vector3.forward;

        Vector3 up = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.99f)
            up = Vector3.forward;

        Vector3 right = Vector3.Cross(up, forward).normalized;
        Vector3 localUp = Vector3.Cross(forward, right).normalized;

        int capRings = 4;
        int centerIdx = verts.Count;

        // 캡 꼭짓점: 반구 형태
        for (int r = 1; r <= capRings; r++)
        {
            float phi = (float)r / capRings * Mathf.PI * 0.5f;
            float ringRadius = radius * Mathf.Cos(phi);
            float offset = radius * Mathf.Sin(phi) * direction;

            for (int s = 0; s <= sides; s++)
            {
                float angle = (float)s / sides * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                Vector3 dir = right * cos + localUp * sin;
                Vector3 pos = center + forward * offset + dir * ringRadius;

                if (pos.y < 0.001f) pos.y = 0.001f;

                verts.Add(pos);
                normals.Add((dir * Mathf.Cos(phi) + forward * direction * Mathf.Sin(phi)).normalized);
            }
        }

        // 꼭대기 점
        Vector3 tip = center + forward * (radius * direction);
        if (tip.y < 0.001f) tip.y = 0.001f;
        int tipIdx = verts.Count;
        verts.Add(tip);
        normals.Add(forward * direction);

        // 본체 단면과 첫 번째 캡 링 연결
        int ring = sides + 1;
        int bodyRingStart = pointIdx * ring;
        int capFirstRing = centerIdx;

        for (int s = 0; s < sides; s++)
        {
            int a = bodyRingStart + s;
            int b = capFirstRing + s;

            if (direction > 0)
            {
                tris.Add(a);
                tris.Add(b);
                tris.Add(a + 1);
                tris.Add(a + 1);
                tris.Add(b);
                tris.Add(b + 1);
            }
            else
            {
                tris.Add(a);
                tris.Add(a + 1);
                tris.Add(b);
                tris.Add(a + 1);
                tris.Add(b + 1);
                tris.Add(b);
            }
        }

        // 캡 링끼리 연결
        for (int r = 0; r < capRings - 1; r++)
        {
            for (int s = 0; s < sides; s++)
            {
                int cur = centerIdx + r * ring + s;
                int next = cur + ring;

                if (direction > 0)
                {
                    tris.Add(cur);
                    tris.Add(next);
                    tris.Add(cur + 1);
                    tris.Add(cur + 1);
                    tris.Add(next);
                    tris.Add(next + 1);
                }
                else
                {
                    tris.Add(cur);
                    tris.Add(cur + 1);
                    tris.Add(next);
                    tris.Add(cur + 1);
                    tris.Add(next + 1);
                    tris.Add(next);
                }
            }
        }

        // 마지막 링과 꼭대기 점 연결
        int lastRing = centerIdx + (capRings - 1) * ring;
        for (int s = 0; s < sides; s++)
        {
            if (direction > 0)
            {
                tris.Add(lastRing + s);
                tris.Add(tipIdx);
                tris.Add(lastRing + s + 1);
            }
            else
            {
                tris.Add(lastRing + s);
                tris.Add(lastRing + s + 1);
                tris.Add(tipIdx);
            }
        }
    }

    // 첫 포인트만 있을 때 구 형태로 표시
    void BuildStartCap(Vector3 center)
    {
        verts.Clear();
        tris.Clear();
        normals.Clear();

        int rings = 8;
        int ring = sides + 1;

        for (int r = 0; r <= rings; r++)
        {
            float phi = (float)r / rings * Mathf.PI;
            float ringRadius = radius * Mathf.Sin(phi);
            float y = radius * Mathf.Cos(phi);

            for (int s = 0; s <= sides; s++)
            {
                float angle = (float)s / sides * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 pos = center + dir * ringRadius + Vector3.up * y;

                if (pos.y < 0.001f) pos.y = 0.001f;

                verts.Add(pos);
                normals.Add(new Vector3(dir.x * Mathf.Sin(phi), Mathf.Cos(phi), dir.z * Mathf.Sin(phi)));
            }
        }

        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < sides; s++)
            {
                int cur = r * ring + s;
                int next = cur + ring;

                tris.Add(cur);
                tris.Add(next);
                tris.Add(cur + 1);
                tris.Add(cur + 1);
                tris.Add(next);
                tris.Add(next + 1);
            }
        }

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
    }
}
