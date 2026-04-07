using System;
using DreamBlastClone.Core;
using DreamBlastClone.Controllers;
using DreamBlastClone.Views;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class BoardInputSessionBridge : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private LevelSessionHost sessionHost;
        [SerializeField] private Camera inputCamera;

        public event Action<LevelSessionTapResult> TapProcessed;

        private void Start()
        {
            RenderCurrentBoard();
        }

        private void Update()
        {
            if (TryReadScreenTap(out var screenPosition))
            {
                TryHandleScreenTap(screenPosition);
            }
        }

        public bool TryHandleScreenTap(Vector2 screenPosition)
        {
            if (!TryResolveDependencies(out var session))
            {
                return false;
            }

            var board = session.Board;
            var worldPoint = ScreenToBoardWorldPoint(screenPosition);

            if (!boardView.TryWorldToBoardCoordinate(board, worldPoint, out var coordinate))
            {
                return false;
            }

            var tapResult = session.ProcessTap(coordinate);
            boardView.Render(board);
            TapProcessed?.Invoke(tapResult);
            return true;
        }

        private bool TryResolveDependencies(out LevelSession session)
        {
            session = null;

            if (boardView is null || sessionHost is null || inputCamera is null)
            {
                return false;
            }

            session = sessionHost.Session;
            return session is not null;
        }

        private void RenderCurrentBoard()
        {
            if (boardView is null || sessionHost?.Session is null)
            {
                return;
            }

            boardView.Render(sessionHost.Session.Board);
        }

        private Vector3 ScreenToBoardWorldPoint(Vector2 screenPosition)
        {
            var boardPlaneDistance = boardView.transform.position.z - inputCamera.transform.position.z;
            return inputCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, boardPlaneDistance));
        }

        private static bool TryReadScreenTap(out Vector2 screenPosition)
        {
            if (Mouse.current is not null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current is not null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            screenPosition = default;
            return false;
        }
    }
}
