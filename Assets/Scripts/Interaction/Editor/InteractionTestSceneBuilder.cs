using GameUP.Rules;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameUP.Interaction.Editor
{
    public static class InteractionTestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Tests_Interaction.unity";

        [MenuItem("GameUP32/Interaction/Build Test Scene")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);

            CreatePrimitive("Floor", PrimitiveType.Cube, new Vector3(0f, -0.15f, 0.8f), new Vector3(9f, 0.3f, 9f), new Color(0.22f, 0.25f, 0.27f));
            CreatePrimitive("Worktable", PrimitiveType.Cube, new Vector3(0f, 0.83f, 0.4f), new Vector3(3.5f, 0.18f, 2.1f), new Color(0.42f, 0.31f, 0.21f));

            GameObject systemRoot = new GameObject("Systems");
            InspectionService service = systemRoot.AddComponent<InspectionService>();
            ProductPresentation presentation = systemRoot.AddComponent<ProductPresentation>();
            InteractionTestBootstrap bootstrap = systemRoot.AddComponent<InteractionTestBootstrap>();

            Transform arrival = CreatePoint("ArrivalPoint", new Vector3(-1.1f, 0.94f, 0.15f));
            Transform desk = CreatePoint("DeskPoint", new Vector3(0f, 0.94f, 0.35f));
            Transform approved = CreatePoint("ApprovedExitPoint", new Vector3(2.2f, 0.98f, 0.25f));
            Transform disposal = CreatePoint("FoodDisposalPoint", new Vector3(-2.2f, 0.98f, 0.25f));
            Transform inspect = CreatePoint("InspectionAnchor", new Vector3(0f, 1.55f, -0.55f));

            SetReference(presentation, "inspectionService", service);
            SetReference(presentation, "arrivalPoint", arrival);
            SetReference(presentation, "deskPoint", desk);
            SetReference(presentation, "approvedExitPoint", approved);
            SetReference(presentation, "foodDisposalPoint", disposal);
            SetReference(bootstrap, "inspectionService", service);

            GameObject yawRoot = new GameObject("PlayerRig");
            yawRoot.transform.position = new Vector3(0f, 1.8f, -3.2f);
            GameObject cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(yawRoot.transform, false);
            cameraObject.transform.localRotation = Quaternion.identity;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            cameraObject.AddComponent<AudioListener>();
            Vector3 lookAngles = Quaternion.LookRotation(desk.position - cameraObject.transform.position, Vector3.up).eulerAngles;
            yawRoot.transform.rotation = Quaternion.Euler(0f, lookAngles.y, 0f);
            cameraObject.transform.localRotation = Quaternion.Euler(lookAngles.x - 3f, 0f, 0f);

            WorkstationInteractionController controller = yawRoot.AddComponent<WorkstationInteractionController>();
            SetReference(controller, "playerCamera", camera);
            SetReference(controller, "cameraYawRoot", yawRoot.transform);
            SetReference(controller, "inspectionService", service);
            SetReference(controller, "presentation", presentation);
            SetReference(controller, "inspectionAnchor", inspect);
            SetReference(bootstrap, "interactionController", controller);

            AddTarget("Analyzer", "ANALİZ", WorkstationAction.Analyzer, new Vector3(-0.8f, 1.15f, 0.95f), new Color(0.12f, 0.78f, 0.9f));
            AddTarget("Approve", "ONAY", WorkstationAction.Approve, new Vector3(0f, 1.15f, 0.95f), new Color(0.22f, 0.82f, 0.3f));
            AddTarget("Reject", "RED", WorkstationAction.Reject, new Vector3(0.8f, 1.15f, 0.95f), new Color(0.92f, 0.18f, 0.12f));
            AddTarget("Waste Paper", "KAĞIT", WorkstationAction.Paper, new Vector3(-1.1f, 1.52f, 1.25f), new Color(0.95f, 0.72f, 0.28f));
            AddTarget("Waste Glass", "CAM", WorkstationAction.Glass, new Vector3(-0.38f, 1.52f, 1.25f), new Color(0.16f, 0.82f, 0.78f));
            AddTarget("Waste Plastic", "PLASTİK", WorkstationAction.Plastic, new Vector3(0.38f, 1.52f, 1.25f), new Color(0.2f, 0.45f, 0.98f));
            AddTarget("Waste Contaminated", "KİRLİ", WorkstationAction.Contaminated, new Vector3(1.1f, 1.52f, 1.25f), new Color(0.52f, 0.38f, 0.5f));

            var light = new GameObject("Key Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.transform.rotation = Quaternion.Euler(45f, -25f, 0f);

            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"Interaction test scene created: {ScenePath}");
        }

        private static Transform CreatePoint(string name, Vector3 position)
        {
            GameObject point = new GameObject(name);
            point.transform.position = position;
            return point.transform;
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = GetOrCreateMaterial(name, color);
            return go;
        }

        private static void AddTarget(string name, string label, WorkstationAction action, Vector3 position, Color color)
        {
            GameObject target = CreatePrimitive(name, PrimitiveType.Cube, position, new Vector3(0.62f, 0.28f, 0.18f), color);
            target.AddComponent<WorkstationTarget>();
            SerializedObject serialized = new SerializedObject(target.GetComponent<WorkstationTarget>());
            serialized.FindProperty("action").enumValueIndex = (int)action;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject labelObject = new GameObject($"Label - {label}");
            // Keep labels on the front face of each button so they do not float into the product's view.
            labelObject.transform.position = position + new Vector3(0f, 0f, -0.101f);
            labelObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            labelObject.transform.localScale = new Vector3(-1f, 1f, 1f);
            TextMesh text = labelObject.AddComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 48;
            text.characterSize = 0.025f;
            text.color = Color.black;
            Renderer labelRenderer = labelObject.GetComponent<Renderer>();
            labelRenderer.sharedMaterial = GetOrCreateLabelMaterial(text);
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            const string materialFolder = "Assets/Art/InteractionTestMaterials";
            if (!AssetDatabase.IsValidFolder(materialFolder))
                AssetDatabase.CreateFolder("Assets/Art", "InteractionTestMaterials");

            string assetPath = $"{materialFolder}/{name.Replace(' ', '_')}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material != null) return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) throw new System.InvalidOperationException("No supported shader found for interaction test materials.");

            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        private static Material GetOrCreateLabelMaterial(TextMesh text)
        {
            const string materialPath = "Assets/Art/InteractionTestMaterials/LabelText.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existing != null) return existing;

            Font font = text.font != null ? text.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) throw new System.InvalidOperationException("Could not find the built-in font for interaction labels.");
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new System.InvalidOperationException("Could not find the depth-tested label shader.");

            Material material = new Material(font.material)
            {
                name = "InteractionTest_LabelText",
                shader = shader,
                mainTexture = font.material.mainTexture
            };
            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new System.InvalidOperationException($"Missing serialized field {propertyName} on {target.name}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
