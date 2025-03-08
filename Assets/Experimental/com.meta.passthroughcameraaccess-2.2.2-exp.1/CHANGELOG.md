# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.2.2-exp.1] - 2025-01-31

### Added

* Added PassthroughCameraManager class which improves flow for managing the lifecycle of WebCamTexture objects
* Added PassthroughCameraUtils class which is responsible for extracting more camera metadata like camera intrinsics / extrinsics from the device
* Added sample CameraToWorld which demonstrates how to translate 2D projections into the 3D world space
* Renamed BrightnessLevel sample in BrightnessEstimation

### Changed

* Bumped version to 2.2.2-exp.1
* Brightness sample renamed to Lighting sample
* Splitted the MetaQuestCameraManager into separate classes base on each functionality
* Changed how to access the CPU and GPU texture from WebCamTexture object, no more callback to get the texture.
* Updated the samples to show how to access the CPU and GPU data.
* Removed Shader sample
* Changed the namespace to Meta.XR.PassthroughCamera;
* Changed the MetaQuestCameraManager prefab name to PassthroughCameraApiPrefab

### Fixed

* Getting Horizon OS version for custom OS version

## [1.2.2-exp.1] - 2025-01-10

### Changed

* Bumped version to 1.2.2-exp.1.

### Fixed

* Get Horizon OS version to check the different camera permissions.
* Fixed android camera permission name.

## [1.2.1-exp.1] - 2024-12-16

### Added

* Added Editor popup to automatically add  (or not) the Horizon OS passthrough camera access permission in AndroidManifest.xml.

### Changed

* Bumped version to 1.2.1-exp.1.
* Added permission checks in Brightness sample.
* Removed WebCamTexture object is not updated log because the camera is not updated every frame.

### Fixed

* Camera permissions flow when horizonOs.permission.Headset_Camera is not accepted.
* AndroidManifest checker before building the apk. IF permission is not in the AndroidManifest the build can't be made.

## [1.2.0-exp.1] - 2024-12-08

### Added

* Added Horizon OS passthrough camera access permission in AndroidManifest.xml and PCA init checkers.

### Changed

* Bumped version to 1.2.0-exp.1.
* Improved samples description

## [1.1.1-exp.1] - 2024-12-07

### Unreleased

* Adding oculus passthrough camera access permission

### Changed

* Bumped version to 1.1.1-exp.1.
* Added selector for quality selection and eye selection in the MetaQuestCameraManager class.
* Minor changes in the documentation.
* Bumped MRUK version from 69.0.0 to 71.0.0.

## [1.1.0-exp.1] - 2024-11-07

### Unreleased

* Adding oculus passthrough camera access permission

### Added

* Added Debug Information Level in the MetaQuestCameraManager class.

### Changed

* Bumped version to 1.1.0-exp.1.
* Changed the package name to Meta Passthrough Camera Access.
* Changed namespace name to Meta.PasshtroughCameraAccess.
* Changed all class names and references for the new namespace (Meta.PasshtroughCameraAccess)
* Updated all samples and reorganaized assets to not lose the references.
* Updated MRUK dependecy version to 69.0.1 (a different version of MRUK could cause crashes under Unity 6)

### Fixed

* Initialize WebCamTexture Object from Editor play mode using local device camera.
* _texture2D variable created after initialize the WebCamTexture to get the real size.
* Fix for the Android Camera Permission flow when other permission popup appears first, on builds with Unity 2022.3.50f1.

## [1.0.7-exp.1] - 2024-11-05

### Unreleased

* Fix for the Android Camera Permission flow when other permission popup appears first, on builds with Unity 2022.3.50f1. (Waiting for proper passthrough api permission)
* Unity 6 (6000.0.25 LTS) crash with WebCamtexture Object play function.

### Added

* StopAndDestroy() function to remove all callbacks, stop and destroy WebCamTexture object before close the app.

### Changed

* Bumped versiont to 1.0.7-exp.1
* OVRManager callback are assigned once the WebCamTexture object is ready.
* Changed CheckWebCamTextureObject() to IsWebCameTextureReady().
* Changed how to get the CPU Texture using SetPixels instead of Graphics.copy.

### Fixed

* IsWebCamTextureReady() function issue with _webCamTextureReady value.
* Small orthographic fixes and improved comments.
* Samples folder path for TGZ package.

## [1.0.6-exp.1] - 2024-11-04

### Unreleased

* Fix for the Android Camera Permission flow when other permission popup appears first, on builds with Unity 2022.3.50f1. (Waiting for proper passthrough api permission)

### Added

* MetaQuestFovUtils and MetaQuestScreenCaptureUtils from David Geiser.
* Added new callback OnWebCamTextureReady whe nthe WebCamTexture obejct is finally ready and playing.

### Changed

* Bumped versiont to 1.0.6-exp.1
* Updated CameraViewer sample to use OnWebCamTextureReady callback.

### Fixed

* WebCamTexture Object initialization from a MAC OS build.

## [1.0.5-exp.1] - 2024-10-31

### Added

* Oculus.VR assembly reference
* OVRManager Input adquired and input lost events to stop and play WebCamTexture Object.
* Added new callbacks OnCPUTextureChange and OnGPUTextureChange with timestamp params.
* Added more Debug.Log with the perfix "PCA".

### Changed

* Improved MetaQuestCameraManager class.
* Improved WebCamTexture application focus events.
* Imporved copy WebCamTexture object to texture 2D using Graphics.CopyTexture.
* OntextureChange change to OnCPUTextureChange.
* Changed quality range values starting from 0 instead of 1.
* Meta copyright text updated.

### Fixed

* Brightness estimation texts.

## [1.0.4-exp.1] - 2024-10-29

### Added

* Public methods to stop and play WebCamTexture object from OVRManager events.

### Changed

* Improved brightness estimation sample.
* Improved WebCamTexture access flow.
* Improved WebCamTexture application focus events.

## [1.0.3-exp.1] - 2024-10-25

### Added

* Added shader graph includes to avoid add the full shadergraph package
* Added High resolution option for the camera data.

### Changed

* Converted Shader Water from shder graph to regualr shader.
* Adjusted the cemra quality per sample.

### Removed

* Removed shader graph as a dependecy

## [1.0.2-exp.1] - 2024-10-21

### Fixed

* Fixed initilize camera in Editor Play Mode.

### Added

* Water shader sample to show how to use camera image as a "fake refelection"
* Shadergpah as a dependency.
* Eye selection in MetaQuestCameraManager component
* Updated package dependencies

### Changed

* Camera Quality slider. Now we use the resolutions provided by the Camera.
* Improved Room debugger script to not requires debugger ui text and start in dark state.

## [1.0.1-exp.1] - 2024-10-21

### Added

* Callbacks for permisssion fails nad success events in MetaQuestCameraManager class.

### Changed

* Reorganaized package structure to separate camera manager assets from samples assets.
* Improved CameraViewer sample and BrightnessLevel sample (naming and added some comments)

### Removed

* Deleted AndroidManifest.xml from UPM package.

## [1.0.0-exp.1] - 2024-10-21

### Fixed

* Fixed permission flow for Unity 6 (6000.23)

### Added

* Camera initialization in Editor  Play mode.
* Added changelog file
* Added manifestfile with android camera permission

### Changed

* Moved asssembly definition from root to Runtime folder.
* Changed version to add experimental version number for this package.

## [0.0.1] - 2024-10-18

First version of com.meta.passthoughapi package to easy manage Quest 3 raw image data via WebCamtexture.

### Added

* Camera Manager prefab with Meta Quest Camera manager class.
* Samples: camera access sample and brightness sample
