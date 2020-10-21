using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ValueAnimator))]
[CanEditMultipleObjects]
public class ValueAnimatorEditor : Editor {
    private ValueAnimator script;
    private Material mat;
    private Texture2D handleColor;

    private readonly Vector2 easingCurveRectStartOffset = new Vector2(70f, 44f);
    private static float easingSize = 120f;
    private readonly Vector2 easingSizeVec = new Vector2(easingSize, easingSize);

    private bool dragStartHandle;
    private Vector2 dragMouseStartPosHandle;
    private Vector3 dragMouseStartPos;

    private bool dragEndHandle;
    private Vector2 dragMouseEndPosHandle;
    private Vector3 dragMouseEndPos;
    private readonly Color handle1Color = new Color(0.1921569f, 0.4745098f, 0.401132f);
    private readonly Color handle2Color = new Color(1f, 0f, 0.1198916f);
    private const float HANDLE_RECT_SIZE = 10f;

    void OnEnable() {
        script = (ValueAnimator) target;
        var shader = Shader.Find("Hidden/Internal-Colored");
        mat = new Material(shader);
        handleColor = Resources.Load<Texture2D>("handles");
    }

    private void OnDisable() {
        DestroyImmediate(mat);
        DestroyImmediate(handleColor);
    }

    public override void OnInspectorGUI() {
        BezierPathEasingCurve easingCurve = script.easingCurve;
        var easingCurveHandle1 = new Vector2(
            easingCurveRectStartOffset.x + easingCurve.handle1.x * easingSize,
            easingCurveRectStartOffset.y + (easingSize - easingCurve.handle1.y * easingSize)
        );
        var easingCurveHandle2 = new Vector2(
            easingCurveRectStartOffset.x + easingCurve.handle2.x * easingSize,
            easingCurveRectStartOffset.y + (easingSize - easingCurve.handle2.y * easingSize)
        );
        var startHandleRect = new Rect(easingCurveHandle1.x - HANDLE_RECT_SIZE / 2f, easingCurveHandle1.y - HANDLE_RECT_SIZE / 2f,
            HANDLE_RECT_SIZE, HANDLE_RECT_SIZE);
        var endHandleRect = new Rect(easingCurveHandle2.x - HANDLE_RECT_SIZE / 2f, easingCurveHandle2.y - HANDLE_RECT_SIZE / 2f,
            HANDLE_RECT_SIZE, HANDLE_RECT_SIZE);

        moveHandles(startHandleRect, easingCurve, endHandleRect);
        easingCurveHeading(easingCurve);
        spaceBox();
        drawEasingBackgroundRect();

        if (Event.current.type == EventType.Repaint) {
            drawHandle(new Vector3(easingCurveRectStartOffset.x, easingCurveRectStartOffset.y + easingSize, 0f), easingCurveHandle1,
                startHandleRect, handle1Color);
            drawHandle(new Vector3(easingCurveRectStartOffset.x + easingSize, easingCurveRectStartOffset.y, 0f), easingCurveHandle2,
                endHandleRect, handle2Color);
            drawLine(easingCurveHandle1, easingCurveHandle2);
        }

        addSerializedFields();
    }

    private static void easingCurveHeading(BezierPathEasingCurve easingCurve) {
        EditorGUILayout.BeginVertical();
        var style = new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleLeft};
        EditorGUILayout.LabelField("");
        style.padding = new RectOffset(0, 0, 0, 5);
        EditorGUILayout.LabelField("Easing curve: " + easingCurve.handle1 + " - " + easingCurve.handle2, style);
        EditorGUILayout.EndVertical();
    }

    private void addSerializedFields() {
        serializedObject.Update();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.Separator();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("animationType"));
        EditorGUILayout.Separator();

        if (script.animationType == AnimationType.FLOAT_VALUE) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("from"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("to"));
        }
        else {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fromVec"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("toVec"));
        }

        EditorGUILayout.Separator();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("delay"));
        EditorGUILayout.Separator();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoStart"));
        EditorGUILayout.Separator();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("repeatMode"));
        EditorGUILayout.EndVertical();
        serializedObject.ApplyModifiedProperties();
    }

    private void drawHandle(Vector3 from, Vector2 easingCurveHandle2, Rect endHandle, Color color) {
        Handles.DrawBezier(
            startPosition: @from,
            endPosition: easingCurveHandle2,
            startTangent: @from,
            endTangent: easingCurveHandle2,
            color: new Color(1f, 1f, 1f, 0.3f),
            handleColor, 2f
        );
        EditorGUI.DrawRect(endHandle, color);
    }

    private void moveHandles(Rect startHandle, BezierPathEasingCurve easingCurve, Rect endHandle) {
        if (Event.current.isMouse) {
            if (startHandle.Contains(Event.current.mousePosition) && !dragStartHandle) {
                dragStartHandle = true;
                dragMouseStartPosHandle = Event.current.mousePosition;
                dragMouseStartPos = easingCurve.handle1;
            }

            if (endHandle.Contains(Event.current.mousePosition) && !dragEndHandle) {
                dragEndHandle = true;
                dragMouseEndPosHandle = Event.current.mousePosition;
                dragMouseEndPos = easingCurve.handle2;
            }

            if (dragStartHandle) {
                var offset = Event.current.mousePosition - dragMouseStartPosHandle;
                var inPercent = offset / easingSizeVec;
                easingCurve.handle1 = dragMouseStartPos + new Vector3(inPercent.x, -inPercent.y, 0f);
                easingCurve.recreate();
            }
            else if (dragEndHandle) {
                var offset = Event.current.mousePosition - dragMouseEndPosHandle;
                var inPercent = offset / easingSizeVec;
                easingCurve.handle2 = dragMouseEndPos + new Vector3(inPercent.x, -inPercent.y, 0f);
                easingCurve.recreate();
            }
        }

        if (Event.current.type == EventType.MouseUp) {
            dragEndHandle = false;
            dragStartHandle = false;
        }
    }

    private void drawEasingBackgroundRect() {
        EditorGUI.DrawRect(new Rect(easingCurveRectStartOffset.x - 2, easingCurveRectStartOffset.y - 2, easingSize + 4, easingSize + 4),
            new Color(0.1f, 0.1f, 0.1f, 1f));
    }

    private void spaceBox() {
        var s = EditorStyles.helpBox;
        s.fixedWidth = easingSize;
        s.fixedHeight = easingSize;
        s.margin = new RectOffset((int) easingCurveRectStartOffset.x, 0, 0, 0);
        s.stretchWidth = false;
        GUILayout.BeginVertical(s);
        EditorGUILayout.LabelField(" ");
        GUILayout.EndVertical();
    }

    private void drawLine(Vector2 easingCurveHandle1, Vector2 easingCurveHandle2) {
        var from = new Vector3(easingCurveRectStartOffset.x, easingCurveRectStartOffset.y + easingSize, 0f);
        var to = new Vector3(easingCurveRectStartOffset.x + easingSize, easingCurveRectStartOffset.y, 0f);
        Handles.DrawBezier(
            startPosition: @from,
            endPosition: to,
            startTangent: easingCurveHandle1,
            endTangent: easingCurveHandle2,
            color: new Color(1f, 1f, 1f, 1f),
            handleColor, 3f
        );
    }
}