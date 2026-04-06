using DreamBlastClone.Core;

namespace DreamBlastClone.Items
{
    public sealed class RocketItemModel : ItemModel
    {
        public RocketItemModel(RocketOrientation orientation)
        {
            Orientation = orientation;
        }

        public RocketOrientation Orientation { get; }
    }
}
