using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;


namespace VRMShaders
{
    public delegate Task<Texture> GetTextureAsyncFunc(TextureDescriptor texDesc, IAwaitCaller awaitCaller);

    public class MaterialFactory : IResponsibilityForDestroyObjects
    {
        private readonly IReadOnlyDictionary<SubAssetKey, Material> m_externalMap;

        public MaterialFactory(IReadOnlyDictionary<SubAssetKey, Material> externalMaterialMap)
        {
            m_externalMap = externalMaterialMap;
        }

        public struct MaterialLoadInfo
        {
            public SubAssetKey Key;
            public readonly Material Asset;
            public readonly bool UseExternal;

            public bool IsSubAsset => !UseExternal;

            public MaterialLoadInfo(SubAssetKey key, Material asset, bool useExternal)
            {
                Key = key;
                Asset = asset;
                UseExternal = useExternal;
            }
        }

        List<MaterialLoadInfo> m_materials = new List<MaterialLoadInfo>();
        public IReadOnlyList<MaterialLoadInfo> Materials => m_materials;
        void Remove(Material material)
        {
            var index = m_materials.FindIndex(x => x.Asset == material);
            if (index >= 0)
            {
                m_materials.RemoveAt(index);

            }
        }

        public void Dispose()
        {
            foreach (var x in m_materials)
            {
                if (!x.UseExternal)
                {
                    // 外部の '.asset' からロードしていない
                    UnityObjectDestroyer.DestroyRuntimeOrEditor(x.Asset);
                }
            }
        }

        /// <summary>
        /// 所有権(Dispose権)を移譲する
        ///
        /// 所有権を移動する関数。
        ///
        /// * 所有権が移動する。return true => ImporterContext.Dispose の対象から外れる
        /// * 所有権が移動しない。return false => Importer.Context.Dispose でDestroyされる
        ///
        /// </summary>
        /// <param name="take"></param>
        public void TransferOwnership(TakeResponsibilityForDestroyObjectFunc take)
        {
            foreach (var x in m_materials.ToArray())
            {
                if (!x.UseExternal)
                {
                    // 外部の '.asset' からロードしていない
                    take(x.Key, x.Asset);
                    m_materials.Remove(x);
                }
            }
        }

        public Material GetMaterial(int index)
        {
            if (index < 0) return null;
            if (index >= m_materials.Count) return null;
            return m_materials[index].Asset;
        }

        public async Task<Material> LoadAsync(MaterialDescriptor matDesc, GetTextureAsyncFunc getTexture, IAwaitCaller awaitCaller)
        {
            if (m_externalMap.TryGetValue(matDesc.SubAssetKey, out Material material))
            {
                m_materials.Add(new MaterialLoadInfo(matDesc.SubAssetKey, material, true));
                return material;
            }

            if (getTexture == null)
            {
                getTexture = (x, y) => Task.FromResult<Texture>(null);
            }

            if (matDesc.Shader == null)
            {
                throw new ArgumentNullException(nameof(matDesc.Shader));
            }

            bool hasShadeMap = false;
            bool useEmission = false;
            Texture baseMap = null;
            Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
            foreach (var kv in matDesc.TextureSlots)
            {
                var texture = await getTexture(kv.Value, awaitCaller);
                if (texture != null)
                {
                    if (this.TryGetMaterialParamKey(kv.Key, out string key, out float scale))
                    {
                        if ("_BaseMap" == key)
                        {
                            baseMap = texture;
                        }
                        if ("_ShadeMap" == key)
                        {
                            hasShadeMap = true;
                        }
                        if ("_EmissionMap" == key)
                        {
                            useEmission = true;
                        }
                        textures[key] = texture;
                    }
                    else
                    {
                        MonoBehaviour.Destroy(texture);
                    }
                }
                else
                {
                    if (this.TryGetMaterialParamKey(kv.Key, out string key, out float scale))
                    {
                        if ("_EmissionMap" == key)
                        {
                            useEmission = true;
                        }
                    }
                }
            }

            // Set URP Shader Name
            var shaderName = "SimpleURPToonLitOutlineExample";
            if (matDesc.RenderQueue.HasValue)
            {
                if (!useEmission && (int)UnityEngine.Rendering.RenderQueue.Transparent <= matDesc.RenderQueue.Value)
                {
                    shaderName = "Shader Graphs/SimpleURPTransparent";
                }
            }

            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new Exception($"shader: {shaderName} not found");
            }
            material = new Material(shader);
            material.name = matDesc.SubAssetKey.Name;
            foreach (var keyValue in textures)
            {
                material.SetTexture(keyValue.Key, keyValue.Value);
            }

