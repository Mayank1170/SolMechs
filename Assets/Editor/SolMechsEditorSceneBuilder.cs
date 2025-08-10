// Assets/Editor/SolMechsEditorSceneBuilder.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MechBattle;

public static class SolMechsEditorSceneBuilder
{
    [MenuItem("SolMechs/Create Mech Editor Scene")]
    public static void CreateScene()
    {
        // fresh scene
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
            UnityEditor.SceneManagement.NewSceneMode.Single);

        // EventSystem
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // Canvas
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Root panel (background)
        var root = CreatePanel("Root", canvasGO.transform, new Color(0.06f, 0.09f, 0.12f, 1f));
        SetRect(root, anchorMin: new Vector2(0,0), anchorMax: new Vector2(1,1), offsetMin: new Vector2(32,32), offsetMax: new Vector2(-32,-32));

        // ----- Columns -----
        var leftCol  = CreatePanel("ColumnLeft",  root.transform, new Color(0,0,0,0));   // transparent
        var midCol   = CreatePanel("ColumnCenter",root.transform, new Color(0,0,0,0));
        var rightCol = CreatePanel("ColumnRight", root.transform, new Color(0,0,0,0));

        // 3 columns layout (manual positions)
        SetRect(leftCol,  new Vector2(0f, 0.05f),  new Vector2(0.38f, 0.95f),  new Vector2(24,24), new Vector2(-12,-24));
        SetRect(midCol,   new Vector2(0.38f,0.05f),new Vector2(0.62f, 0.95f),  new Vector2(12,24), new Vector2(-12,-24));
        SetRect(rightCol, new Vector2(0.62f,0.05f),new Vector2(1.00f, 0.95f),  new Vector2(12,24), new Vector2(-24,-24));

        // ====== ColumnLeft: Paper‑doll ======
        var dollFrame = CreatePanel("PaperDollFrame", leftCol.transform, new Color(0.08f,0.12f,0.18f,1f));
        SetRect(dollFrame, new Vector2(0,0), new Vector2(1,1), new Vector2(16,16), new Vector2(-16,-16));

        // stack of images
        var imgLower    = CreateImage("imgLower", dollFrame.transform);
        var imgMatrix   = CreateImage("imgMatrix", dollFrame.transform);
        var imgLeftArm  = CreateImage("imgLeftArm", dollFrame.transform);
        var imgRightArm = CreateImage("imgRightArm", dollFrame.transform);
        foreach (var img in new[] {imgLower, imgMatrix, imgLeftArm, imgRightArm})
        {
            var r = img.rectTransform;
            r.anchorMin = new Vector2(0.5f, 0.5f);
            r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(600, 600);
            r.anchoredPosition = Vector2.zero;
            img.preserveAspect = true;
        }

        // ====== ColumnCenter: Selection + Save ======
        var selContainer = CreatePanel("SelectionPanel", midCol.transform, new Color(0.08f,0.12f,0.18f,1f));
        SetRect(selContainer, new Vector2(0,0), new Vector2(1,1), new Vector2(16,16), new Vector2(-16,-16));
        var vSel = selContainer.AddComponent<VerticalLayoutGroup>();
        vSel.childControlHeight = true; vSel.childForceExpandHeight = false; vSel.spacing = 12; vSel.padding = new RectOffset(16,16,16,16);

        var ddMatrix = CreateDropdown("MatrixDropdown", selContainer.transform, "Matrix");
        var ddRA     = CreateDropdown("RightArmDropdown", selContainer.transform, "Right Arm");
        var ddLA     = CreateDropdown("LeftArmDropdown", selContainer.transform, "Left Arm");
        var ddIN     = CreateDropdown("LowerDropdown", selContainer.transform, "Lower Body");
        var saveBtn  = CreateButton("SaveButton", selContainer.transform, "Save Build");

