#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

/// <summary>
/// Editor Tool: สร้าง Game Clear Panel แล้วบันทึกเป็น Prefab ใน Resources/Prefabs/
/// ทำครั้งเดียว — GameManager จะโหลดและสร้างขึ้นมาเองอัตโนมัติในทุก Stage
///
/// วิธีใช้: เมนูบาร์ Unity → Tools → Score System → Save Game Clear Panel as Prefab
/// </summary>
public class CreateGameClearPanel : EditorWindow
{
    [MenuItem("Tools/Score System/Save Game Clear Panel as Prefab")]
    static void Create()
    {
        // ==============================
        // สร้าง Folder ถ้ายังไม่มี
        // ==============================
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

        // ==============================
        // สร้าง Canvas ชั่วคราวเพื่อ Build UI
        // (จะลบออกหลังบันทึก Prefab)
        // ==============================
        GameObject tempCanvas = new GameObject("_TempCanvas");
        Canvas canvas = tempCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tempCanvas.AddComponent<CanvasScaler>();
        tempCanvas.AddComponent<GraphicRaycaster>();

        // ==============================
        // สร้าง UI ทั้งหมด
        // ==============================
        GameObject panelRoot = BuildPanel(tempCanvas.transform);

        // ==============================
        // บันทึกเป็น Prefab ลง Resources
        // ==============================
        string prefabPath = "Assets/Resources/Prefabs/GameClearPanel.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(panelRoot, prefabPath);

        // ==============================
        // ลบ Canvas ชั่วคราวออกจาก Scene
        // ==============================
        DestroyImmediate(tempCanvas);

        AssetDatabase.Refresh();

        if (prefab != null)
        {
            Debug.Log($"[CreateGameClearPanel] ✅ บันทึก Prefab สำเร็จที่: {prefabPath}");
            Debug.Log("[CreateGameClearPanel] 🎮 GameManager จะโหลด Prefab นี้อัตโนมัติในทุก Stage!");
            EditorUtility.DisplayDialog(
                "สำเร็จ! ✅",
                "บันทึก GameClearPanel Prefab แล้วที่:\n" + prefabPath +
                "\n\nGameManager จะโหลดและสร้าง Panel นี้อัตโนมัติในทุก Stage\nไม่ต้องทำซ้ำอีกแล้ว!",
                "OK");
        }
        else
        {
            Debug.LogError("[CreateGameClearPanel] ❌ บันทึก Prefab ไม่สำเร็จ!");
        }
    }

    // ==============================
    // สร้าง Panel UI ทั้งหมด
    // ==============================
    static GameObject BuildPanel(Transform parent)
    {
        // พื้นหลังดำโปร่งแสงเต็มจอ
        GameObject panelRoot = CreateUIObject("GameClearPanel", parent);
        SetStretchFull(panelRoot.GetComponent<RectTransform>());
        Image panelBG = panelRoot.AddComponent<Image>();
        panelBG.color = new Color(0f, 0f, 0f, 0.75f);
        panelRoot.SetActive(false);

        // กล่องเนื้อหากลางหน้าจอ
        GameObject card = CreateUIObject("CardBackground", panelRoot.transform);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot     = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(620f, 560f);
        cardRT.anchoredPosition = Vector2.zero;
        Image cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.08f, 0.08f, 0.12f, 0.97f);
        AddOutline(card, new Color(1f, 0.85f, 0.2f, 0.8f), new Vector2(2f, -2f));

        // หัวข้อ STAGE CLEAR!
        GameObject title = CreateTMPText("TitleText", card.transform,
            "★  STAGE CLEAR!  ★", 52f, new Color(1f, 0.9f, 0.2f), FontStyles.Bold);
        SetAnchoredPos(title, 0f, 220f);
        title.GetComponent<RectTransform>().sizeDelta = new Vector2(580f, 70f);

        CreateDivider(card.transform, 165f);

