using System;

namespace DreamBlastClone.Systems
{
    public sealed class BoardTapDispatchResult
    {
        public BoardTapDispatchResult(
            bool isValidTap,
            TapRouteType routeType,
            NormalCubeTapPipelineResult normalCube,
            SpecialItemTapPipelineResult specialItem)
        {
            IsValidTap = isValidTap;
            RouteType = routeType;
            NormalCube = normalCube ?? throw new ArgumentNullException(nameof(normalCube));
            SpecialItem = specialItem ?? throw new ArgumentNullException(nameof(specialItem));
        }

        public bool IsValidTap { get; }

        public TapRouteType RouteType { get; }

        public NormalCubeTapPipelineResult NormalCube { get; }

        public SpecialItemTapPipelineResult SpecialItem { get; }

        public static BoardTapDispatchResult Invalid()
        {
            return new BoardTapDispatchResult(
                isValidTap: false,
                routeType: TapRouteType.None,
                normalCube: NormalCubeTapPipelineResult.Invalid(),
                specialItem: SpecialItemTapPipelineResult.Invalid());
        }
    }
}