        // ====== ColumnRight: Stats + Move ======
        var rightWrap = CreatePanel("RightWrap", rightCol.transform, new Color(0,0,0,0));
        SetRect(rightWrap, new Vector2(0,0), new Vector2(1,1), new Vector2(0,0), new Vector2(0,0));

        var statsPanel = CreatePanel("StatsPanel", rightWrap.transform, new Color(0.08f,0.12f,0.18f,1f));
        var movesPanel = CreatePanel("MovePanel",  rightWrap.transform, new Color(0.08f,0.12f,0.18f,1f));
        SetRect(statsPanel, new Vector2(0,0.5f), new Vector2(1,1), new Vector2(16,-8), new Vector2(-16,-16));
        SetRect(movesPanel, new Vector2(0,0),    new Vector2(1,0.5f), new Vector2(16,16), new Vector2(-16,8));

        // Stats content
        var vStats = statsPanel.AddComponent<VerticalLayoutGroup>();
        vStats.spacing = 8; vStats.padding = new RectOffset(16,16,16,16);
        var hp  = CreateStatRow(statsPanel.transform, "HP");
        var atk = CreateStatRow(statsPanel.transform, "ATK");
        var def = CreateStatRow(statsPanel.transform, "DEF");
        var eng = CreateStatRow(statsPanel.transform, "ENG");
        var spd = CreateStatRow(statsPanel.transform, "SPD");
        var sys = CreateStatRow(statsPanel.transform, "SYS");

        // Move detail + list
        var vMoves = movesPanel.AddComponent<VerticalLayoutGroup>();
        vMoves.spacing = 6; vMoves.padding = new RectOffset(16,16,16,16);

        var moveName    = CreateLabel(movesPanel.transform, "moveNameText",   "MOVE NAME");
        var movePart    = CreateLabel(movesPanel.transform, "movePartText",   "PART NAME");
        var moveDamage  = CreateLabel(movesPanel.transform, "moveDamageText", "DAMAGE: 0");
        var moveType    = CreateLabel(movesPanel.transform, "moveTypeText",   "TYPE: -");
        var moveTarget  = CreateLabel(movesPanel.transform, "moveTargetText", "TARGET: -");
        var moveDesc    = CreateLabel(movesPanel.transform, "moveDescText",   "Effect/Description", 12);

        var moveListRow = new GameObject("MoveList", typeof(HorizontalLayoutGroup));
        moveListRow.transform.SetParent(movesPanel.transform, false);
        var h = moveListRow.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 8; h.childForceExpandWidth = true;

        var moveBtn1 = CreateButton("move1Button", moveListRow.transform, "—");
        var moveBtn2 = CreateButton("move2Button", moveListRow.transform, "—");
        var moveBtn3 = CreateButton("move3Button", moveListRow.transform, "—");

        // ----- EditorController hookup -----
        var editorGO = new GameObject("Editor", typeof(EditorController));
        editorGO.transform.SetParent(canvasGO.transform, false);
        var ctrl = editorGO.GetComponent<EditorController>();

        // Assign Selection UI
        ctrl.matrixDropdown   = ddMatrix;
        ctrl.rightArmDropdown = ddRA;
        ctrl.leftArmDropdown  = ddLA;
        ctrl.lowerDropdown    = ddIN;
        ctrl.saveButton       = saveBtn;

        // Paper doll
        ctrl.imgMatrix   = imgMatrix;
        ctrl.imgRightArm = imgRightArm;
        ctrl.imgLeftArm  = imgLeftArm;
        ctrl.imgLower    = imgLower;

        // Stats
        ctrl.hpBar  = hp;
        ctrl.atkBar = atk;
        ctrl.defBar = def;
        ctrl.engBar = eng;
        ctrl.spdBar = spd;
        ctrl.sysBar = sys;

        // Move detail
        ctrl.moveNameText   = moveName;
        ctrl.movePartText   = movePart;
        ctrl.moveDamageText = moveDamage;
        ctrl.moveTypeText   = moveType;
        ctrl.moveTargetText = moveTarget;
        ctrl.moveDescText   = moveDesc;

