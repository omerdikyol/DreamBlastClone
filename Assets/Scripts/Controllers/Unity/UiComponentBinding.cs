using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace DreamBlastClone.Controllers.Unity
{
    internal static class UiComponentBinding
    {
        public static void SetText(Component target, string value)
        {
            if (target is null)
            {
                return;
            }

            var textProperty = target.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);

            if (textProperty?.CanWrite == true && textProperty.PropertyType == typeof(string))
            {
                textProperty.SetValue(target, value);
            }
        }

        public static void SetSprite(Component target, Sprite sprite)
        {
            switch (target)
            {
                case Image image:
                    image.sprite = sprite;
                    break;
                case SpriteRenderer spriteRenderer:
                    spriteRenderer.sprite = sprite;
                    break;
            }
        }

        public static void SetColor(Component target, Color color)
        {
            switch (target)
            {
                case Graphic graphic:
                    graphic.color = color;
                    break;
                case SpriteRenderer spriteRenderer:
                    spriteRenderer.color = color;
                    break;
            }
        }

        public static void SetEnabled(Component target, bool isEnabled)
        {
            switch (target)
            {
                case Behaviour behaviour:
                    behaviour.enabled = isEnabled;
                    break;
                case Renderer renderer:
                    renderer.enabled = isEnabled;
                    break;
            }
        }
    }
}
