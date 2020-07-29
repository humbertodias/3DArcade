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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Video;

namespace Arcade
{
    public abstract class CylArcadeController : ArcadeController
    {
        protected abstract Transform TransformAnchor { get; }
        protected abstract Vector3 TransformVector { get; }

        protected CylArcadeProperties _cylArcadeProperties;

        protected int _sprockets;
        protected int _selectionIndex;
        protected Vector3 _centerTargetPosition;

        protected Transform _targetSelection;

        public CylArcadeController(ArcadeHierarchy arcadeHierarchy,
                                   PlayerFpsControls playerFpsControls,
                                   PlayerCylControls playerCylControls,
                                   Database<EmulatorConfiguration> emulatorDatabase,
                                   AssetCache<GameObject> gameObjectCache,
                                   AssetCache<Texture> textureCache,
                                   AssetCache<string> videoCache)
        : base(arcadeHierarchy, playerFpsControls, playerCylControls, emulatorDatabase, gameObjectCache, textureCache, videoCache)
        {
            _audioMinDistance = 0f;
            _audioMaxDistance = 100f;

            _volumeCurve = new AnimationCurve(new Keyframe[]
            {
                 new Keyframe(0f, 1f),
                 new Keyframe(1f, 1f)
            });
        }

        protected abstract float GetSpacing(Transform previousModel, Transform currentModel);

        protected abstract bool MoveForwardCondition();

        protected abstract bool MoveBackwardCondition();

        protected abstract void AdjustWheelForward(float dt);

        protected abstract void AdjustWheelBackward(float dt);

        protected abstract void AdjustModelPosition(Transform model, bool forward, float spacing);

        public sealed override void StartArcade(ArcadeConfiguration arcadeConfiguration)
        {
            Assert.IsNotNull(arcadeConfiguration);
            Assert.IsNotNull(arcadeConfiguration.CylArcadeProperties);

            _playerFpsControls.gameObject.SetActive(false);
            _playerCylControls.gameObject.SetActive(true);

            SetupPlayer(_playerCylControls, arcadeConfiguration.CylArcadeProperties.CameraSettings);

            _ = _coroutineHelper.StartCoroutine(SetupWorld(arcadeConfiguration));
        }

        public sealed override bool SetupVideo(Renderer screen, List<string> directories, List<string> namesToTry)
        {
            string videopath = _videoCache.Load(directories, namesToTry);
            if (string.IsNullOrEmpty(videopath))
            {
                return false;
            }

            screen.material.EnableEmissive();

            AudioSource audioSource = screen.gameObject.AddComponentIfNotFound<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.dopplerLevel = 0f;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = _audioMinDistance;
            audioSource.maxDistance = _audioMaxDistance;
            audioSource.volume = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, _volumeCurve);

            VideoPlayer videoPlayer = screen.gameObject.AddComponentIfNotFound<VideoPlayer>();
            videoPlayer.errorReceived -= OnVideoPlayerErrorReceived;
            videoPlayer.errorReceived += OnVideoPlayerErrorReceived;
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = true;
            videoPlayer.skipOnDrop = true;
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videopath;
            videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.controlledAudioTrackCount = 1;
            videoPlayer.targetMaterialProperty = MaterialUtils.SHADER_EMISSIVE_TEXTURE_NAME;
            videoPlayer.Stop();

            return true;
        }

