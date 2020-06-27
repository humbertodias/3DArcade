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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Arcade_r
{
    public abstract class ArcadeCylController : ArcadeController
    {
        protected readonly List<Transform> _allGames;

        protected CylArcadeProperties _cylArcadeProperties;

        protected int _sprockets;
        protected int _selectionIndex;

        protected Vector3 _centerTargetPosition;

        public ArcadeCylController(ArcadeHierarchy arcadeHierarchy,
                                   PlayerFpsControls playerFpsControls,
                                   PlayerCylControls playerCylControls,
                                   Database<EmulatorConfiguration> emulatorDatabase,
                                   AssetCache<GameObject> gameObjectCache,
                                   AssetCache<Texture> textureCache,
                                   AssetCache<string> videoCache)
        : base(arcadeHierarchy, playerFpsControls, playerCylControls, emulatorDatabase, gameObjectCache, textureCache, videoCache)
        {
            _allGames = new List<Transform>();
        }

        protected abstract void SetupWheel();

        public override bool StartArcade(ArcadeConfiguration arcadeConfiguration)
        {
            Assert.IsNotNull(arcadeConfiguration);
            Assert.IsNotNull(arcadeConfiguration.CylArcadeProperties);

            _playerFpsControls.gameObject.SetActive(false);
            _playerCylControls.gameObject.SetActive(true);

            SetupPlayer(_playerCylControls, arcadeConfiguration.CylArcadeProperties.CameraSettings);

            SetupWorld(arcadeConfiguration);

            return true;
        }

        protected override void SetupWorld(ArcadeConfiguration arcadeConfiguration)
        {
            _cylArcadeProperties = arcadeConfiguration.CylArcadeProperties;

            _playerCylControls.SetVerticalLookLimits(-40f, 40f);
            _playerCylControls.SetHorizontalLookLimits(0f, 0f);

            RenderSettings renderSettings = arcadeConfiguration.RenderSettings;
            AddModelsToWorld(arcadeConfiguration.ArcadeModelList, _arcadeHierarchy.ArcadesNode, renderSettings, ARCADE_RESOURCES_DIRECTORY, ContentMatcher.GetNamesToTryForArcade);
            AddModelsToWorld(arcadeConfiguration.PropModelList, _arcadeHierarchy.PropsNode, renderSettings, PROP_RESOURCES_DIRECTORY, ContentMatcher.GetNamesToTryForProp);

            AddGameModelsToWorld(arcadeConfiguration.GameModelList, renderSettings, GAME_RESOURCES_DIRECTORY, ContentMatcher.GetNamesToTryForGame);

            _centerTargetPosition = new Vector3(0f, 0f, _cylArcadeProperties.WheelRadius);
            _sprockets            = Mathf.Clamp(_cylArcadeProperties.Sprockets, 1, _allGames.Count);
            int selectedSprocket  = Mathf.Clamp(_cylArcadeProperties.SelectedSprocket - 1, 0, _sprockets);
            int halfSprockets     = _sprockets % 2 != 0 ? _sprockets / 2 : _sprockets / 2 - 1;
            _selectionIndex       = halfSprockets - selectedSprocket;
            _allGames.RotateRight(_selectionIndex);

            SetupWheel();
        }

        private void AddGameModelsToWorld(ModelConfiguration[] modelConfigurations, RenderSettings renderSettings, string resourceDirectory, ContentMatcher.GetNamesToTryDelegate getNamesToTry)
        {
            if (modelConfigurations == null)
            {
                return;
            }

            _allGames.Clear();

            foreach (ModelConfiguration modelConfiguration in modelConfigurations)
            {
                EmulatorConfiguration emulator = _contentMatcher.GetEmulatorForConfiguration(modelConfiguration);
                List<string> namesToTry        = getNamesToTry(modelConfiguration, emulator);

                GameObject prefab = _gameObjectCache.Load(resourceDirectory, namesToTry);
                Assert.IsNotNull(prefab, "prefab is null!");

                GameObject instantiatedModel = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, _arcadeHierarchy.GamesNode);
                instantiatedModel.name = modelConfiguration.Id;
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

                // Only look for artworks in play mode / at runtime
                if (Application.isPlaying)
                {
                    SetupMarqueeNode(instantiatedModel, modelConfiguration, emulator, renderSettings.MarqueeIntensity);
                    SetupScreenNode(instantiatedModel, modelConfiguration, emulator, GetScreenIntensity(modelConfiguration, renderSettings));
                    SetupGenericNode(instantiatedModel, modelConfiguration, emulator);
                }

                instantiatedModel.SetActive(false);
            }
        }
    }
}