from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    BaseDocTemplate,
    Frame,
    KeepTogether,
    PageBreak,
    PageTemplate,
    Paragraph,
    Spacer,
    Table,
    TableStyle,
)


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "output" / "pdf" / "unity_2d_tilemap_design_guide_ko.pdf"
OUTPUT.parent.mkdir(parents=True, exist_ok=True)

FONT_REGULAR = r"C:\Windows\Fonts\malgun.ttf"
FONT_BOLD = r"C:\Windows\Fonts\malgunbd.ttf"
pdfmetrics.registerFont(TTFont("Malgun", FONT_REGULAR))
pdfmetrics.registerFont(TTFont("Malgun-Bold", FONT_BOLD))

PAGE_W, PAGE_H = A4
MARGIN_X = 19 * mm
MARGIN_TOP = 20 * mm
MARGIN_BOTTOM = 18 * mm

NAVY = colors.HexColor("#17324D")
BLUE = colors.HexColor("#2A6F97")
SKY = colors.HexColor("#EAF4FA")
GREEN = colors.HexColor("#4F7D54")
PALE_GREEN = colors.HexColor("#EEF5EA")
GOLD = colors.HexColor("#D39B39")
PALE_GOLD = colors.HexColor("#FFF7E6")
TEXT = colors.HexColor("#25313B")
MUTED = colors.HexColor("#63717D")
LINE = colors.HexColor("#D9E2E8")
WHITE = colors.white


def header_footer(canvas, doc):
    canvas.saveState()
    if doc.page > 1:
        canvas.setFillColor(NAVY)
        canvas.setFont("Malgun-Bold", 8.5)
        canvas.drawString(MARGIN_X, PAGE_H - 11 * mm, "UNITY 2D TOP-DOWN MAP DESIGN")
        canvas.setStrokeColor(LINE)
        canvas.setLineWidth(0.6)
        canvas.line(MARGIN_X, PAGE_H - 14 * mm, PAGE_W - MARGIN_X, PAGE_H - 14 * mm)

    canvas.setStrokeColor(LINE)
    canvas.setLineWidth(0.5)
    canvas.line(MARGIN_X, 12 * mm, PAGE_W - MARGIN_X, 12 * mm)
    canvas.setFillColor(MUTED)
    canvas.setFont("Malgun", 8)
    canvas.drawString(MARGIN_X, 8 * mm, "SimpleGame - Tilemap 설계 정리본")
    canvas.drawRightString(PAGE_W - MARGIN_X, 8 * mm, f"{doc.page}")
    canvas.restoreState()


doc = BaseDocTemplate(
    str(OUTPUT),
    pagesize=A4,
    leftMargin=MARGIN_X,
    rightMargin=MARGIN_X,
    topMargin=MARGIN_TOP,
    bottomMargin=MARGIN_BOTTOM,
    title="Unity 2D 탑다운 맵 Tilemap 설계 가이드",
    author="OpenAI Codex",
    subject="SimpleGame 프로젝트의 Tilemap 및 Prefab 기반 맵 구조 설계",
)

frame = Frame(
    MARGIN_X,
    MARGIN_BOTTOM,
    PAGE_W - 2 * MARGIN_X,
    PAGE_H - MARGIN_TOP - MARGIN_BOTTOM,
    id="content",
)
doc.addPageTemplates(PageTemplate(id="main", frames=[frame], onPage=header_footer))

