using UnityEditor;
using UnityEngine;

/// <summary>
/// WeaponData의 커스텀 인스펙터. attackType에 따라 실제로 쓰이는 필드만 골라서 보여준다.
/// - Melee: 부채꼴 판정(inner/outer/angle) + 근접 임팩트 이펙트
/// - Ranged / Homing: 투사체 프리팹 + 속도/수명/관통
/// - Area: 탐색 반경(outerRadius 재사용) + 메테오 프리팹 + 폭발/불장판
/// - Orbit: 쿨타임 없이 구슬 개수/반경/회전주기/프리팹
/// 필드 자체는 WeaponData.cs에 그대로 있고, 여기서는 표시 여부만 제어한다(값은 전부 보존됨).
/// </summary>
[CustomEditor(typeof(WeaponData))]
[CanEditMultipleObjects]
public class WeaponDataEditor : Editor
{
    private SerializedProperty idProp;
    private SerializedProperty weaponNameProp;
    private SerializedProperty iconProp;
    private SerializedProperty visualPresetProp;
    private SerializedProperty gradeProp;

    private SerializedProperty attackTypeProp;
    private SerializedProperty aimTypeProp;

    private SerializedProperty cooldownProp;

    private SerializedProperty innerRadiusProp;
    private SerializedProperty outerRadiusProp;
    private SerializedProperty angleProp;

    private SerializedProperty damageProp;

    private SerializedProperty attackSoundsProp;
    private SerializedProperty attackSoundVolumeProp;

    private SerializedProperty meleeImpactPrefabProp;
    private SerializedProperty meleeImpactLifetimeProp;

    private SerializedProperty projectilePrefabProp;
    private SerializedProperty projectileSpeedProp;
    private SerializedProperty projectileLifetimeProp;
    private SerializedProperty pierceProp;

    private SerializedProperty orbCountProp;
    private SerializedProperty orbRadiusProp;
    private SerializedProperty orbRotationPeriodProp;
    private SerializedProperty orbPrefabProp;

    private SerializedProperty explosionRadiusProp;
    private SerializedProperty fallDurationProp;
    private SerializedProperty explosionPrefabProp;
    private SerializedProperty explosionEffectLifetimeProp;
    private SerializedProperty fireFloorPrefabProp;
    private SerializedProperty fireFloorDurationProp;
    private SerializedProperty fireFloorTickDamageProp;
    private SerializedProperty fireFloorTickIntervalProp;

    private SerializedProperty poolPrewarmCountProp;