        // Move list buttons
        ctrl.move1Button = moveBtn1;
        ctrl.move2Button = moveBtn2;
        ctrl.move3Button = moveBtn3;

        // Try to auto-assign catalogs from Resources (optional)
        ctrl.matrixCatalog = FindAssetInProject<MatrixCatalog>();
        ctrl.partCatalog   = FindAssetInProject<PartCatalog>();

        Debug.Log("SolMechs Mech Editor scene created. Assign MatrixCatalog/PartCatalog if they’re not auto‑linked, then Play.");
    }

    // ---------- helpers ----------
    static GameObject CreatePanel(string name, Transform parent, Color bg)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.color = bg;
        return go;
    }

    static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
    }

    static Image CreateImage(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = Color.white;
        return img;
    }

    static Dropdown CreateDropdown(string name, Transform parent, string label)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        var layout = root.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4;

        var lblGO = new GameObject("Label", typeof(Text));
        lblGO.transform.SetParent(root.transform, false);
        var lbl = lblGO.GetComponent<Text>();
        lbl.text = label; lbl.color = Color.white; lbl.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        lbl.alignment = TextAnchor.MiddleLeft;

        var ddGO = new GameObject("Dropdown", typeof(Image), typeof(Dropdown));
        ddGO.transform.SetParent(root.transform, false);
        var img = ddGO.GetComponent<Image>(); img.color = new Color(0.18f,0.22f,0.28f,1f);
        var dd = ddGO.GetComponent<Dropdown>();
        dd.targetGraphic = img;

        // Add child Text for caption (built-in dropdown expects)
        var caption = new GameObject("Label", typeof(Text));
        caption.transform.SetParent(ddGO.transform, false);
        var cTxt = caption.GetComponent<Text>();
        cTxt.text = "—"; cTxt.color = Color.white; cTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        cTxt.alignment = TextAnchor.MiddleLeft;
        var cRect = cTxt.GetComponent<RectTransform>(); cRect.anchorMin = Vector2.zero; cRect.anchorMax = Vector2.one; cRect.offsetMin = new Vector2(10,0); cRect.offsetMax = new Vector2(-30,0);

        var arrow = new GameObject("Arrow", typeof(Text));
        arrow.transform.SetParent(ddGO.transform, false);
        var aTxt = arrow.GetComponent<Text>();
        aTxt.text = "▼"; aTxt.color = Color.white; aTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        aTxt.alignment = TextAnchor.MiddleRight;
        var aRect = aTxt.GetComponent<RectTransform>(); aRect.anchorMin = Vector2.zero; aRect.anchorMax = Vector2.one; aRect.offsetMin = new Vector2(0,0); aRect.offsetMax = new Vector2(-10,0);

        dd.captionText = cTxt;

        // Template (required by Dropdown)
        var template = new GameObject("Template", typeof(Image), typeof(ScrollRect));
        template.transform.SetParent(ddGO.transform, false);
        var tImg = template.GetComponent<Image>(); tImg.color = new Color(0.12f,0.16f,0.2f,1);
        var tRect = template.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0,1); tRect.anchorMax = new Vector2(1,1); tRect.pivot = new Vector2(0.5f,1);
        tRect.sizeDelta = new Vector2(0, 120);
        template.SetActive(false);

        var viewport = new GameObject("Viewport", typeof(Mask), typeof(Image));
        viewport.transform.SetParent(template.transform, false);
        var vpImg = viewport.GetComponent<Image>(); vpImg.color = new Color(0,0,0,0.2f); vpImg.raycastTarget = true;
        viewport.GetComponent<Mask>().showMaskGraphic = false;
        var vpRect = viewport.GetComponent<RectTransform>(); vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one; vpRect.offsetMin = Vector2.zero; vpRect.offsetMax = Vector2.zero;

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var ctRect = content.GetComponent<RectTransform>(); ctRect.anchorMin = new Vector2(0,1); ctRect.anchorMax = new Vector2(1,1); ctRect.pivot = new Vector2(0.5f,1); ctRect.offsetMin = new Vector2(0,0); ctRect.offsetMax = new Vector2(0,0);

        var item = new GameObject("Item", typeof(Toggle));
        item.transform.SetParent(content.transform, false);
        var itemToggle = item.GetComponent<Toggle>();
        var itemBG = new GameObject("Item Background", typeof(Image));
        itemBG.transform.SetParent(item.transform, false);
        var itemCheck = new GameObject("Item Checkmark", typeof(Image));
        itemCheck.transform.SetParent(itemBG.transform, false);
        var itemLabel = new GameObject("Item Label", typeof(Text));
        itemLabel.transform.SetParent(item.transform, false);
        var itemText = itemLabel.GetComponent<Text>();
        itemText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        itemText.color = Color.white;
        itemText.text = "Option";

        // hook dropdown template hierarchy
        dd.template = template.GetComponent<RectTransform>();
        dd.itemText = itemText;
        dd.options.Clear();
        dd.options.Add(new Dropdown.OptionData("—"));
        dd.RefreshShownValue();

        var rect = ddGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 36);

        return dd;
    }

    static Button CreateButton(string name, Transform parent, string text)
    {
        var go = new GameObject(name, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.color = new Color(0.18f,0.22f,0.28f,1f);
        var btn = go.GetComponent<Button>();

        var t = new GameObject("Text", typeof(Text));
        t.transform.SetParent(go.transform, false);
        var txt = t.GetComponent<Text>();
        txt.text = text; txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.color = Color.white;
        var r = txt.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 40);

        return btn;
    }

    static Text CreateLabel(Transform parent, string name, string text, int size = 14)
    {
        var go = new GameObject(name, typeof(Text));
        go.transform.SetParent(parent, false);
        var txt = go.GetComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.color = Color.white;
        txt.fontSize = size;
        txt.alignment = TextAnchor.MiddleLeft;
        return txt;
    }

    static Slider CreateStatRow(Transform parent, string label)
    {
        var row = new GameObject(label + "_Row");
        row.transform.SetParent(parent, false);
        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 8; h.childAlignment = TextAnchor.MiddleLeft; h.childForceExpandWidth = true;

        var l = CreateLabel(row.transform, label + "_Label", label);
        var sliderGO = new GameObject(label + "_Slider", typeof(Slider));
        sliderGO.transform.SetParent(row.transform, false);
        var s = sliderGO.GetComponent<Slider>(); s.interactable = false;
        // basic slider visuals
        var bg = new GameObject("Background", typeof(Image));
        bg.transform.SetParent(sliderGO.transform, false);
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGO.transform, false);
        var fill = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);

        var rt = sliderGO.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(0, 18);
        var bgImg = bg.GetComponent<Image>(); bgImg.color = new Color(0,0,0,0.35f);
        var fillImg = fill.GetComponent<Image>(); fillImg.color = new Color(0.2f,0.85f,0.7f,1f);

        var fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0.25f);
        fillAreaRT.anchorMax = new Vector2(1, 0.75f);
        fillAreaRT.offsetMin = new Vector2(5, 0);
        fillAreaRT.offsetMax = new Vector2(-5, 0);

        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.25f);
        bgRT.anchorMax = new Vector2(1, 0.75f);
        bgRT.offsetMin = new Vector2(0, 0);
        bgRT.offsetMax = new Vector2(0, 0);

        s.targetGraphic = bgImg;
        s.fillRect = fillImg.rectTransform;
        s.handleRect = null;
        s.maxValue = 100; s.value = 50;
        return s;
    }

    static T FindAssetInProject<T>() where T : Object
    {
        var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        if (guids.Length == 0) return null;
        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
#endif