styles = getSampleStyleSheet()
styles.add(
    ParagraphStyle(
        name="TitleKo",
        fontName="Malgun-Bold",
        fontSize=25,
        leading=34,
        textColor=WHITE,
        alignment=TA_LEFT,
        spaceAfter=5 * mm,
    )
)
styles.add(
    ParagraphStyle(
        name="SubtitleKo",
        fontName="Malgun",
        fontSize=11,
        leading=17,
        textColor=colors.HexColor("#D9EAF3"),
    )
)
styles.add(
    ParagraphStyle(
        name="H1Ko",
        fontName="Malgun-Bold",
        fontSize=17,
        leading=23,
        textColor=NAVY,
        spaceBefore=4 * mm,
        spaceAfter=3 * mm,
        keepWithNext=True,
    )
)
styles.add(
    ParagraphStyle(
        name="H2Ko",
        fontName="Malgun-Bold",
        fontSize=12.5,
        leading=18,
        textColor=BLUE,
        spaceBefore=3 * mm,
        spaceAfter=2 * mm,
        keepWithNext=True,
    )
)
styles.add(
    ParagraphStyle(
        name="BodyKo",
        fontName="Malgun",
        fontSize=9.6,
        leading=15.5,
        textColor=TEXT,
        spaceAfter=2.2 * mm,
        wordWrap="CJK",
    )
)
styles.add(
    ParagraphStyle(
        name="SmallKo",
        fontName="Malgun",
        fontSize=8.4,
        leading=13,
        textColor=TEXT,
        wordWrap="CJK",
    )
)
styles.add(
    ParagraphStyle(
        name="SmallBoldKo",
        fontName="Malgun-Bold",
        fontSize=8.5,
        leading=13,
        textColor=NAVY,
        wordWrap="CJK",
    )
)
styles.add(
    ParagraphStyle(
        name="SmallBoldWhiteKo",
        fontName="Malgun-Bold",
        fontSize=8.5,
        leading=13,
        textColor=WHITE,
        wordWrap="CJK",
    )
)
styles.add(
    ParagraphStyle(
        name="BulletKo",
        fontName="Malgun",
        fontSize=9.4,
        leading=15,
        textColor=TEXT,
        leftIndent=5 * mm,
        firstLineIndent=-3.5 * mm,
        bulletIndent=0,
        spaceAfter=1.4 * mm,
        wordWrap="CJK",
    )
)
styles.add(
    ParagraphStyle(
        name="CodeKo",
        fontName="Malgun",
        fontSize=8.7,
        leading=14.5,
        textColor=colors.HexColor("#E9F1F5"),
        leftIndent=2 * mm,
        rightIndent=2 * mm,
    )
)
styles.add(
    ParagraphStyle(
        name="CalloutTitle",
        fontName="Malgun-Bold",
        fontSize=10,
        leading=14,
        textColor=NAVY,
        spaceAfter=1.5 * mm,
    )
)


def p(text, style="BodyKo"):
    return Paragraph(text, styles[style])


def bullet(text):
    return Paragraph(f"• {text}", styles["BulletKo"])


def section(title):
    return Paragraph(title, styles["H1Ko"])


def subsection(title):
    return Paragraph(title, styles["H2Ko"])


def callout(title, text, color=SKY, accent=BLUE):
    table = Table(
        [[Paragraph(title, styles["CalloutTitle"])], [Paragraph(text, styles["BodyKo"])]],
        colWidths=[PAGE_W - 2 * MARGIN_X - 8 * mm],
        hAlign="LEFT",
    )
    table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), color),
                ("BOX", (0, 0), (-1, -1), 0.7, accent),
                ("LINEBEFORE", (0, 0), (0, -1), 4, accent),
                ("LEFTPADDING", (0, 0), (-1, -1), 6 * mm),
                ("RIGHTPADDING", (0, 0), (-1, -1), 5 * mm),
                ("TOPPADDING", (0, 0), (-1, 0), 4 * mm),
                ("BOTTOMPADDING", (0, 0), (-1, 0), 1 * mm),
                ("TOPPADDING", (0, 1), (-1, 1), 0),
                ("BOTTOMPADDING", (0, 1), (-1, 1), 3 * mm),
            ]
        )
    )
    return table


def data_table(headers, rows, widths):
    data = [[Paragraph(h, styles["SmallBoldWhiteKo"]) for h in headers]]
    for row in rows:
        data.append([Paragraph(str(cell), styles["SmallKo"]) for cell in row])
    table = Table(data, colWidths=widths, repeatRows=1, hAlign="LEFT")
    table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), NAVY),
                ("TEXTCOLOR", (0, 0), (-1, 0), WHITE),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("GRID", (0, 0), (-1, -1), 0.5, LINE),
                ("ROWBACKGROUNDS", (0, 1), (-1, -1), [WHITE, colors.HexColor("#F7FAFC")]),
                ("LEFTPADDING", (0, 0), (-1, -1), 3 * mm),
                ("RIGHTPADDING", (0, 0), (-1, -1), 3 * mm),
                ("TOPPADDING", (0, 0), (-1, -1), 2.5 * mm),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 2.5 * mm),
            ]
        )
    )
    return table


