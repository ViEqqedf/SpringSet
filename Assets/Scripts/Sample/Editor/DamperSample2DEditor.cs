using UnityEditor;

// DamperSample2D 的自定义 Inspector：根据 Func 只显示对应阻尼函数的强度参数
[CustomEditor(typeof(DamperSample2D))]
public class DamperSample2DEditor : Editor
{
    private SerializedProperty _scope;
    private SerializedProperty _func;
    private SerializedProperty _factor;
    private SerializedProperty _damping;
    private SerializedProperty _ft;
    private SerializedProperty _halfLife;
    private SerializedProperty _eps;
    private SerializedProperty _interval;
    private SerializedProperty _current;
    private SerializedProperty _target;

    private void OnEnable()
    {
        _scope = serializedObject.FindProperty("Scope");
        _func = serializedObject.FindProperty("Func");
        _factor = serializedObject.FindProperty("Factor");
        _damping = serializedObject.FindProperty("Damping");
        _ft = serializedObject.FindProperty("Ft");
        _halfLife = serializedObject.FindProperty("HalfLife");
        _eps = serializedObject.FindProperty("Eps");
        _interval = serializedObject.FindProperty("Interval");
        _current = serializedObject.FindProperty("Current");
        _target = serializedObject.FindProperty("Target");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_scope);
        EditorGUILayout.PropertyField(_func);

        // 只显示当前选中阻尼函数对应的强度参数
        EDamperFunc func = (EDamperFunc)_func.enumValueIndex;
        switch (func)
        {
            case EDamperFunc.Exponential:
                EditorGUILayout.PropertyField(_damping);
                EditorGUILayout.PropertyField(_ft);
                break;
            case EDamperFunc.Exact:
                EditorGUILayout.PropertyField(_halfLife);
                EditorGUILayout.PropertyField(_eps);
                break;
            default:
                EditorGUILayout.PropertyField(_factor);
                break;
        }

        EditorGUILayout.PropertyField(_interval);
        EditorGUILayout.PropertyField(_current);
        EditorGUILayout.PropertyField(_target);

        serializedObject.ApplyModifiedProperties();
    }
}
