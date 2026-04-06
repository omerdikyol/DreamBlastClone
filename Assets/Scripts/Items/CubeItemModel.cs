using DreamBlastClone.Core;

namespace DreamBlastClone.Items
{
    public sealed class CubeItemModel : ItemModel
    {
        public CubeItemModel(CubeColor color)
        {
            Color = color;
        }

        public CubeColor Color { get; }
    }
}