story = []

# Cover
cover = Table(
    [
        [Paragraph("Unity 2D 탑다운 맵<br/>Tilemap 설계 가이드", styles["TitleKo"])],
        [
            Paragraph(
                "SimpleGame 프로젝트를 위한 실무형 지형, 오브젝트, 충돌 및 정렬 구조",
                styles["SubtitleKo"],
            )
        ],
        [Spacer(1, 18 * mm)],
        [
            Paragraph(
                "작성일 2026-07-25<br/>대상 환경 Unity 6.3 / URP 2D / Tilemap Extras",
                styles["SubtitleKo"],
            )
        ],
    ],
    colWidths=[PAGE_W - 2 * MARGIN_X],
    rowHeights=[52 * mm, 26 * mm, 38 * mm, 25 * mm],
)
cover.setStyle(
    TableStyle(
        [
            ("BACKGROUND", (0, 0), (-1, -1), NAVY),
            ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
            ("LEFTPADDING", (0, 0), (-1, -1), 13 * mm),
            ("RIGHTPADDING", (0, 0), (-1, -1), 13 * mm),
            ("TOPPADDING", (0, 0), (-1, -1), 8 * mm),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 8 * mm),
        ]
    )
)
story.append(Spacer(1, 18 * mm))
story.append(cover)
story.append(Spacer(1, 14 * mm))
story.append(
    callout(
        "핵심 결론",
        "넓고 반복되는 지형은 Tilemap으로, 충돌·상호작용·애니메이션이 있는 요소는 Prefab GameObject로 관리한다. "
        "현재 SpriteRenderer 기반 맵은 빠른 시각 검증용 목업이며, 실제 제작 단계에서는 Tilemap과 Prefab을 결합한 하이브리드 구조로 전환하는 것이 적합하다.",
        color=PALE_GOLD,
        accent=GOLD,
    )
)
story.append(PageBreak())

# 1
story.append(section("1. 실무에서 사용하는 기본 원칙"))
story.append(
    p(
        "실무에서는 모든 요소를 Tilemap으로 만들거나 모든 요소를 개별 GameObject로 배치하지 않는다. "
        "반복 빈도, 상호작용 여부, 충돌 형태, 애니메이션, 정렬 요구사항을 기준으로 표현 방식을 나눈다."
    )
)
story.append(
    data_table(
        ["콘텐츠 유형", "권장 방식", "판단 기준"],
        [
            ["잔디, 흙길, 물, 절벽", "Tilemap", "넓은 면적에 반복되고 셀 단위 편집이 적합함"],
            ["단순 풀, 작은 돌, 바닥 얼룩", "Detail Tilemap", "충돌과 상호작용이 없고 대량 배치됨"],
            ["나무, 건물, 우물", "Prefab GameObject", "Y축 정렬, 충돌, 트리거 또는 여러 컴포넌트가 필요함"],
            ["Cart, 상자, 모닥불", "Interactive Prefab", "이동, 열기, 파티클, 저장 상태 등이 필요함"],
            ["맵 외곽의 장식 나무", "StaticDecor Tilemap 또는 Prefab Brush", "대량 배치와 성능이 우선임"],
            ["스폰, 포털, 카메라 경계", "Gameplay 데이터 오브젝트", "화면에는 보이지 않지만 게임 로직에서 사용함"],
        ],
        [38 * mm, 42 * mm, 82 * mm],
    )
)
story.append(Spacer(1, 5 * mm))
story.append(
    callout(
        "분류 기준",
        "시각적 종류보다 행동 기준으로 분리한다. 같은 나무라도 배경 장식용은 Tilemap, 채집 가능한 나무는 Prefab이 될 수 있다.",
    )
)

story.append(section("2. 권장 Scene 계층"))
hierarchy = """MapRoot
  Grid
    Terrain
    GroundDetails
    Water
    StaticDecor
    Collision
    Navigation
  WorldObjects
    Structures
    Trees
    StaticProps
    InteractiveProps
    Foreground
  Gameplay
    PlayerSpawn
    EnemySpawns
    ItemSpawns
    Portals
    CameraBounds"""
