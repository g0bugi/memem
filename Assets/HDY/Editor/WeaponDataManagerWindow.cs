using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 프로젝트 전체의 WeaponData를 표 형태로 한 화면에 모아서 보여주고 편집하는 에디터 창.
/// 아이콘 미리보기, 핵심 스탯을 인라인으로 편집할 수 있고, 여러 무기를 체크한 뒤
/// 비주얼 프리셋(WeaponVisualPreset)을 한번에 적용하는 일괄 편집을 지원한다.
/// 개별 무기의 나머지 세부 필드(사거리/각도/투사체 속도 등)는 "열기" 버튼으로 평소처럼
/// 인스펙터에서 편집하면 된다.
/// </summary>
public class WeaponDataManagerWindow : EditorWindow
{
    private static readonly string[] AttackTypeFilterOptions =
        { "전체", "Melee", "Ranged", "Area", "Homing", "Orbit" };

    private List<WeaponData> weapons = new List<WeaponData>();
    private readonly HashSet<WeaponData> selected = new HashSet<WeaponData>();
    private Vector2 scroll;
    private string searchText = "";
    private int attackTypeFilterIndex = 0;
    
    private readonly HashSet<WeaponData> soundExpanded = new HashSet<WeaponData>();
private WeaponVisualPreset bulkPresetToApply;

    [MenuItem("HDY/무기 데이터 매니저")]
    public static void Open()
    {
        WeaponDataManagerWindow window = GetWindow<WeaponDataManagerWindow>("무기 데이터 매니저");
        window.minSize = new Vector2(1030, 400);
        window.RefreshList();
    }

    private void OnEnable()
    {
        RefreshList();
    }

    private void RefreshList()
    {
        weapons.Clear();
        string[] guids = AssetDatabase.FindAssets("t:WeaponData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponData data = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (data != null) weapons.Add(data);
        }
        weapons = weapons.OrderBy(w => (int)w.attackType).ThenBy(w => w.weaponName).ToList();
        Repaint();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawBulkBar();
        DrawHeaderRow();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (WeaponData data in weapons)
        {
            if (data == null) continue;
            if (!PassesFilters(data)) continue;
            DrawRow(data);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.HelpBox(
            "근접임팩트/투사체 칸을 직접 채우면 그 값이 우선 적용되고, 비워두면(None) 프리셋 값을 대신 사용합니다. " +
            "여러 무기를 체크한 뒤 상단에서 프리셋을 골라 '선택 무기에 프리셋 일괄 적용'을 누르면 한번에 바뀝니다.",
            MessageType.Info);
    }

    private bool PassesFilters(WeaponData data)
    {
        if (attackTypeFilterIndex > 0)
        {
            string wanted = AttackTypeFilterOptions[attackTypeFilterIndex];
            if (data.attackType.ToString() != wanted) return false;
        }

        if (!string.IsNullOrEmpty(searchText))
        {
            string haystack = ((data.id ?? "") + " " + (data.weaponName ?? "")).ToLowerInvariant();
            if (!haystack.Contains(searchText.ToLowerInvariant())) return false;
        }

        return true;
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        searchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField, GUILayout.MinWidth(150));
        attackTypeFilterIndex = EditorGUILayout.Popup(attackTypeFilterIndex, AttackTypeFilterOptions, EditorStyles.toolbarPopup, GUILayout.Width(90));

        if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            RefreshList();
        }

