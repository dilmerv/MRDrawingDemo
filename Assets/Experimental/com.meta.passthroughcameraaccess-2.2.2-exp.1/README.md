# Meta Passthrough Camera Access Unity Package

## Description

This Unity package enables access to raw camera data using **WebCamTexture** object for Quest 3 devices

## Package Setup

- Create a new Unity Project using the 3D Template
- Switch Platform to Android in Build Settings
- Copy this package tarball into the project's Packages folder
- Add the package explicitly to the project by opening Package Manager, clicking on the '+' sign in the left top corner, choosing 'Add project from tarball', and selecting this tarball package file
- Wait until the package and all its dependencies are added, restart Unity Editor if it requests so
- In Project Settings install XR Plug-in Management
- There enable the Oculus Plug-in Provider for Android platform
- Go to Project Settings -> Oculus Project Setup tools and apply all the required and suggested fixes
- In Package Manager find current project, open Samples tab, and import all the samples
- Go to Build Project, add the CameraViewer scene and build an APK to your Quest 3 headset.
- Install your app and run from the headset.