hierarchy_box = Table(
    [[Paragraph(hierarchy.replace("\n", "<br/>").replace("  ", "&nbsp;&nbsp;"), styles["CodeKo"])]],
    colWidths=[PAGE_W - 2 * MARGIN_X],
)
hierarchy_box.setStyle(
    TableStyle(
        [
            ("BACKGROUND", (0, 0), (-1, -1), colors.HexColor("#233A4A")),
            ("BOX", (0, 0), (-1, -1), 0.7, NAVY),
            ("LEFTPADDING", (0, 0), (-1, -1), 7 * mm),
            ("RIGHTPADDING", (0, 0), (-1, -1), 7 * mm),
            ("TOPPADDING", (0, 0), (-1, -1), 5 * mm),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 5 * mm),
        ]
    )
)
story.append(hierarchy_box)

# 2
story.append(section("3. 현재 Ground 리소스의 Tilemap 설계"))
story.append(
    p(
        "현재 Resources/PNG의 Ground 01~56은 모두 256 x 256 이미지이며, 잔디와 도로 경계가 한 장에 합성된 형태다. "
        "도로만 투명하게 분리된 오버레이가 아니므로 Grass Tilemap 위에 Road Tilemap을 겹치는 방식보다 "
        "하나의 Terrain Tilemap에서 셀을 교체하는 방식이 자연스럽다."
    )
)
story.append(subsection("Terrain Tile 구성 예시"))
for item in [
    "기본 잔디 타일",
    "흙길 중앙 타일",
    "가로 및 세로 직선 타일",
    "안쪽 및 바깥쪽 모서리 타일",
    "T자 교차로와 십자 교차로",
    "막다른 길과 넓은 광장 변형",
]:
    story.append(bullet(item))

story.append(
    callout(
        "현재 에셋에 적합한 방식",
        "Terrain Tilemap 한 셀에는 잔디 또는 도로 경계가 포함된 Ground 타일 한 장만 배치한다. "
        "나중에 도로만 투명한 스프라이트를 제작할 경우 Ground와 Road를 별도 Tilemap으로 분리할 수 있다.",
        color=PALE_GREEN,
        accent=GREEN,
    )
)

story.append(section("4. Grid 크기와 Pixels Per Unit"))
story.append(
    p(
        "Ground 이미지가 256픽셀이므로 PPU와 Grid Cell Size를 함께 결정해야 한다. "
        "프로젝트 중간에 이 규칙을 바꾸면 Collider, 카메라 크기, 이동 속도와 Prefab 스케일이 모두 영향을 받는다."
    )
)
story.append(
    data_table(
        ["선택안", "월드 크기", "장점", "주의점"],
        [
            ["PPU 100 / Cell 2.56", "타일 1장 = 2.56 유닛", "현재 목업 비율과 일반적인 Unity 단위를 유지", "셀 좌표가 소수이므로 수동 계산이 번거로울 수 있음"],
            ["PPU 256 / Cell 1", "타일 1장 = 1 유닛", "맵 좌표와 알고리즘 구현이 단순함", "모든 Prop의 PPU와 카메라 비율을 다시 통일해야 함"],
        ],
        [38 * mm, 38 * mm, 48 * mm, 48 * mm],
    )
)
story.append(Spacer(1, 4 * mm))
story.append(
    callout(
        "권장 결정",
        "아직 게임 로직이 거의 없는 초기 프로젝트이므로 PPU 256 / Cell Size 1도 선택할 수 있다. "
        "다만 현재 에셋 전체를 같은 기준으로 다시 가져와야 한다. 빠르게 진행하려면 PPU 100 / Cell Size 2.56을 유지하는 편이 안전하다.",
        color=PALE_GOLD,
        accent=GOLD,
    )
)

tree_table = data_table(
    ["용도", "권장 방식"],
    [
        ["맵 외곽을 채우는 배경 나무", "StaticDecor Tilemap"],
        ["충돌만 있는 고정 나무", "Object Tilemap 또는 간단한 Prefab"],
        ["플레이어가 앞뒤로 지나가는 나무", "하단 Pivot을 가진 Prefab GameObject"],
        ["채집, 파괴, 애니메이션이 있는 나무", "상태와 스크립트를 포함한 Prefab GameObject"],
        ["수백 개의 단순 수풀", "Detail Tilemap"],
    ],
    [72 * mm, 100 * mm],
)
story.append(KeepTogether([section("5. 나무와 자연물"), tree_table]))

