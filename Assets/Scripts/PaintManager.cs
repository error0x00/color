using UnityEngine;
using UnityEngine.InputSystem;

// 입력 처리 + 물감 줄기(PaintStroke) 생성 관리
public class PaintManager : MonoBehaviour
{
    [Header("Paint")]
    public Material paintMat;
    public float brushSize = 0.08f;

    // 현재 짜는 중인 물감 줄기
    private PaintStroke activeStroke;

    // 종이 오브젝트 (레이캐스트 대상)
    private GameObject paper;
    private Camera mainCam;

    // 현재 선택된 물감 색
    private Color currentColor = new Color(0.89f, 0.15f, 0.21f);

    // 마우스가 같은 자리에 머문 시간
    private Vector3 lastHitPos;
    private float stayTime;

    // 테스트용 색 목록 (숫자키 1~5)
    private Color[] colors = new Color[]
    {
        new Color(0.89f, 0.15f, 0.21f), // 1: 빨강
        new Color(0.98f, 0.93f, 0.35f), // 2: 노랑
        new Color(0.00f, 0.32f, 0.65f), // 3: 파랑
        new Color(1.00f, 1.00f, 1.00f), // 4: 흰색
        new Color(0.10f, 0.10f, 0.10f), // 5: 검정
    };

    void Start()
    {
        mainCam = Camera.main;
        SetupPaper();
    }

    // 종이: 유니티 기본 Plane 사용
    void SetupPaper()
    {
        paper = GameObject.CreatePrimitive(PrimitiveType.Plane);
        paper.name = "Paper";
        paper.transform.SetParent(transform);
        paper.transform.localPosition = Vector3.zero;
        paper.transform.localScale = new Vector3(0.5f, 1f, 0.5f);

        // 종이 색 설정
        MeshRenderer mr = paper.GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mr.material.color = new Color(0.92f, 0.90f, 0.87f);
    }

    void Update()
    {
        HandleColorSelect();
        HandlePaint();
    }

    // 숫자키로 색 선택
    void HandleColorSelect()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) currentColor = colors[0];
        if (kb.digit2Key.wasPressedThisFrame) currentColor = colors[1];
        if (kb.digit3Key.wasPressedThisFrame) currentColor = colors[2];
        if (kb.digit4Key.wasPressedThisFrame) currentColor = colors[3];
        if (kb.digit5Key.wasPressedThisFrame) currentColor = colors[4];
    }

    // 마우스 입력 처리
    void HandlePaint()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // 클릭 시작: 새 물감 줄기 생성
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector3 hitPos;
            if (RaycastPaper(out hitPos))
            {
                activeStroke = CreateStroke();
                activeStroke.AddPoint(hitPos);
                lastHitPos = hitPos;
                stayTime = 0f;
            }
        }
        // 드래그 중: 포인트 추가
        else if (mouse.leftButton.isPressed && activeStroke != null)
        {
            Vector3 hitPos;
            if (RaycastPaper(out hitPos))
            {
                float dist = Vector3.Distance(hitPos, lastHitPos);

                if (dist < 0.01f)
                {
                    // 같은 자리에 머무르면 굵어짐
                    stayTime += Time.deltaTime;
                    if (stayTime > 0.1f)
                    {
                        activeStroke.Grow(Time.deltaTime * 0.02f);
                    }
                }
                else
                {
                    activeStroke.AddPoint(hitPos);
                    lastHitPos = hitPos;
                    stayTime = 0f;
                }
            }
        }
        // 클릭 끝: 줄기 완성
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            activeStroke = null;
        }
    }

    // 종이에 레이캐스트
    bool RaycastPaper(out Vector3 hitPos)
    {
        hitPos = Vector3.zero;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == paper.transform)
            {
                hitPos = hit.point;
                // 종이 바로 위에 위치
                hitPos.y = brushSize + 0.01f;
                return true;
            }
        }
        return false;
    }

    // 물감 줄기 오브젝트 생성
    PaintStroke CreateStroke()
    {
        GameObject obj = new GameObject("Stroke");
        obj.transform.SetParent(transform);

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();

        // 머티리얼 복사해서 색 적용
        mr.material = new Material(paintMat);
        mr.material.color = currentColor;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        PaintStroke stroke = obj.AddComponent<PaintStroke>();
        stroke.Setup(currentColor, brushSize);

        return stroke;
    }
}