        protected sealed override IEnumerator SetupWorld(ArcadeConfiguration arcadeConfiguration)
        {
            ArcadeLoaded = false;

            _cylArcadeProperties = arcadeConfiguration.CylArcadeProperties;

            _playerCylControls.SetVerticalLookLimits(-40f, 40f);
            _playerCylControls.SetHorizontalLookLimits(0f, 0f);

            RenderSettings renderSettings = arcadeConfiguration.RenderSettings;
            _ = _coroutineHelper.StartCoroutine(AddModelsToWorld(arcadeConfiguration.ArcadeModelList, _arcadeHierarchy.ArcadesNode, renderSettings, ARCADE_RESOURCES_DIRECTORY, ContentMatcher.GetNamesToTryForArcade));
            _ = _coroutineHelper.StartCoroutine(AddModelsToWorld(arcadeConfiguration.PropModelList, _arcadeHierarchy.PropsNode, renderSettings, PROP_RESOURCES_DIRECTORY, ContentMatcher.GetNamesToTryForProp));
            _ = _coroutineHelper.StartCoroutine(AddGameModelsToWorld(arcadeConfiguration.GameModelList, renderSettings, GAME_RESOURCES_DIRECTORY, ContentMatcher.GetNamesToTryForGame));

            while (!_gameModelsLoaded)
            {
                yield return null;
            }

            _centerTargetPosition = new Vector3(0f, 0f, _cylArcadeProperties.SelectedPositionZ);
            _sprockets            = Mathf.Clamp(_cylArcadeProperties.Sprockets, 1, _allGames.Count);
            int selectedSprocket  = Mathf.Clamp(_cylArcadeProperties.SelectedSprocket - 1, 0, _sprockets);
            int halfSprockets     = _sprockets % 2 != 0 ? _sprockets / 2 : _sprockets / 2 - 1;
            _selectionIndex       = halfSprockets - selectedSprocket;

            if (_cylArcadeProperties.InverseList)
            {
                _allGames.Reverse();
                _allGames.RotateRight(_selectionIndex + 1);
            }
            else
            {
                _allGames.RotateRight(_selectionIndex);
            }

            LateSetupWorld();

            SetupWheel();

            if (_selectionIndex >= 0 && _allGames.Count >= _selectionIndex)
            {
                CurrentGame = _allGames[_selectionIndex].GetComponent<ModelConfigurationComponent>();
            }
            else
            {
                CurrentGame = null;
            }

            ArcadeLoaded = true;
        }

        protected sealed override IEnumerator AddGameModelsToWorld(ModelConfiguration[] modelConfigurations, RenderSettings renderSettings, string resourceDirectory, ContentMatcher.GetNamesToTryDelegate getNamesToTry)
        {
            _gameModelsLoaded = false;

            _allGames.Clear();

            foreach (ModelConfiguration modelConfiguration in modelConfigurations)
            {
                EmulatorConfiguration emulator = _contentMatcher.GetEmulatorForConfiguration(modelConfiguration);
                List<string> namesToTry        = getNamesToTry(modelConfiguration, emulator);

                GameObject prefab = _gameObjectCache.Load(resourceDirectory, namesToTry);
                if (prefab == null)
                {
                    continue;
                }

                GameObject instantiatedModel = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, _arcadeHierarchy.GamesNode);
                instantiatedModel.name       = modelConfiguration.Id;
                instantiatedModel.transform.localScale = modelConfiguration.Scale;
                instantiatedModel.transform.SetLayersRecursively(_arcadeHierarchy.GamesNode.gameObject.layer);

                instantiatedModel.AddComponent<ModelConfigurationComponent>()
                                 .FromModelConfiguration(modelConfiguration);

                _allGames.Add(instantiatedModel.transform);

                if (instantiatedModel.TryGetComponent(out Rigidbody rigidbody))
                {
                    rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    rigidbody.interpolation          = RigidbodyInterpolation.None;
                    rigidbody.useGravity             = false;
                    rigidbody.isKinematic            = true;
                }

                // Look for artworks only in play mode / runtime
                if (Application.isPlaying)
                {
                    _marqueeNodeController.Setup(instantiatedModel, modelConfiguration, emulator, renderSettings.MarqueeIntensity);
                    _screenNodeController.Setup(instantiatedModel, modelConfiguration, emulator, GetScreenIntensity(modelConfiguration, renderSettings));
                    _genericNodeController.Setup(instantiatedModel, modelConfiguration, emulator, 1f);
                }

                instantiatedModel.SetActive(false);

                // Instantiate asynchronously only when loaded from the editor menu / auto reload
                if (Application.isPlaying)
                {
                    yield return null;
                }
            }