        if (GUILayout.Button("모두 저장", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            AssetDatabase.SaveAssets();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"총 {weapons.Count}개", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawBulkBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField($"선택됨: {selected.Count}", GUILayout.Width(80));
        bulkPresetToApply = (WeaponVisualPreset)EditorGUILayout.ObjectField(
            bulkPresetToApply, typeof(WeaponVisualPreset), false, GUILayout.Width(220));

        using (new EditorGUI.DisabledScope(selected.Count == 0 || bulkPresetToApply == null))
        {
            if (GUILayout.Button("선택 무기에 프리셋 일괄 적용", EditorStyles.toolbarButton, GUILayout.Width(190)))
            {
                ApplyPresetToSelected();
            }
        }

        using (new EditorGUI.DisabledScope(selected.Count == 0))
        {
            if (GUILayout.Button("선택 해제", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                selected.Clear();
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void ApplyPresetToSelected()
    {
        foreach (WeaponData data in selected)
        {
            if (data == null) continue;
            SerializedObject so = new SerializedObject(data);
            so.Update();
            so.FindProperty("visualPreset").objectReferenceValue = bulkPresetToApply;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();
    }

private void DrawHeaderRow()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("", GUILayout.Width(20));
        GUILayout.Label("아이콘", GUILayout.Width(36));
        GUILayout.Label("id", GUILayout.Width(110));
        GUILayout.Label("이름", GUILayout.Width(100));
        GUILayout.Label("등급", GUILayout.Width(70));
        GUILayout.Label("타입", GUILayout.Width(65));
        GUILayout.Label("쿨타임", GUILayout.Width(45));
        GUILayout.Label("데미지", GUILayout.Width(45));
        GUILayout.Label("비주얼 프리셋", GUILayout.Width(150));
        GUILayout.Label("근접임팩트(오버라이드)", GUILayout.Width(140));
        GUILayout.Label("투사체(오버라이드)", GUILayout.Width(140));
        GUILayout.Label("사운드", GUILayout.Width(90));
        GUILayout.Label("", GUILayout.Width(45));
        EditorGUILayout.EndHorizontal();
    }

private void DrawRow(WeaponData data)
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();

        bool isSelected = selected.Contains(data);
        bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
        if (newSelected != isSelected)
        {
            if (newSelected) selected.Add(data); else selected.Remove(data);
        }

        Texture iconTex = null;
        if (data.ResolvedIcon != null)
        {
            iconTex = AssetPreview.GetAssetPreview(data.ResolvedIcon);
        }
        GUILayout.Label(iconTex != null ? iconTex : Texture2D.grayTexture, GUILayout.Width(32), GUILayout.Height(32));

        SerializedObject so = new SerializedObject(data);
        so.Update();

        EditorGUILayout.LabelField(data.id, GUILayout.Width(110));

        EditorGUILayout.PropertyField(so.FindProperty("weaponName"), GUIContent.none, GUILayout.Width(100));
        EditorGUILayout.PropertyField(so.FindProperty("grade"), GUIContent.none, GUILayout.Width(70));
        EditorGUILayout.PropertyField(so.FindProperty("attackType"), GUIContent.none, GUILayout.Width(65));
        EditorGUILayout.PropertyField(so.FindProperty("cooldown"), GUIContent.none, GUILayout.Width(45));
        EditorGUILayout.PropertyField(so.FindProperty("damage"), GUIContent.none, GUILayout.Width(45));
        EditorGUILayout.PropertyField(so.FindProperty("visualPreset"), GUIContent.none, GUILayout.Width(150));
        EditorGUILayout.PropertyField(so.FindProperty("meleeImpactPrefab"), GUIContent.none, GUILayout.Width(140));
        EditorGUILayout.PropertyField(so.FindProperty("projectilePrefab"), GUIContent.none, GUILayout.Width(140));

        bool isSoundExpanded = soundExpanded.Contains(data);
        string soundButtonLabel = $"사운드({(data.attackSounds != null ? data.attackSounds.Length : 0)})";
        if (GUILayout.Button(soundButtonLabel, GUILayout.Width(90)))
        {
            if (isSoundExpanded) soundExpanded.Remove(data); else soundExpanded.Add(data);
            isSoundExpanded = !isSoundExpanded;
        }

        so.ApplyModifiedProperties();

        if (GUILayout.Button("열기", GUILayout.Width(40)))
        {
            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);
        }

        EditorGUILayout.EndHorizontal();

        if (isSoundExpanded)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            SerializedObject soundSo = new SerializedObject(data);
            soundSo.Update();
            EditorGUILayout.PropertyField(soundSo.FindProperty("attackSounds"), new GUIContent("공격 사운드 클립 (여러 개면 무작위 재생)"), true);
            EditorGUILayout.PropertyField(soundSo.FindProperty("attackSoundVolume"), new GUIContent("볼륨 배율"));
            soundSo.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();
    }
}
