using System;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;

namespace MinerEx
{
    [BepInPlugin("sorayuki.dsp.plugins.MinerEx", "MinerEx", "1.0")]
    public class MinerEx : BaseUnityPlugin
    {
        internal static ManualLogSource sLog;
        static Harmony _harmony;
        static readonly int[] _tmp_ids = new int[1024];

        private void Awake()
        {
            sLog = Logger;
            _harmony = Harmony.CreateAndPatchAll(typeof(MinerEx));
            sLog.LogInfo("Plugin MinerEx Loaded.");
        }

        //[HarmonyPatch(typeof(BuildTool_BlueprintPaste), "CheckBuildConditions")]
        [HarmonyPatch(typeof(BuildTool_Click), "CheckBuildConditions")]
        [HarmonyPostfix]
        public static void MoreVeins(ref bool __result, BuildTool_Click __instance)
        {
            if (__instance.buildPreviews.Count == 0)
            {
                return;
            }
            for (int i = 0; i < __instance.buildPreviews.Count; i++)
            {
                BuildPreview buildPreview = __instance.buildPreviews[i];
                if (buildPreview.condition == EBuildCondition.Ok)
                {
                    Vector3 vector = buildPreview.lpos;
                    Pose pose = new Pose(buildPreview.lpos, buildPreview.lrot);
                    Vector3 forward = pose.forward;
                    Vector3 up = pose.up;
                    if (vector.sqrMagnitude < 1f)
                    {
                        buildPreview.condition = EBuildCondition.Failure;
                    }
                    else
                    {
                        if (buildPreview.desc.veinMiner)
                        {
                            Array.Clear(_tmp_ids, 0, _tmp_ids.Length);
                            Vector3 vector2 = vector + forward * -1.2f;
                            Vector3 rhs = -forward;
                            Vector3 vector3 = up;
                            int veinsInAreaNonAlloc = __instance.actionBuild.nearcdLogic.GetVeinsInAreaNonAlloc(vector2, 12f * 3, _tmp_ids);
                            PrebuildData prebuildData = default;
                            prebuildData.InitParametersArray(veinsInAreaNonAlloc);
                            VeinData[] veinPool = __instance.factory.veinPool;
                            int paramCount = 0;
                            for (int j = 0; j < veinsInAreaNonAlloc; j++)
                            {
                                if (_tmp_ids[j] != 0 && veinPool[_tmp_ids[j]].id == _tmp_ids[j])
                                {
                                    if (veinPool[_tmp_ids[j]].type != EVeinType.Oil)
                                    {
                                        Vector3 vector4 = veinPool[_tmp_ids[j]].pos - vector2;
                                        float num2 = Vector3.Dot(vector3, vector4);
                                        vector4 -= vector3 * num2;
                                        float sqrMagnitude = vector4.sqrMagnitude;
                                        float num3 = Vector3.Dot(vector4.normalized, rhs);
                                        if (sqrMagnitude <= 60.0625f * 9 && num3 >= -0.73f && Mathf.Abs(num2) <= 2f)
                                        {
                                            prebuildData.parameters[paramCount++] = _tmp_ids[j];
                                        }
                                    }
                                }
                                else
                                {
                                    Assert.CannotBeReached();
                                }
                            }
                            prebuildData.paramCount = paramCount;
                            prebuildData.ArrageParametersArray();
                            buildPreview.parameters = prebuildData.parameters;
                            buildPreview.paramCount = prebuildData.paramCount;
                            Array.Clear(_tmp_ids, 0, _tmp_ids.Length);
                            if (prebuildData.paramCount == 0)
                            {
                                buildPreview.condition = EBuildCondition.NeedResource;
                            }
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchAll(_harmony.Id);
        }
    }
}