            _gameModelsLoaded = true;
        }

        protected override void LateSetupWorld()
        {
            base.LateSetupWorld();

            _playerCylControls.MouseLookEnabled = _cylArcadeProperties.MouseLook;
        }

        protected sealed override IEnumerator CoNavigateForward(float dt)
        {
            _animating = true;

            _targetSelection = _allGames[_selectionIndex + 1];

            ParentGamesToAnchor();

            while (MoveForwardCondition())
            {
                AdjustWheelForward(dt);
                yield return null;
            }

            ResetGamesParent();

            _allGames.RotateLeft();

            UpdateWheel();

            _animating = false;
        }

        protected sealed override IEnumerator CoNavigateBackward(float dt)
        {
            _animating = true;

            _targetSelection = _allGames[_selectionIndex - 1];

            ParentGamesToAnchor();

            while (MoveBackwardCondition())
            {
                AdjustWheelBackward(dt);
                yield return null;
            }

            ResetGamesParent();

            _allGames.RotateRight();

            UpdateWheel();

            _animating = false;
        }

        protected void SetupWheel()
        {
            if (_allGames.Count < 1)
            {
                return;
            }

            Transform firstModel = _allGames[_selectionIndex];
            firstModel.gameObject.SetActive(true);
            firstModel.SetPositionAndRotation(_centerTargetPosition, Quaternion.Euler(_cylArcadeProperties.SprocketRotation));

            for (int i = _selectionIndex + 1; i < _sprockets; ++i)
            {
                Transform previousModel = _allGames[i - 1];

                Transform currentModel = _allGames[i];
                currentModel.gameObject.SetActive(true);
                currentModel.SetPositionAndRotation(previousModel.localPosition, previousModel.localRotation);
                float spacing = GetSpacing(previousModel, currentModel);
                AdjustModelPosition(currentModel, true, spacing);
            }

            for (int i = _selectionIndex - 1; i >= 0; --i)
            {
                Transform previousModel = _allGames[i + 1];

                Transform currentModel = _allGames[i];
                currentModel.gameObject.SetActive(true);
                currentModel.SetPositionAndRotation(previousModel.localPosition, previousModel.localRotation);
                float spacing = GetSpacing(previousModel, currentModel);
                AdjustModelPosition(currentModel, false, spacing);
            }

            foreach (Transform model in _allGames.Skip(_sprockets))
            {
                model.gameObject.SetActive(false);
                model.localPosition = Vector3.zero;
            }
        }

        protected void UpdateWheel()
        {
            if (_allGames.Count < 1)
            {
                return;
            }

            Transform previousModel = _allGames[_sprockets - 2];
            Transform newModel      = _allGames[_sprockets - 1];
            newModel.gameObject.SetActive(true);
            newModel.SetPositionAndRotation(previousModel.localPosition, previousModel.localRotation);
            float spacing = GetSpacing(previousModel, newModel);
            AdjustModelPosition(newModel, true, spacing);

            previousModel = _allGames[1];
            newModel      = _allGames[0];
            newModel.gameObject.SetActive(true);
            newModel.SetPositionAndRotation(previousModel.localPosition, previousModel.localRotation);
            spacing = GetSpacing(previousModel, newModel);
            AdjustModelPosition(newModel, false, spacing);

            foreach (Transform model in _allGames.Skip(_sprockets))
            {
                model.gameObject.SetActive(false);
                model.localPosition = Vector3.zero;
            }

            CurrentGame = _allGames[_selectionIndex].GetComponent<ModelConfigurationComponent>();
        }

        protected float GetHorizontalSpacing(Transform previousModel, Transform currentModel) => previousModel.GetHalfWidth() + currentModel.GetHalfWidth() + _cylArcadeProperties.ModelSpacing;

        protected float GetVerticalSpacing(Transform previousModel, Transform currentModel) => previousModel.GetHalfHeight() + currentModel.GetHalfHeight() + _cylArcadeProperties.ModelSpacing;

        protected void ParentGamesToAnchor()
        {
            foreach (Transform game in _allGames.Take(_sprockets))
            {
                game.SetParent(TransformAnchor);
            }
        }

        protected void ResetGamesParent()
        {
            foreach (Transform game in _allGames.Take(_sprockets))
            {
                game.SetParent(_arcadeHierarchy.GamesNode);
            }
        }
    }
}