            foreach (var kv in matDesc.Vectors)
            {
                if (this.TryGetMaterialParamKey(kv.Key, out string key, out float scale))
                {
                    if (matDesc.TextureSlots.ContainsKey(kv.Key))
                    {
                        // texture offset & scale
                        material.SetTextureOffset(key, new Vector2(kv.Value[0] * scale, kv.Value[1] * scale));
                        material.SetTextureScale(key, new Vector2(kv.Value[2] * scale, kv.Value[3] * scale));
                    }
                    else
                    {
                        // vector4
                        var v = new Vector4(kv.Value[0] * scale, kv.Value[1] * scale, kv.Value[2] * scale, kv.Value[3] * scale);
                        material.SetVector(key, v);
                    }
                }
            }

            bool useOutline = false;
            foreach (var kv in matDesc.FloatValues)
            {
                if (this.TryGetMaterialParamKey(kv.Key, out string key, out float scale))
                {
                    if ("_OutlineWidthMode" == key)
                    {
                        useOutline = 0.0f != kv.Value;
                    }
                    material.SetFloat(key, kv.Value * scale);
                }
            }

            if (useEmission)
            {
                material.SetFloat("_UseEmission", 1);
            }
            if (!useOutline)
            {
                material.SetFloat("_OutlineWidth", 0.0001f);
            }

            if (!hasShadeMap && null != baseMap)
            {
                material.SetTexture("_ShadeMap", baseMap);
            }

            foreach (var kv in matDesc.Colors)
            {
                if (this.TryGetMaterialParamKey(kv.Key, out string key, out float scale))
                {
                    var color = kv.Value * scale;
                    material.SetColor(key, color);
                    if ("_BaseColor" == key && !hasShadeMap)
                    {
                        material.SetColor("_ShadeColor", new Color(Mathf.Clamp(color[0] - 0.1f, 0, 1f), Mathf.Clamp(color[1] - 0.2f, 0, 1f), Mathf.Clamp(color[2] - 0.2f, 0, 1f), color[3]));
                    }
                }
            }

            if (matDesc.RenderQueue.HasValue)
            {
                material.renderQueue = matDesc.RenderQueue.Value;
            }

            m_materials.Add(new MaterialLoadInfo(matDesc.SubAssetKey, material, false));

            return material;
        }

        public static void SetTextureOffsetAndScale(Material material, string propertyName, Vector2 offset, Vector2 scale)
        {
            material.SetTextureOffset(propertyName, offset);
            material.SetTextureScale(propertyName, scale);
        }

        /// <summary>
        /// Toon表現用マテリアルにマッチするパラメータキーへの変換
        /// </summary>
        /// <param name="keyIn">入力のキー</param>
        /// <param name="keyOut">変換後のキー</param>
        /// <param name="scaleValue">値の倍率</param>
        /// <returns>変換可能</returns>
        bool TryGetMaterialParamKey(string keyIn, out string keyOut, out float scaleValue)
        {
            keyOut = keyIn;
            scaleValue = 1.0f;
            bool validKey = true;
            switch (keyIn)
            {
                case "_Color":
                    keyOut = "_BaseColor";
                    break;
                case "_MainTex":
                    keyOut = "_BaseMap";
                    break;
                case "_ShadeColor":
                    keyOut = "_ShadeColor";
                    break;
                case "_ShadeTexture":
                    keyOut = "_ShadeMap";
                    break;
                case "_ShadeTex":
                    keyOut = "_ShadeMap";
                    break;
                case "_EmissionColor":
                    keyOut = "_EmissionColor";
                    break;
                case "_EmissionMap":
                    keyOut = "_EmissionMap";
                    break;
                case "_OutlineColor":
                    keyOut = "_OutlineColor";
                    break;
                case "_OutlineWidth":
                    keyOut = "_OutlineWidth";
                    scaleValue = 0.005f;
                    break;
                case "_OutlineWidthMode":
                    keyOut = "_OutlineWidthMode";
                    break;
                case "_CutOff":
                    keyOut = "_Cutoff";
                    break;
                case "_CullMode":
                    keyOut = "_Cull";
                    break;
                default:
                    validKey = false;
                    break;
            }
            return validKey;
        }
    }
}
