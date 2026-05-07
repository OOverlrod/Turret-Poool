#if UNITY_EDITOR
using System.IO;
using TurretDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TurretDemo.EditorTools
{
    /// <summary>
    /// 샘플 씬에 Primitive 기반 사격장·터렛·드론 계층과 프리팹을 생성합니다.
    /// Unity 메뉴: Turret &gt; Build Demo Scene
    /// </summary>
    public static class TurretDemoSceneBuilder
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string ProjectilePrefabPath = "Assets/Prefabs/Turret/Projectile.prefab";
        private const string EnemyPrefabPath = "Assets/Prefabs/Turret/Enemy.prefab";

        [MenuItem("Turret/Build Demo Scene")]
        public static void BuildDemoSceneFromMenu()
        {
            BuildInternal();
        }

        /// <summary>
        /// CI/배치모드에서 씬을 구성할 때 호출합니다. (예: -executeMethod TurretDemo.EditorTools.TurretDemoSceneBuilder.BuildDemoSceneForBatch)
        /// </summary>
        public static void BuildDemoSceneForBatch()
        {
            BuildInternal();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void BuildInternal()
        {
            Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            RemovePreviousDemoRoots();

            GameObject shootingFloor = CreatePrimitiveCube("ShootingRangeFloor", PrimitiveType.Cube);
            shootingFloor.transform.SetParent(null, false);
            shootingFloor.transform.position = new Vector3(0f, -0.1f, 0f);
            shootingFloor.transform.localScale = new Vector3(24f, 0.2f, 24f);

            GameObject turretRoot = new GameObject("TurretRoot");
            turretRoot.transform.SetParent(null, false);
            turretRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            GameObject turretBase = CreatePrimitiveCube("TurretBase", PrimitiveType.Cube);
            turretBase.transform.SetParent(turretRoot.transform, false);
            turretBase.transform.localPosition = new Vector3(0f, 0.075f, 0f);
            turretBase.transform.localScale = new Vector3(2f, 0.15f, 2f);

            GameObject yawPivot = new GameObject("YawPivot");
            yawPivot.transform.SetParent(turretRoot.transform, false);
            yawPivot.transform.localPosition = new Vector3(0f, 0.16f, 0f);

            GameObject turretHead = CreatePrimitiveCube("TurretHead", PrimitiveType.Cube);
            turretHead.transform.SetParent(yawPivot.transform, false);
            turretHead.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            turretHead.transform.localScale = new Vector3(0.55f, 0.35f, 0.55f);

            GameObject pitchPivot = new GameObject("PitchPivot");
            pitchPivot.transform.SetParent(yawPivot.transform, false);
            pitchPivot.transform.localPosition = new Vector3(0f, 0.12f, 0.05f);
            pitchPivot.transform.localRotation = Quaternion.identity;

            GameObject barrel = CreatePrimitiveCube("Barrel", PrimitiveType.Cube);
            barrel.transform.SetParent(pitchPivot.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0f, 0.2f);
            barrel.transform.localRotation = Quaternion.identity;
            barrel.transform.localScale = new Vector3(0.12f, 0.12f, 1.4f);

            GameObject muzzlePoint = new GameObject("MuzzlePoint");
            muzzlePoint.transform.SetParent(barrel.transform, false);
            muzzlePoint.transform.localPosition = new Vector3(0f, 0f, 0.71f);
            muzzlePoint.transform.localRotation = Quaternion.identity;

            GameObject projectilePrefabAsset = CreateProjectilePrefabAsset();
            GameObject enemyPrefabAsset = CreateEnemyPrefabAsset();

            GameObject enemySpawnSystem = new GameObject("EnemySpawnSystem");
            enemySpawnSystem.transform.SetParent(null, false);
            enemySpawnSystem.transform.position = Vector3.zero;

            GameObject enemyRoot = new GameObject("EnemyRoot");
            enemyRoot.transform.SetParent(enemySpawnSystem.transform, false);

            GameObject spawnPointsRoot = new GameObject("EnemySpawnPoints");
            spawnPointsRoot.transform.SetParent(enemySpawnSystem.transform, false);

            Transform[] spawnPoints = CreateSpawnPoints(spawnPointsRoot.transform);

            NearestEnemyTurretController autoTurret = turretRoot.AddComponent<NearestEnemyTurretController>();
            TurretGizmoVisualizer gizmoVisualizer = turretRoot.AddComponent<TurretGizmoVisualizer>();
            EnemySpawner enemySpawner = enemySpawnSystem.AddComponent<EnemySpawner>();

            SerializedObject so = new SerializedObject(autoTurret);
            so.FindProperty("yawPivot").objectReferenceValue = yawPivot.transform;
            so.FindProperty("pitchPivot").objectReferenceValue = pitchPivot.transform;
            so.FindProperty("muzzlePoint").objectReferenceValue = muzzlePoint.transform;
            so.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefabAsset;
            so.FindProperty("yawSpeedDegreesPerSecond").floatValue = 120f;
            so.FindProperty("pitchSpeedDegreesPerSecond").floatValue = 90f;
            so.FindProperty("minPitchDegrees").floatValue = -45f;
            so.FindProperty("maxPitchDegrees").floatValue = 20f;
            so.FindProperty("fireAngleThresholdDegrees").floatValue = 5f;
            so.FindProperty("fireIntervalSeconds").floatValue = 0.5f;
            so.FindProperty("projectileSpeed").floatValue = 12f;
            so.FindProperty("projectileLifeTimeSeconds").floatValue = 3f;
            so.FindProperty("projectileDamage").floatValue = 10f;
            so.FindProperty("turretTeamId").intValue = 0;
            so.FindProperty("engagementRangeWorldUnits").floatValue = 18f;
            so.FindProperty("showDebugGizmos").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject targetSelectorSo = new SerializedObject(autoTurret);
            targetSelectorSo.FindProperty("selfTeamId").intValue = 0;
            targetSelectorSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject gizmoSo = new SerializedObject(gizmoVisualizer);
            gizmoSo.FindProperty("turretController").objectReferenceValue = autoTurret;
            gizmoSo.FindProperty("drawOnlyWhenSelected").boolValue = false;
            gizmoSo.FindProperty("showAimCone").boolValue = true;
            gizmoSo.FindProperty("showRays").boolValue = true;
            gizmoSo.FindProperty("showEngagementSphere").boolValue = false;
            gizmoSo.FindProperty("aimConeSegments").intValue = 32;
            gizmoSo.FindProperty("gizmoRayLengthWorldUnits").floatValue = 8f;
            gizmoSo.FindProperty("aimConeDepthWorldUnits").floatValue = 4f;
            gizmoSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject spawnerSo = new SerializedObject(enemySpawner);
            spawnerSo.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefabAsset;
            spawnerSo.FindProperty("spawnPoints").arraySize = spawnPoints.Length;
            for (int index = 0; index < spawnPoints.Length; index++)
            {
                spawnerSo.FindProperty("spawnPoints").GetArrayElementAtIndex(index).objectReferenceValue = spawnPoints[index];
            }
            spawnerSo.FindProperty("enemyRoot").objectReferenceValue = enemyRoot.transform;
            spawnerSo.FindProperty("lookAtCenter").objectReferenceValue = turretRoot.transform;
            spawnerSo.FindProperty("initialSpawnCount").intValue = 4;
            spawnerSo.FindProperty("maxAliveCount").intValue = 8;
            spawnerSo.FindProperty("spawnIntervalSeconds").floatValue = 1f;
            spawnerSo.FindProperty("enemyMoveSpeedUnitsPerSecond").floatValue = 5f;
            spawnerSo.FindProperty("enemyLifeTimeSeconds").floatValue = 10f;
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();

            RepositionMainCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[TurretDemo] Build 완료: SampleScene 저장, Projectile 프리팹 생성. Play Mode로 동작을 확인하세요.");
        }

        private static void RemovePreviousDemoRoots()
        {
            DestroyRootIfExists("TurretRoot");
            DestroyRootIfExists("ShootingRangeFloor");
            DestroyRootIfExists("EnemySpawnSystem");
        }

        private static void DestroyRootIfExists(string objectName)
        {
            GameObject found = GameObject.Find(objectName);
            if (found != null && found.transform.parent == null)
            {
                Object.DestroyImmediate(found);
            }
        }

        private static GameObject CreatePrimitiveCube(string objectName, PrimitiveType primitiveType)
        {
            GameObject created = GameObject.CreatePrimitive(primitiveType);
            created.name = objectName;
            return created;
        }

        private static GameObject CreateProjectilePrefabAsset()
        {
            string directory = Path.GetDirectoryName(ProjectilePrefabPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            GameObject projectileInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileInstance.name = "Projectile";
            projectileInstance.transform.localScale = Vector3.one * 0.12f;
            SphereCollider projectileCollider = projectileInstance.GetComponent<SphereCollider>();
            if (projectileCollider != null)
            {
                projectileCollider.isTrigger = true;
            }

            Rigidbody projectileBody = projectileInstance.AddComponent<Rigidbody>();
            projectileBody.useGravity = false;
            projectileBody.isKinematic = true;
            projectileInstance.AddComponent<ProjectileMover>();

            GameObject prefabRoot = PrefabUtility.SaveAsPrefabAsset(projectileInstance, ProjectilePrefabPath);
            Object.DestroyImmediate(projectileInstance);
            return prefabRoot;
        }

        private static GameObject CreateEnemyPrefabAsset()
        {
            string directory = Path.GetDirectoryName(EnemyPrefabPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            GameObject enemyInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyInstance.name = "Enemy";
            enemyInstance.transform.localScale = new Vector3(0.6f, 0.35f, 0.6f);
            EnemyTarget enemyTarget = enemyInstance.AddComponent<EnemyTarget>();
            enemyInstance.AddComponent<EnemyLinearMover>();

            SerializedObject enemyTargetSo = new SerializedObject(enemyTarget);
            enemyTargetSo.FindProperty("teamId").intValue = 1;
            enemyTargetSo.FindProperty("maxHealth").floatValue = 10f;
            enemyTargetSo.FindProperty("showDeathMarker").boolValue = true;
            enemyTargetSo.FindProperty("deathMarkerLifeTimeSeconds").floatValue = 0.35f;
            enemyTargetSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefabRoot = PrefabUtility.SaveAsPrefabAsset(enemyInstance, EnemyPrefabPath);
            Object.DestroyImmediate(enemyInstance);
            return prefabRoot;
        }

        private static Transform[] CreateSpawnPoints(Transform parent)
        {
            const int spawnCount = 6;
            const float radius = 11f;
            const float yHeight = 1.3f;
            Transform[] points = new Transform[spawnCount];

            for (int index = 0; index < spawnCount; index++)
            {
                float angle = (Mathf.PI * 2f * index) / spawnCount;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, yHeight, Mathf.Sin(angle) * radius);

                GameObject spawnPointObject = new GameObject($"SpawnPoint_{index + 1}");
                spawnPointObject.transform.SetParent(parent, false);
                spawnPointObject.transform.position = position;
                spawnPointObject.transform.rotation = Quaternion.LookRotation((-position).normalized, Vector3.up);
                points[index] = spawnPointObject.transform;
            }

            return points;
        }

        private static void RepositionMainCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            mainCamera.transform.SetPositionAndRotation(
                new Vector3(0f, 6.5f, -14f),
                Quaternion.Euler(18f, 0f, 0f));
        }
    }
}
#endif