# 3
story.append(section("6. 구조물과 일반 오브젝트"))
story.append(subsection("행동 기준 그룹"))
story.append(
    data_table(
        ["그룹", "예시", "구현 형태"],
        [
            ["Structures", "House, Windmill, Castle", "Collider, 입구 Trigger, SortingGroup을 포함한 Prefab"],
            ["Trees", "Large, Medium, Small Tree", "하단 Pivot, 줄기 Collider, 선택적 상호작용"],
            ["StaticProps", "Rock, Bush, Stump, Fence", "Tilemap 또는 간단한 Prefab"],
            ["InteractiveProps", "TreasureChest, Cart, Campfire, Well", "상태, 애니메이션, 스크립트를 포함한 Prefab"],
            ["Foreground", "전경 수풀, 수관, 화면 가장자리 장식", "캐릭터보다 높은 Sorting Layer"],
        ],
        [35 * mm, 53 * mm, 84 * mm],
    )
)
story.append(Spacer(1, 4 * mm))
story.append(
    p(
        "건물은 하나의 큰 SpriteRenderer만 사용하는 경우에도 Prefab으로 관리하는 편이 좋다. "
        "입구 위치, Collider, 그림자, 문 애니메이션과 내부 이동 Trigger를 한 단위로 재사용할 수 있기 때문이다."
    )
)

story.append(section("7. 탑다운 정렬 구조"))
story.append(
    p(
        "탑다운 화면에서는 캐릭터가 나무나 건물의 앞과 뒤를 자연스럽게 오갈 수 있어야 한다. "
        "Ground와 WorldObject의 렌더링 정책을 분리하면 정렬 정확도와 성능을 함께 확보할 수 있다."
    )
)
story.append(
    data_table(
        ["Sorting Layer", "대상", "권장 렌더링"],
        [
            ["Background", "원거리 배경", "Chunk 또는 일반 Sprite"],
            ["Ground", "Terrain, Road, Water", "TilemapRenderer Chunk"],
            ["GroundDetail", "꽃, 잔디 장식, 작은 돌", "TilemapRenderer Chunk"],
            ["World", "캐릭터, 건물, 나무, 상호작용 Prop", "하단 Pivot 기반 Y축 정렬"],
            ["Foreground", "앞쪽 수관과 화면 가림 장식", "World보다 높은 정렬"],
            ["Effects / UI", "파티클, 월드 UI, 화면 UI", "용도별 별도 Layer"],
        ],
        [36 * mm, 66 * mm, 70 * mm],
    )
)
story.append(Spacer(1, 4 * mm))
story.append(
    callout(
        "TilemapRenderer 모드",
        "Ground와 GroundDetail은 Chunk 모드가 일반적이다. Tilemap 타일이 캐릭터와 Y축으로 섞여야 하는 경우에만 Individual 모드를 제한적으로 사용한다. "
        "여러 SpriteRenderer로 구성된 Prefab에는 SortingGroup을 사용한다.",
    )
)
sorting_checklist = [subsection("Pivot과 Collider")]
for item in [
    "나무와 건물 Sprite의 Pivot은 하단 중앙에 둔다.",
    "나무 Collider는 수관 전체가 아니라 줄기 아래쪽에만 둔다.",
    "캐릭터의 정렬 기준도 발 위치가 되도록 Pivot 또는 별도 Sorting Anchor를 사용한다.",
    "복합 구조물은 SortingGroup으로 내부 Sprite의 순서를 유지한다.",
]:
    sorting_checklist.append(bullet(item))
story.append(KeepTogether(sorting_checklist))

