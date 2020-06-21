﻿/* MIT License

 * Copyright (c) 2020 Skurdt
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE. */

using UnityEngine;

namespace Arcade_r
{
    public sealed class ArcadeCylNormalState : ArcadeState
    {
        private const float INTERACT_MAX_DISTANCE = 40f;

        private float _timer;
        private float _acceleration;
        private float _speed;

        public ArcadeCylNormalState(ArcadeContext context)
        : base(context)
        {
        }

        public override void OnEnter()
        {
            Debug.Log($"> <color=green>Entered</color> {GetType().Name}");

            _context.App.PlayerCylControls.CylArcadeActions.Enable();
            if (Cursor.visible)
            {
                _context.App.PlayerCylControls.CylArcadeActions.Look.Disable();
            }

            _context.App.UIController.EnableNormalUI();

            _context.App.CurrentPlayerControls = _context.App.PlayerCylControls;

            _context.App.VideoPlayerController.SetPlayer(_context.App.PlayerCylControls.transform);
        }

        public override void OnExit()
        {
            Debug.Log($"> <color=orange>Exited</color> {GetType().Name}");

            _context.App.PlayerCylControls.CylArcadeActions.Disable();

            _context.App.UIController.DisableNormalUI();
        }

        public override void Update(float dt)
        {
            if (_context.App.PlayerCylControls.GlobalActions.Quit.triggered)
            {
                SystemUtils.ExitApp();
            }

            if (_context.App.PlayerCylControls.GlobalActions.ToggleCursor.triggered)
            {
                SystemUtils.ToggleMouseCursor();
                if (!Cursor.visible)
                {
                    _context.App.PlayerCylControls.CylArcadeActions.Look.Enable();
                }
                else
                {
                    _context.App.PlayerCylControls.CylArcadeActions.Look.Disable();
                }
            }

            InteractionController.FindInteractable(ref _context.CurrentModelConfiguration,
                                                   _context.App.PlayerCylControls.Camera,
                                                   INTERACT_MAX_DISTANCE,
                                                   _context.RaycastLayers);

            _context.App.VideoPlayerController.UpdateVideosState();

            if (!Cursor.visible && _context.App.PlayerCylControls.CylArcadeActions.Interact.triggered)
            {
                HandleInteraction();
            }

            if (_context.App.PlayerCylControls.CylArcadeActions.NavigationForward.phase == UnityEngine.InputSystem.InputActionPhase.Started)
            {
                if (_context.App.PlayerCylControls.CylArcadeActions.NavigationForward.triggered)
                {
                    _timer        = 0f;
                    _acceleration = 6f;
                    _speed        = 180f;
                    _context.App.ArcadeCylController.Forward(dt, 1, _speed);
                }
                else if ((_timer += _acceleration * dt) > 2.0f)
                {
                    ++_acceleration;
                    ++_speed;
                    _acceleration = Mathf.Clamp(_acceleration, 6f, 100f);
                    _speed        = Mathf.Clamp(_speed, 180f, 360f);
                    _context.App.ArcadeCylController.Forward(dt, 1, _speed);
                    _timer = 0f;
                }
            }

            if (_context.App.PlayerCylControls.CylArcadeActions.NavigationBackward.phase == UnityEngine.InputSystem.InputActionPhase.Started)
            {
                if (_context.App.PlayerCylControls.CylArcadeActions.NavigationBackward.triggered)
                {
                    _timer        = 0f;
                    _acceleration = 6f;
                    _speed        = 180f;
                    _context.App.ArcadeCylController.Backward(dt, 1, _speed);
                }
                else if ((_timer += _acceleration * dt) > 2.0f)
                {
                    ++_acceleration;
                    ++_speed;
                    _acceleration = Mathf.Clamp(_acceleration, 6f, 100f);
                    _speed        = Mathf.Clamp(_speed, 180f, 360f);
                    _context.App.ArcadeCylController.Backward(dt, 1, _speed);
                     _timer = 0f;
               }
            }
        }

        private void HandleInteraction()
        {
            if (_context.CurrentModelConfiguration == null)
            {
                return;
            }

            //if (_context.CurrentModelConfiguration.Grabbable)
            //{
            //    _context.TransitionTo<ArcadeGrabState>();
            //}
            //else
            {
                switch (_context.CurrentModelConfiguration.InteractionType)
                {
                    case InteractionType.GameInternal:
                    {
                        _context.TransitionTo<ArcadeInternalGameState>();
                    }
                    break;
                    case InteractionType.GameExternal:
                    {
                        _context.TransitionTo<ArcadeExternalGameState>();
                    }
                    break;
                    case InteractionType.FpsArcadeConfiguration:
                    {
                        _context.SetAndStartCurrentArcadeConfiguration(_context.CurrentModelConfiguration.Id, ArcadeType.Fps);
                    }
                    break;
                    case InteractionType.CylArcadeConfiguration:
                    {
                        _context.SetAndStartCurrentArcadeConfiguration(_context.CurrentModelConfiguration.Id, ArcadeType.Cyl);
                    }
                    break;
                    case InteractionType.FpsMenuConfiguration:
                    case InteractionType.CylMenuConfiguration:
                    case InteractionType.URL:
                    case InteractionType.None:
                    default:
                        break;
                }
            }
        }
    }
}
