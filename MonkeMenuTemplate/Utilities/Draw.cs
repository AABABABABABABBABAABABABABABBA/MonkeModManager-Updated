using UnityEngine;

namespace MonkeMenu.Utilities
{
    /// <summary>Helpers for making simple coloured shapes and text at runtime.</summary>
    public static class Draw
    {
        private static Shader shader;
        private static Font font;

        /// <summary>Gorilla Tag uses URP, so the built-in Standard shader would render pink.</summary>
        private static Shader Shader =>
            shader != null ? shader : (shader =
                UnityEngine.Shader.Find("GorillaTag/UberShader")
                ?? UnityEngine.Shader.Find("Universal Render Pipeline/Unlit")
                ?? UnityEngine.Shader.Find("Unlit/Color")
                ?? UnityEngine.Shader.Find("Sprites/Default"));

        private static Font Font
        {
            get
            {
                if (font != null) return font;
                // The built-in font's name changed in Unity 2022.2.
                try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                if (font == null)
                {
                    try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
                }
                return font;
            }
        }

        public static void SetColor(GameObject obj, Color color)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null) return;
            var material = renderer.material;
            if (Shader != null) material.shader = Shader;
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        }

        private static GameObject Primitive(PrimitiveType type, Color color)
        {
            var obj = GameObject.CreatePrimitive(type);
            // No colliders: we don't want the menu to bump into the player's hands.
            Object.Destroy(obj.GetComponent<Collider>());
            SetColor(obj, color);
            return obj;
        }

        public static GameObject Cube(Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var cube = Primitive(PrimitiveType.Cube, color);
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            return cube;
        }

        public static GameObject Sphere(float diameter, Color color)
        {
            var sphere = Primitive(PrimitiveType.Sphere, color);
            sphere.transform.localScale = Vector3.one * diameter;
            return sphere;
        }

        /// <summary>
        /// Text facing out of the menu (+X in menu space, with +Z as up).
        /// characterSize controls how big it is in the world.
        /// </summary>
        public static TextMesh Text(Transform parent, string text, Vector3 localPosition, float characterSize, FontStyle style)
        {
            var obj = new GameObject("Text: " + text);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = Quaternion.LookRotation(Vector3.left, Vector3.forward);

            var mesh = obj.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 40;
            mesh.characterSize = characterSize;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontStyle = style;
            mesh.color = Menu.MenuSettings.TextColor;
            if (Font != null)
            {
                mesh.font = Font;
                obj.GetComponent<MeshRenderer>().material = Font.material;
            }
            return mesh;
        }

        public static void Destroy(GameObject obj)
        {
            if (obj != null) Object.Destroy(obj);
        }
    }
}
