UPU - .unityPackage unpacker GUI and CLI Tool
===

This little Windows tool helps you to unpack Unity Packages from [Unity 3D](http://www.unity.com/ "Unity 3D"), and avoids that you have to open the Unity Editor in order to access the precious asset files bundled within the package.

It also can add a context menu handler for the Windows Explorer which makes extraction of the files a lot easier.

##UPU GUI


What's working already?
---

1. You can extract the whole unity package to a defined directory, or the same directory where the package resides in.
2. Add/Remove Windows Explorer context menu handlers for *.unitypackage files.

Usage
---
UpuGui.exe [options]

**Options:**<br />
-i, --input: unitypackage input file.<br />
-o, --output: The output path of the extracted unitypackage.<br />
-r, --register: Register context menu handler.<br />
-u, --unregister: Unregister context menu handler.<br />

Works on
---

- Windows

Todos
---

1. optimized decompiled code.

Download
---
Latest Version: https://github.com/Myrkie/UpuGui/releases

Search Engine Keywords:
unity3d, unitypackage, unpack, extract, deflate, assets, UPUGui
