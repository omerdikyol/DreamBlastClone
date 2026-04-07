using DreamBlastClone.Core;

namespace DreamBlastClone.Systems
{
    public interface IRefillCubeColorResolver
    {
        CubeColor ResolveColor(BoardCoordinate coordinate);
    }
}