    private void OnEnable()
    {
        idProp = serializedObject.FindProperty("id");
        weaponNameProp = serializedObject.FindProperty("weaponName");
        iconProp = serializedObject.FindProperty("icon");
        visualPresetProp = serializedObject.FindProperty("visualPreset");
        gradeProp = serializedObject.FindProperty("grade");

        attackTypeProp = serializedObject.FindProperty("attackType");
        aimTypeProp = serializedObject.FindProperty("aimType");

        cooldownProp = serializedObject.FindProperty("cooldown");

        innerRadiusProp = serializedObject.FindProperty("innerRadius");
        outerRadiusProp = serializedObject.FindProperty("outerRadius");
        angleProp = serializedObject.FindProperty("angle");

        damageProp = serializedObject.FindProperty("damage");

        attackSoundsProp = serializedObject.FindProperty("attackSounds");
        attackSoundVolumeProp = serializedObject.FindProperty("attackSoundVolume");

        meleeImpactPrefabProp = serializedObject.FindProperty("meleeImpactPrefab");
        meleeImpactLifetimeProp = serializedObject.FindProperty("meleeImpactLifetime");

        projectilePrefabProp = serializedObject.FindProperty("projectilePrefab");
        projectileSpeedProp = serializedObject.FindProperty("projectileSpeed");
        projectileLifetimeProp = serializedObject.FindProperty("projectileLifetime");
        pierceProp = serializedObject.FindProperty("pierce");

        orbCountProp = serializedObject.FindProperty("orbCount");
        orbRadiusProp = serializedObject.FindProperty("orbRadius");
        orbRotationPeriodProp = serializedObject.FindProperty("orbRotationPeriod");
        orbPrefabProp = serializedObject.FindProperty("orbPrefab");

        explosionRadiusProp = serializedObject.FindProperty("explosionRadius");
        fallDurationProp = serializedObject.FindProperty("fallDuration");
        explosionPrefabProp = serializedObject.FindProperty("explosionPrefab");
        explosionEffectLifetimeProp = serializedObject.FindProperty("explosionEffectLifetime");
        fireFloorPrefabProp = serializedObject.FindProperty("fireFloorPrefab");
        fireFloorDurationProp = serializedObject.FindProperty("fireFloorDuration");
        fireFloorTickDamageProp = serializedObject.FindProperty("fireFloorTickDamage");
        fireFloorTickIntervalProp = serializedObject.FindProperty("fireFloorTickInterval");

        poolPrewarmCountProp = serializedObject.FindProperty("poolPrewarmCount");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        WeaponAttackType attackType = (WeaponAttackType)attackTypeProp.enumValueIndex;
        bool isMelee = attackType == WeaponAttackType.Melee;
        bool isArea = attackType == WeaponAttackType.Area;
        bool isOrbit = attackType == WeaponAttackType.Orbit;
        bool isRangedLike = attackType == WeaponAttackType.Ranged || attackType == WeaponAttackType.Homing;

        DrawHeader("Info");
        EditorGUILayout.PropertyField(idProp);
        EditorGUILayout.PropertyField(weaponNameProp);
        EditorGUILayout.PropertyField(iconProp);
        EditorGUILayout.PropertyField(visualPresetProp);
        EditorGUILayout.PropertyField(gradeProp);

        DrawHeader("Type");
        EditorGUILayout.PropertyField(attackTypeProp);
        if (!isOrbit)
        {
            EditorGUILayout.PropertyField(aimTypeProp);
        }

        if (!isOrbit)
        {
            DrawHeader("Timing");
            EditorGUILayout.PropertyField(cooldownProp);
        }

        if (isMelee)
        {
            DrawHeader("Melee Hitbox (부채꼴)");
            EditorGUILayout.PropertyField(innerRadiusProp);
            EditorGUILayout.PropertyField(outerRadiusProp);
            EditorGUILayout.PropertyField(angleProp);
        }
        else if (isArea)
        {
            DrawHeader("Target Search");
            EditorGUILayout.PropertyField(outerRadiusProp, new GUIContent("탐색 반경 (outerRadius)"));
        }

        DrawHeader("Damage");
        EditorGUILayout.PropertyField(damageProp);

        DrawHeader("Sound");
        EditorGUILayout.PropertyField(attackSoundsProp, true);
        EditorGUILayout.PropertyField(attackSoundVolumeProp);

        if (isMelee)
        {
            DrawHeader("Melee Impact Prefab");
            EditorGUILayout.PropertyField(meleeImpactPrefabProp);
            EditorGUILayout.PropertyField(meleeImpactLifetimeProp);
        }

        if (isRangedLike || isArea)
        {
            DrawHeader(isArea ? "Meteor Prefab" : "Projectile Prefab");
            EditorGUILayout.PropertyField(projectilePrefabProp);
        }

        if (isRangedLike)
        {
            DrawHeader("Ranged (투사체)");
            EditorGUILayout.PropertyField(projectileSpeedProp);
            EditorGUILayout.PropertyField(projectileLifetimeProp);
            EditorGUILayout.PropertyField(pierceProp);
        }

        if (isOrbit)
        {
            DrawHeader("Orbit (요술봉 등 패시브 구슬)");
            EditorGUILayout.PropertyField(orbCountProp);
            EditorGUILayout.PropertyField(orbRadiusProp);
            EditorGUILayout.PropertyField(orbRotationPeriodProp);
            EditorGUILayout.PropertyField(orbPrefabProp);
        }

        if (isArea)
        {
            DrawHeader("Area / Meteor (범위 공격)");
            EditorGUILayout.PropertyField(explosionRadiusProp);
            EditorGUILayout.PropertyField(fallDurationProp);
            EditorGUILayout.PropertyField(explosionPrefabProp);
            EditorGUILayout.PropertyField(explosionEffectLifetimeProp);
            EditorGUILayout.PropertyField(fireFloorPrefabProp);
            EditorGUILayout.PropertyField(fireFloorDurationProp);
            EditorGUILayout.PropertyField(fireFloorTickDamageProp);
            EditorGUILayout.PropertyField(fireFloorTickIntervalProp);
        }

        DrawHeader("Pooling");
        EditorGUILayout.PropertyField(poolPrewarmCountProp);

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawHeader(string label)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    }
}