# 4
story.append(section("8. 충돌과 게임 데이터 레이어"))
story.append(
    p(
        "보이는 Terrain과 충돌 데이터를 분리하면 아트 교체가 게임플레이에 영향을 주지 않는다. "
        "Collision Tilemap은 개발 중에만 색상으로 표시하고 실제 플레이에서는 Renderer를 비활성화한다."
    )
)
collision_box = Table(
    [
        [Paragraph("Collision Tilemap", styles["SmallBoldKo"]), Paragraph("TilemapCollider2D", styles["SmallKo"])],
        [Paragraph("Static Rigidbody2D", styles["SmallBoldKo"]), Paragraph("물리적으로 움직이지 않는 월드 충돌", styles["SmallKo"])],
        [Paragraph("CompositeCollider2D", styles["SmallBoldKo"]), Paragraph("인접 셀의 Collider를 합쳐 경계 수와 비용 감소", styles["SmallKo"])],
        [Paragraph("Prefab Collider", styles["SmallBoldKo"]), Paragraph("건물, 나무, 상호작용 오브젝트에 개별 적용", styles["SmallKo"])],
    ],
    colWidths=[55 * mm, 117 * mm],
)
collision_box.setStyle(
    TableStyle(
        [
            ("BACKGROUND", (0, 0), (0, -1), SKY),
            ("GRID", (0, 0), (-1, -1), 0.5, LINE),
            ("VALIGN", (0, 0), (-1, -1), "TOP"),
            ("LEFTPADDING", (0, 0), (-1, -1), 4 * mm),
            ("RIGHTPADDING", (0, 0), (-1, -1), 4 * mm),
            ("TOPPADDING", (0, 0), (-1, -1), 3 * mm),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 3 * mm),
        ]
    )
)
story.append(collision_box)
story.append(Spacer(1, 5 * mm))
story.append(subsection("Gameplay 레이어 예시"))
for item in [
    "PlayerSpawn과 EnemySpawn",
    "아이템 및 상자 스폰 지점",
    "문, 포털, 지역 이동 Trigger",
    "카메라 이동 경계",
    "NavMesh 또는 커스텀 이동 가능 셀 데이터",
]:
    story.append(bullet(item))

story.append(section("9. SimpleGame 적용 순서"))
steps = [
    ("1", "가져오기 규칙 고정", "PPU, Filter Mode, Compression, Pivot 규칙을 먼저 확정한다."),
    ("2", "Tile Asset 생성", "Ground 01~56을 Terrain용 Tile Asset으로 변환하고 Palette를 만든다."),
    ("3", "Terrain Tilemap 구축", "잔디와 도로를 하나의 Terrain Tilemap에서 셀 교체 방식으로 구성한다."),
    ("4", "보조 Tilemap 추가", "GroundDetails, StaticDecor, Collision, Navigation 레이어를 생성한다."),
    ("5", "Prefab 제작", "건물, 나무, Cart, 보물상자, 우물, 모닥불을 용도별 Prefab으로 만든다."),
    ("6", "정렬 구성", "Sorting Layer, 하단 Pivot, SortingGroup 및 Y축 정렬 규칙을 적용한다."),
    ("7", "목업 재배치", "현재 SummerVillageMap의 구도를 새 구조에 맞춰 옮긴다."),
    ("8", "검증", "경계 이음새, 충돌, 캐릭터 앞뒤 정렬, 카메라 프레이밍을 확인한다."),
]
step_rows = []
for number, title, detail in steps:
    step_rows.append(
        [
            Paragraph(number, styles["SmallBoldWhiteKo"]),
            Paragraph(title, styles["SmallBoldKo"]),
            Paragraph(detail, styles["SmallKo"]),
        ]
    )
steps_table = Table(step_rows, colWidths=[12 * mm, 40 * mm, 120 * mm])
steps_table.setStyle(
    TableStyle(
        [
            ("BACKGROUND", (0, 0), (0, -1), NAVY),
            ("TEXTCOLOR", (0, 0), (0, -1), WHITE),
            ("ALIGN", (0, 0), (0, -1), "CENTER"),
            ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
            ("ROWBACKGROUNDS", (1, 0), (-1, -1), [WHITE, colors.HexColor("#F7FAFC")]),
            ("GRID", (0, 0), (-1, -1), 0.5, LINE),
            ("LEFTPADDING", (0, 0), (-1, -1), 3 * mm),
            ("RIGHTPADDING", (0, 0), (-1, -1), 3 * mm),
            ("TOPPADDING", (0, 0), (-1, -1), 2.5 * mm),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 2.5 * mm),
        ]
    )
)
story.append(steps_table)

doc.build(story)
print(OUTPUT)