        // เวลา
        GameObject yourTimeLabel = CreateTMPText("YourTimeLabel", card.transform,
            "YOUR TIME", 18f, new Color(0.7f, 0.7f, 0.7f));
        SetAnchoredPos(yourTimeLabel, -110f, 128f);
        yourTimeLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 30f);

        GameObject clearTimeText = CreateTMPText("ClearTimeText", card.transform,
            "00:00.00", 28f, Color.white, FontStyles.Bold);
        SetAnchoredPos(clearTimeText, -110f, 98f);
        clearTimeText.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 40f);

        GameObject bestTimeLabel = CreateTMPText("BestTimeLabel", card.transform,
            "BEST TIME", 18f, new Color(0.7f, 0.7f, 0.7f));
        SetAnchoredPos(bestTimeLabel, 110f, 128f);
        bestTimeLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 30f);

        GameObject clearBestTimeText = CreateTMPText("ClearBestTimeText", card.transform,
            "--:--.--", 28f, new Color(0.4f, 1f, 0.5f), FontStyles.Bold);
        SetAnchoredPos(clearBestTimeText, 110f, 98f);
        clearBestTimeText.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 40f);

        CreateDivider(card.transform, 65f);

        // ORB SCORE
        GameObject orbScoreText = CreateTMPText("OrbScoreText", card.transform,
            "ORB SCORE:  0", 24f, new Color(0.8f, 0.8f, 0.8f));
        SetAnchoredPos(orbScoreText, 0f, 22f);
        orbScoreText.GetComponent<RectTransform>().sizeDelta = new Vector2(520f, 35f);

        // TIME BONUS
        GameObject timeBonusText = CreateTMPText("TimeBonusText", card.transform,
            "TIME BONUS:  +0", 24f, new Color(0.4f, 0.85f, 1f));
        SetAnchoredPos(timeBonusText, 0f, -20f);
        timeBonusText.GetComponent<RectTransform>().sizeDelta = new Vector2(520f, 35f);

        CreateDivider(card.transform, -60f);

        // FINAL SCORE (ตัวใหญ่)
        GameObject finalScoreText = CreateTMPText("FinalScoreText", card.transform,
            "FINAL SCORE:  0", 38f, new Color(1f, 0.85f, 0.2f), FontStyles.Bold);
        SetAnchoredPos(finalScoreText, 0f, -108f);
        finalScoreText.GetComponent<RectTransform>().sizeDelta = new Vector2(540f, 55f);

        // BEST SCORE
        GameObject bestScoreText = CreateTMPText("BestScoreText", card.transform,
            "BEST SCORE:  0", 20f, new Color(0.6f, 0.6f, 0.6f));
        SetAnchoredPos(bestScoreText, 0f, -155f);
        bestScoreText.GetComponent<RectTransform>().sizeDelta = new Vector2(520f, 30f);

        // ปุ่ม PLAY AGAIN
        CreateButton("RestartButton", card.transform,
            "▶  PLAY AGAIN",
            new Color(0.15f, 0.55f, 0.15f),
            new Vector2(-150f, -230f),
            new Vector2(240f, 55f));

        // ปุ่ม MAIN MENU
        CreateButton("MainMenuButton", card.transform,
            "⌂  MAIN MENU",
            new Color(0.35f, 0.35f, 0.35f),
            new Vector2(150f, -230f),
            new Vector2(240f, 55f));

        return panelRoot;
    }

    // ==============================
    // Helper Methods
    // ==============================
    static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void SetStretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    static GameObject CreateTMPText(string name, Transform parent, string text,
        float fontSize, Color color, FontStyles style = FontStyles.Normal)
    {
        GameObject go = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    static void SetAnchoredPos(GameObject go, float x, float y)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
    }

    static void CreateDivider(Transform parent, float yPos)
    {
        GameObject div = CreateUIObject("Divider", parent);
        RectTransform rt = div.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(540f, 2f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        Image img = div.AddComponent<Image>();
        img.color = new Color(1f, 0.85f, 0.2f, 0.4f);
    }

    static void AddOutline(GameObject go, Color color, Vector2 distance)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor    = color;
        outline.effectDistance = distance;
    }

    static GameObject CreateButton(string name, Transform parent,
        string label, Color bgColor, Vector2 pos, Vector2 size)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        RectTransform rt  = btnObj.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;
        Image img  = btnObj.AddComponent<Image>();
        img.color  = bgColor;
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(bgColor.r + 0.15f, bgColor.g + 0.15f, bgColor.b + 0.15f);
        cb.pressedColor     = new Color(bgColor.r - 0.1f,  bgColor.g - 0.1f,  bgColor.b - 0.1f);
        btn.colors = cb;

        GameObject textObj = CreateUIObject("Text", btnObj.transform);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 22f;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        return btnObj;
    }
}
#endif
