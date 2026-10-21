# Third-party notices

Glasspane is released under the [MIT License](LICENSE). It depends on, and an app built with it redistributes:

| Component | License | Copyright |
|---|---|---|
| Vortice.Direct3D11, Vortice.DXGI, Vortice.Direct2D1, Vortice.DirectComposition (and their Vortice.DirectX / Vortice.Mathematics dependencies) | MIT | Copyright (c) Amer Koleci and Contributors |
| SharpGen.Runtime, SharpGen.Runtime.COM | MIT | (c) 2010-2017 Alexandre Mutel, 2017-2023 Jeremy Koritzinsky, 2023-2024 Amer Koleci |
| .NET runtime and libraries (statically compiled in when you publish with NativeAOT) | MIT | Copyright (c) .NET Foundation and Contributors; further notices ship with the .NET SDK as `ThirdPartyNotices.txt` |

The MIT License requires that the copyright notice and permission notice be included in copies or substantial portions of the software. If you ship a binary that contains these components, include this file (or equivalent notices) with it.

Windows itself (Direct3D, Direct2D, DirectWrite, DWM) is used through the operating system and is not redistributed. The repository contains no third-party fonts or images; the icons are drawn by the author.
