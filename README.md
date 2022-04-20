# SimpleURPToonLitOutlineExample
![image](https://user-images.githubusercontent.com/20289559/151178653-4e27ddb3-24cb-4ecf-b207-35709d203b23.png)
Test VRM from https://codealdnoah.booth.pm/

![castAndRecieveShadow](https://user-images.githubusercontent.com/20289559/164125018-c7bc1ab7-97ba-4a91-9c14-842da64de09b.jpg)  
Cast and recieve shadows

<!-- ![image](https://user-images.githubusercontent.com/20289559/151182615-79250fcc-526f-4a03-851e-5d71515ea968.png) -->

## What is this ?
Unity URP (Universal Render Pipeline) Toon Lit Shader for UniVRM Runtime Load.

## Environment
Unity 2021.2.8f1 with URP or later.

## How to use

1. [Download](../../releases) .unitypackage.
2. Import the .unitypackage with [UniVRM](https://github.com/vrm-c/UniVRM).
3. Replace "\Assets\VRMShaders\GLTF\IO\Runtime\Material\Importer\MaterialFactory.cs"(UniVRM) with "\Assets\SimplestarGame\SimpleURPToonLitOutlineExample\Scripts\MaterialFactory.cs"(This Package).

## Base Code
The base shader code is [Unity URP Simplified Toon Lit Shader Example (for you to learn writing custom lit shader in URP)](https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample)

## Unity-Technologies Custom Lighting Shader Graph
This package uses [ShaderGraph-Custom-Lighting](https://github.com/Unity-Technologies/ShaderGraph-Custom-Lighting)
