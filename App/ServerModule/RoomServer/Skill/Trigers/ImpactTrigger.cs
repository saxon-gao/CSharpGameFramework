﻿using System;
using System.Collections.Generic;
using ScriptRuntime;
using SkillSystem;

namespace GameFramework.Skill.Trigers
{
    /// <summary>
    /// impact(starttime[,centerx,centery,centerz,relativeToTarget]);
    /// </summary>
    public class ImpactTrigger : AbstractSkillTriger
    {
        public override ISkillTriger Clone()
        {
            ImpactTrigger copy = new ImpactTrigger();
            copy.m_StartTime = m_StartTime;
            copy.m_RelativeCenter = m_RelativeCenter;
            copy.m_RelativeToTarget = m_RelativeToTarget;
            copy.m_RealStartTime = m_RealStartTime;
            return copy;
        }

        public override void Reset()
        {
            m_RealStartTime = m_StartTime;
        }

        public override bool Execute(object sender, SkillInstance instance, long delta, long curSectionTime)
        {
            GfxSkillSenderInfo senderObj = sender as GfxSkillSenderInfo;
            if (null == senderObj) return false;
            Scene scene = senderObj.Scene;
            EntityInfo obj = senderObj.GfxObj;
            if (obj == null) {
                return false;
            }
            if (m_RealStartTime < 0) {
                m_RealStartTime = TriggerUtil.RefixStartTime((int)m_StartTime, instance.LocalVariables, senderObj.ConfigData);
            }
            if (curSectionTime < m_RealStartTime) {
                return true;
            }
            int targetType = scene.EntityController.GetTargetType(senderObj.ActorId, senderObj.ConfigData, senderObj.Seq);
            int impactId = 0;
            if (targetType == (int)SkillTargetType.Self)
                impactId = senderObj.ConfigData.impactToSelf;
            else
                impactId = senderObj.ConfigData.impactToTarget;
            int senderId;
            int targetId;
            scene.EntityController.CalcSenderAndTarget(senderObj, out senderId, out targetId);
            if (senderObj.ConfigData.aoeType != (int)SkillAoeType.Unknown) {
                float minDistSqr = float.MaxValue;
                TriggerUtil.AoeQuery(senderObj, instance, senderId, targetType, m_RelativeCenter, m_RelativeToTarget, (float distSqr, int objId) => {
                    if (distSqr < minDistSqr) {
                        minDistSqr = distSqr;
                        targetId = objId;
                    }
                    return true;
                });
            }
            Dictionary<string, object> args;
            TriggerUtil.CalcHitConfig(instance.LocalVariables, senderObj.ConfigData, out args);
            scene.EntityController.SendImpact(senderObj.ConfigData, senderObj.Seq, senderObj.ActorId, senderId, targetId, impactId, args);
            return false;
        }

        protected override void Load(Dsl.CallData callData, int dslSkillId)
        {
            int num = callData.GetParamNum();
            if (num >= 1) {
                m_StartTime = long.Parse(callData.GetParamId(0));
            }
            if (num >= 5) {
                m_RelativeCenter.X = float.Parse(callData.GetParamId(1));
                m_RelativeCenter.Y = float.Parse(callData.GetParamId(2));
                m_RelativeCenter.Z = float.Parse(callData.GetParamId(3));
                m_RelativeToTarget = callData.GetParamId(4) == "true";
            }
            m_RealStartTime = m_StartTime;
        }

        private Vector3 m_RelativeCenter = Vector3.Zero;
        private bool m_RelativeToTarget = false;

        private long m_RealStartTime = 0;
    }
    /// <summary>
    /// aoeimpact(start_time, center_x, center_y, center_z, relativeToTarget);
    /// </summary>
    internal class AoeImpactTriger : AbstractSkillTriger
    {
        public override ISkillTriger Clone()
        {
            AoeImpactTriger triger = new AoeImpactTriger();
            triger.m_StartTime = m_StartTime;
            triger.m_RelativeCenter = m_RelativeCenter;
            triger.m_RelativeToTarget = m_RelativeToTarget;
            triger.m_RealStartTime = m_RealStartTime;
            return triger;
        }
        public override void Reset()
        {
            m_RealStartTime = m_StartTime;
        }
        public override bool Execute(object sender, SkillInstance instance, long delta, long curSectionTime)
        {
            GfxSkillSenderInfo senderObj = sender as GfxSkillSenderInfo;
            if (null == senderObj) return false;
            Scene scene = senderObj.Scene;
            EntityInfo obj = senderObj.GfxObj;
            if (null == obj) {
                return false;
            }
            if (m_RealStartTime < 0) {
                m_RealStartTime = TriggerUtil.RefixStartTime((int)m_StartTime, instance.LocalVariables, senderObj.ConfigData);
            }
            if (curSectionTime >= m_RealStartTime) {
                int targetType = scene.EntityController.GetTargetType(senderObj.ActorId, senderObj.ConfigData, senderObj.Seq);
                int impactId = 0;
                if (targetType == (int)SkillTargetType.Self)
                    impactId = senderObj.ConfigData.impactToSelf;
                else
                    impactId = senderObj.ConfigData.impactToTarget;
                int senderId = 0;
                if (senderObj.ConfigData.type == (int)SkillOrImpactType.Skill) {
                    senderId = senderObj.ActorId;
                } else {
                    senderId = senderObj.TargetActorId;
                }
                int ct = 0;
                TriggerUtil.AoeQuery(senderObj, instance, senderId, targetType, m_RelativeCenter, m_RelativeToTarget, (float distSqr, int objId) => {
                    Dictionary<string, object> args;
                    TriggerUtil.CalcHitConfig(instance.LocalVariables, senderObj.ConfigData, out args);
                    scene.EntityController.SendImpact(senderObj.ConfigData, senderObj.Seq, senderObj.ActorId, senderId, objId, impactId, args);
                    ++ct;
                    if (senderObj.ConfigData.maxAoeTargetCount <= 0 || ct < senderObj.ConfigData.maxAoeTargetCount) {
                        return true;
                    } else {
                        return false;
                    }
                });
                return false;
            } else {
                return true;
            }
        }

        protected override void Load(Dsl.CallData callData, int dslSkillId)
        {
            int num = callData.GetParamNum();
            if (num >= 5) {
                m_StartTime = long.Parse(callData.GetParamId(0));
                m_RelativeCenter.X = float.Parse(callData.GetParamId(1));
                m_RelativeCenter.Y = float.Parse(callData.GetParamId(2));
                m_RelativeCenter.Z = float.Parse(callData.GetParamId(3));
                m_RelativeToTarget = callData.GetParamId(4) == "true";
            }
            m_RealStartTime = m_StartTime;
        }

        private Vector3 m_RelativeCenter = Vector3.Zero;
        private bool m_RelativeToTarget = false;

        private long m_RealStartTime = 0;
    }
    /// <summary>
    /// chainaoeimpact(start_time, center_x, center_y, center_z, relativeToTarget, duration, interval);
    /// </summary>
    internal class ChainAoeImpactTriger : AbstractSkillTriger
    {
        public override ISkillTriger Clone()
        {
            ChainAoeImpactTriger triger = new ChainAoeImpactTriger();
            triger.m_StartTime = m_StartTime;
            triger.m_RelativeCenter = m_RelativeCenter;
            triger.m_RelativeToTarget = m_RelativeToTarget;
            triger.m_DurationTime = m_DurationTime;
            triger.m_IntervalTime = m_IntervalTime;
            triger.m_RealStartTime = m_RealStartTime;
            return triger;
        }
        public override void Reset()
        {
            m_LastTime = 0;
            m_SortedTargets.Clear();
            m_Targets.Clear();
            m_CurTargetIndex = 0;
            m_SenderId = 0;
            m_ImpactId = 0;
            m_RealStartTime = m_StartTime;
        }
        public override bool Execute(object sender, SkillInstance instance, long delta, long curSectionTime)
        {
            GfxSkillSenderInfo senderObj = sender as GfxSkillSenderInfo;
            if (null == senderObj) return false;
            Scene scene = senderObj.Scene;
            EntityInfo obj = senderObj.GfxObj;
            if (obj == null) {
                return false;
            }
            if (m_RealStartTime < 0) {
                m_RealStartTime = TriggerUtil.RefixStartTime((int)m_StartTime, instance.LocalVariables, senderObj.ConfigData);
            }
            if (curSectionTime < m_RealStartTime) {
                return true;
            }
            if (curSectionTime > m_RealStartTime + m_DurationTime) {
                return false;
            }
            if (m_LastTime + m_IntervalTime < curSectionTime) {
                m_LastTime = curSectionTime;

                int ct = m_Targets.Count;
                if (ct <= 0) {
                    int targetType = scene.EntityController.GetTargetType(senderObj.ActorId, senderObj.ConfigData, senderObj.Seq);
                    if (targetType == (int)SkillTargetType.Self)
                        m_ImpactId = senderObj.ConfigData.impactToSelf;
                    else
                        m_ImpactId = senderObj.ConfigData.impactToTarget;
                    if (senderObj.ConfigData.type == (int)SkillOrImpactType.Skill) {
                        m_SenderId = senderObj.ActorId;
                    } else {
                        m_SenderId = senderObj.TargetActorId;
                    }
                    TriggerUtil.AoeQuery(senderObj, instance, m_SenderId, targetType, m_RelativeCenter, m_RelativeToTarget, (float distSqr, int objId) => {
                        m_SortedTargets.Add((int)(distSqr * c_MaxObjectId) * c_MaxObjectId + objId, objId);
                        return true;
                    });
                    var vals = m_SortedTargets.Values;
                    if (vals.Count > senderObj.ConfigData.maxAoeTargetCount) {
                        var enumerator = vals.GetEnumerator();
                        for (int ix = 0; ix < senderObj.ConfigData.maxAoeTargetCount; ++ix) {
                            enumerator.MoveNext();
                            m_Targets.Add(enumerator.Current);
                        }
                    } else {
                        m_Targets.AddRange(vals);
                    }
                    m_CurTargetIndex = 0;
                    ct = m_Targets.Count;
                }
                if (ct > 0 && m_CurTargetIndex < ct) {
                    Dictionary<string, object> args;
                    TriggerUtil.CalcHitConfig(instance.LocalVariables, senderObj.ConfigData, out args);
                    scene.EntityController.SendImpact(senderObj.ConfigData, senderObj.Seq, senderObj.ActorId, m_SenderId, m_Targets[m_CurTargetIndex], m_ImpactId, args);
                    ++m_CurTargetIndex;
                } else {
                    return false;
                }
            }
            return true;
        }

        protected override void Load(Dsl.CallData callData, int dslSkillId)
        {
            int num = callData.GetParamNum();
            if (num >= 7) {
                m_StartTime = long.Parse(callData.GetParamId(0));
                m_RelativeCenter.X = float.Parse(callData.GetParamId(1));
                m_RelativeCenter.Y = float.Parse(callData.GetParamId(2));
                m_RelativeCenter.Z = float.Parse(callData.GetParamId(3));
                m_RelativeToTarget = callData.GetParamId(4) == "true";
                m_DurationTime = long.Parse(callData.GetParamId(5));
                m_IntervalTime = long.Parse(callData.GetParamId(6));
            }
            m_RealStartTime = m_StartTime;
        }

        private Vector3 m_RelativeCenter = Vector3.Zero;
        private bool m_RelativeToTarget = false;
        private long m_DurationTime = 0;
        private long m_IntervalTime = 0;

        private long m_RealStartTime = 0;

        private long m_LastTime = 0;
        private int m_SenderId = 0;
        private int m_ImpactId = 0;
        private SortedDictionary<int, int> m_SortedTargets = new SortedDictionary<int, int>();
        private List<int> m_Targets = new List<int>();
        private int m_CurTargetIndex = 0;

        private const int c_MaxObjectId = 1000;
    }
    /// <summary>
    /// periodicallyimpact(starttime, center_x, center_y, center_z, relativeToTarget, duration, interval);
    /// </summary>
    public class PeriodicallyImpactTrigger : AbstractSkillTriger
    {
        public override ISkillTriger Clone()
        {
            PeriodicallyImpactTrigger copy = new PeriodicallyImpactTrigger();
            copy.m_StartTime = m_StartTime;
            copy.m_RelativeCenter = m_RelativeCenter;
            copy.m_RelativeToTarget = m_RelativeToTarget;
            copy.m_DurationTime = m_DurationTime;
            copy.m_IntervalTime = m_IntervalTime;
            copy.m_RealStartTime = m_RealStartTime;
            return copy;
        }

        public override void Reset()
        {
            m_LastTime = 0;
            m_RealStartTime = m_StartTime;
        }

        public override bool Execute(object sender, SkillInstance instance, long delta, long curSectionTime)
        {
            GfxSkillSenderInfo senderObj = sender as GfxSkillSenderInfo;
            if (null == senderObj) return false;
            Scene scene = senderObj.Scene;
            EntityInfo obj = senderObj.GfxObj;
            if (obj == null) {
                return false;
            }
            if (m_RealStartTime < 0) {
                m_RealStartTime = TriggerUtil.RefixStartTime((int)m_StartTime, instance.LocalVariables, senderObj.ConfigData);
            }
            long durationTime = m_DurationTime;
            long intervalTime = m_IntervalTime;
            if (durationTime <= 0) {
                durationTime = (long)senderObj.ConfigData.duration;
            }
            if (intervalTime <= 0) {
                intervalTime = (long)senderObj.ConfigData.interval;
            }
            if (curSectionTime < m_RealStartTime) {
                return true;
            }
            if (curSectionTime > m_RealStartTime + durationTime) {
                return false;
            }
            if (m_LastTime + intervalTime < curSectionTime) {
                m_LastTime = curSectionTime;

                int targetType = scene.EntityController.GetTargetType(senderObj.ActorId, senderObj.ConfigData, senderObj.Seq);
                int impactId = 0;
                if (targetType == (int)SkillTargetType.Self)
                    impactId = senderObj.ConfigData.impactToSelf;
                else
                    impactId = senderObj.ConfigData.impactToTarget;
                int senderId;
                int targetId;
                scene.EntityController.CalcSenderAndTarget(senderObj, out senderId, out targetId);
                if (senderObj.ConfigData.aoeType != (int)SkillAoeType.Unknown) {
                    float minDistSqr = float.MaxValue;
                    TriggerUtil.AoeQuery(senderObj, instance, senderId, targetType, m_RelativeCenter, m_RelativeToTarget, (float distSqr, int objId) => {
                        if (distSqr < minDistSqr) {
                            minDistSqr = distSqr;
                            targetId = objId;
                        }
                        return true;
                    });
                }
                Dictionary<string, object> args;
                TriggerUtil.CalcHitConfig(instance.LocalVariables, senderObj.ConfigData, out args);
                scene.EntityController.SendImpact(senderObj.ConfigData, senderObj.Seq, senderObj.ActorId, senderId, targetId, impactId, args);
            }
            return true;
        }

        protected override void Load(Dsl.CallData callData, int dslSkillId)
        {
            int num = callData.GetParamNum();
            if (num >= 7) {
                m_StartTime = long.Parse(callData.GetParamId(0));
                m_RelativeCenter.X = float.Parse(callData.GetParamId(1));
                m_RelativeCenter.Y = float.Parse(callData.GetParamId(2));
                m_RelativeCenter.Z = float.Parse(callData.GetParamId(3));
                m_RelativeToTarget = callData.GetParamId(4) == "true";
                m_DurationTime = long.Parse(callData.GetParamId(5));
                m_IntervalTime = long.Parse(callData.GetParamId(6));
            }
            m_RealStartTime = m_StartTime;
        }

        private Vector3 m_RelativeCenter = Vector3.Zero;
        private bool m_RelativeToTarget = false;
        private long m_DurationTime = 0;
        private long m_IntervalTime = 0;

        private long m_LastTime = 0;
        private long m_RealStartTime = 0;
    }
    /// <summary>
    /// periodicallyaoeimpact(start_time, center_x, center_y, center_z, relativeToTarget, duration, interval);
    /// </summary>
    internal class PeriodicallyAoeImpactTriger : AbstractSkillTriger
    {
        public override ISkillTriger Clone()
        {
            PeriodicallyAoeImpactTriger triger = new PeriodicallyAoeImpactTriger();
            triger.m_StartTime = m_StartTime;
            triger.m_RelativeCenter = m_RelativeCenter;
            triger.m_RelativeToTarget = m_RelativeToTarget;
            triger.m_DurationTime = m_DurationTime;
            triger.m_IntervalTime = m_IntervalTime;
            triger.m_RealStartTime = m_RealStartTime;
            return triger;
        }
        public override void Reset()
        {
            m_LastTime = 0;
            m_RealStartTime = m_StartTime;
        }
        public override bool Execute(object sender, SkillInstance instance, long delta, long curSectionTime)
        {
            GfxSkillSenderInfo senderObj = sender as GfxSkillSenderInfo;
            if (null == senderObj) return false;
            Scene scene = senderObj.Scene;
            EntityInfo obj = senderObj.GfxObj;
            if (obj == null) {
                return false;
            }
            if (m_RealStartTime < 0) {
                m_RealStartTime = TriggerUtil.RefixStartTime((int)m_StartTime, instance.LocalVariables, senderObj.ConfigData);
            }
            if (curSectionTime < m_RealStartTime) {
                return true;
            }
            if (curSectionTime > m_RealStartTime + m_DurationTime) {
                return false;
            }
            if (m_LastTime + m_IntervalTime < curSectionTime) {
                m_LastTime = curSectionTime;

                int targetType = scene.EntityController.GetTargetType(senderObj.ActorId, senderObj.ConfigData, senderObj.Seq);
                int impactId = 0;
                if (targetType == (int)SkillTargetType.Self)
                    impactId = senderObj.ConfigData.impactToSelf;
                else
                    impactId = senderObj.ConfigData.impactToTarget;
                int senderId = 0;
                if (senderObj.ConfigData.type == (int)SkillOrImpactType.Skill) {
                    senderId = senderObj.ActorId;
                } else {
                    senderId = senderObj.TargetActorId;
                }
                int ct = 0;
                TriggerUtil.AoeQuery(senderObj, instance, senderId, targetType, m_RelativeCenter, m_RelativeToTarget, (float distSqr, int objId) => {
                    Dictionary<string, object> args;
                    TriggerUtil.CalcHitConfig(instance.LocalVariables, senderObj.ConfigData, out args);
                    scene.EntityController.SendImpact(senderObj.ConfigData, senderObj.Seq, senderObj.ActorId, senderId, objId, impactId, args);
                    ++ct;
                    if (senderObj.ConfigData.maxAoeTargetCount <= 0 || ct < senderObj.ConfigData.maxAoeTargetCount) {
                        return true;
                    } else {
                        return false;
                    }
                });
            }
            return true;
        }

        protected override void Load(Dsl.CallData callData, int dslSkillId)
        {
            int num = callData.GetParamNum();
            if (num >= 6) {
                m_StartTime = long.Parse(callData.GetParamId(0));
                m_RelativeCenter.X = float.Parse(callData.GetParamId(1));
                m_RelativeCenter.Y = float.Parse(callData.GetParamId(2));
                m_RelativeCenter.Z = float.Parse(callData.GetParamId(3));
                m_RelativeToTarget = callData.GetParamId(4) == "true";
                m_DurationTime = long.Parse(callData.GetParamId(5));
                m_IntervalTime = long.Parse(callData.GetParamId(6));
            }
            m_RealStartTime = m_StartTime;
        }

        private Vector3 m_RelativeCenter = Vector3.Zero;
        private bool m_RelativeToTarget = false;
        private long m_DurationTime = 0;
        private long m_IntervalTime = 0;
        private long m_LastTime = 0;

        private long m_RealStartTime = 0;
    }
    /// <summary>
    /// track(speed[, start_time]);
    /// </summary>
    internal class TrackTriger : AbstractSkillTriger
    {
        public override ISkillTriger Clone()
        {
            TrackTriger triger = new TrackTriger();
            triger.m_Speed = m_Speed;
            triger.m_StartTime = m_StartTime;
            triger.m_RealStartTime = m_RealStartTime;
            triger.m_RealSpeed = m_RealSpeed;
            return triger;
        }
        public override void Reset()
        {
            m_IsStarted = false;
            m_LifeTime = 0;
            m_RealStartTime = m_StartTime;
            m_RealSpeed = m_Speed;
        }
        public override bool Execute(object sender, SkillInstance instance, long delta, long curSectionTime)
        {
            GfxSkillSenderInfo senderObj = sender as GfxSkillSenderInfo;
            if (null == senderObj) {
                return false;
            }
            if (senderObj.ConfigData.type == (int)SkillOrImpactType.Skill) {
                return false;//track只能在impact或buff里使用
            }
            Scene scene = senderObj.Scene;
            EntityInfo obj = senderObj.GfxObj;
            if (null != obj) {
                if (m_RealStartTime < 0) {
                    m_RealStartTime = TriggerUtil.RefixStartTime((int)m_StartTime, instance.LocalVariables, senderObj.ConfigData);
                }
                if (curSectionTime >= m_RealStartTime) {

                    if (!m_IsStarted) {
                        m_IsStarted = true;

                        Vector3 dest = obj.GetMovementStateInfo().GetPosition3D();
                        dest.Y += 1.5f;

                        Vector3 pos = scene.EntityController.GetImpactSenderPosition(senderObj.ActorId, senderObj.SkillId, senderObj.Seq);

                        if (m_RealSpeed < Geometry.c_FloatPrecision) {
                            object speedObj;
                            if (instance.LocalVariables.TryGetValue("emitSpeed", out speedObj)) {
                                m_RealSpeed = (float)speedObj;
                            }
                        }
                        if (m_RealSpeed >= Geometry.c_FloatPrecision) {
                            m_LifeTime = (long)(1000 * (dest - pos).Length() / m_RealSpeed);
                        }
                    } else if (curSectionTime > m_RealStartTime + m_LifeTime) {
                        m_HitEffectRotation = Quaternion.Identity;

                        Dictionary<string, object> args;
                        TriggerUtil.CalcHitConfig(instance.LocalVariables, senderObj.ConfigData, out args);
                        if (args.ContainsKey("hitEffectRotation"))
                            args["hitEffectRotation"] = m_HitEffectRotation;
                        else
                            args.Add("hitEffectRotation", m_HitEffectRotation);
                        scene.EntityController.TrackSendImpact(senderObj.ActorId, senderObj.SkillId, senderObj.Seq, args);

                        instance.StopCurSection();
                        return false;
                    }

                    //GameFramework.LogSystem.Debug("EmitEffectTriger:{0}", m_EffectPath);
                    return true;
                } else {
                    return true;
                }
            } else {
                instance.StopCurSection();
                return false;
            }
        }
        protected override void Load(Dsl.CallData callData, int dslSkillId)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_Speed = float.Parse(callData.GetParamId(0));
            }
            if (num > 1) {
                m_StartTime = long.Parse(callData.GetParamId(1));
            }
            m_RealStartTime = m_StartTime;
            m_RealSpeed = m_Speed;
        }

        private float m_Speed = 10f;

        private long m_RealStartTime = 0;
        private float m_RealSpeed = 10f;

        private bool m_IsStarted = false;
        private Quaternion m_HitEffectRotation;
        private long m_LifeTime = 0;
    }
    /// <summary>
    /// colliderimpact(start_time, center_x, center_y, center_z, duration[, finishOnCollide, singleHit]);
    /// </summary>
    internal class ColliderImpactTriger : AbstractSkillTriger
    {
        public override ISkillTriger Clone()
        {
            ColliderImpactTriger triger = new ColliderImpactTriger();
            triger.m_StartTime = m_StartTime;
            triger.m_RelativeCenter = m_RelativeCenter;
            triger.m_DurationTime = m_DurationTime;
            triger.m_FinishOnCollide = m_FinishOnCollide;
            triger.m_SingleHit = m_SingleHit;
            triger.m_RealStartTime = m_RealStartTime;
            return triger;
        }
        public override void Reset()
        {
            m_IsStarted = false;
            m_LastPos = Vector3.Zero;
            m_RealStartTime = m_StartTime;
            m_Targets.Clear();
        }
        public override bool Execute(object sender, SkillInstance instance, long delta, long curSectionTime)
        {
            GfxSkillSenderInfo senderObj = sender as GfxSkillSenderInfo;
            if (null == senderObj) return false;
            Scene scene = senderObj.Scene;
            EntityInfo obj = senderObj.GfxObj;
            if (obj == null) {
                return false;
            }
            if (m_RealStartTime < 0) {
                m_RealStartTime = TriggerUtil.RefixStartTime((int)m_StartTime, instance.LocalVariables, senderObj.ConfigData);
            }
            if (curSectionTime < m_RealStartTime) {
                return true;
            }
            int impactId = 0;
            int senderId = 0;
            int targetType = scene.EntityController.GetTargetType(senderObj.ActorId, senderObj.ConfigData, senderObj.Seq);
            if (targetType == (int)SkillTargetType.Self)
                impactId = senderObj.ConfigData.impactToSelf;
            else
                impactId = senderObj.ConfigData.impactToTarget;
            if (senderObj.ConfigData.type == (int)SkillOrImpactType.Skill) {
                senderId = senderObj.ActorId;
            } else {
                senderId = senderObj.TargetActorId;
            }

            float range = 0;
            TableConfig.Skill cfg = senderObj.ConfigData;
            if (null != cfg) {
                range = cfg.aoeSize;
            }
            float angle = obj.GetMovementStateInfo().GetFaceDir();
            Vector3 center = Geometry.TransformPoint(obj.GetMovementStateInfo().GetPosition3D(), m_RelativeCenter, angle);
            if (!m_IsStarted) {
                m_IsStarted = true;
                m_LastPos = center;
            } else if ((center - m_LastPos).LengthSquared() >= 0.25f || m_RealStartTime + m_DurationTime < curSectionTime) {
                Vector3 c = (m_LastPos + center) / 2;
                Vector3 angleu = center - m_LastPos;
                float queryRadius = range + angleu.Length() / 2;

                int ct = 0;
                bool isCollide = false;
                scene.KdTree.Query(c.X, c.Y, c.Z, queryRadius, (float distSqr, KdTreeObject kdTreeObj) => {
                    int targetId = kdTreeObj.Object.GetId();
                    if (targetType == (int)SkillTargetType.Enemy && CharacterRelation.RELATION_ENEMY == scene.EntityController.GetRelation(senderId, targetId) ||
                        targetType == (int)SkillTargetType.Friend && CharacterRelation.RELATION_FRIEND == scene.EntityController.GetRelation(senderId, targetId)) {
                        bool isMatch = Geometry.IsCapsuleDiskIntersect(new ScriptRuntime.Vector2(center.X, center.Z), new ScriptRuntime.Vector2(angleu.X, angleu.Z), range, new ScriptRuntime.Vector2(kdTreeObj.Position.X, kdTreeObj.Position.Z), kdTreeObj.Radius);
                        if (isMatch) {
                            isCollide = true;
                            if (!m_SingleHit || !m_Targets.Contains(targetId)) {
                                m_Targets.Add(targetId);
                                Dictionary<string, object> args;
                                TriggerUtil.CalcHitConfig(instance.LocalVariables, senderObj.ConfigData, out args);
                                scene.EntityController.SendImpact(senderObj.ConfigData, senderObj.Seq, senderObj.ActorId, senderId, targetId, impactId, args);
                                ++ct;
                                if (senderObj.ConfigData.maxAoeTargetCount <= 0 || ct < senderObj.ConfigData.maxAoeTargetCount) {
                                    return true;
                                } else {
                                    return false;
                                }
                            }
                        }
                    }
                    return true;
                });

                m_LastPos = center;

                if (isCollide && m_FinishOnCollide) {
                    return false;
                }
            }
            if (m_RealStartTime + m_DurationTime < curSectionTime) {
                instance.StopCurSection();
                return false;
            }
            return true;
        }

        protected override void Load(Dsl.CallData callData, int dslSkillId)
        {
            int num = callData.GetParamNum();
            if (num >= 5) {
                m_StartTime = long.Parse(callData.GetParamId(0));
                m_RelativeCenter.X = float.Parse(callData.GetParamId(1));
                m_RelativeCenter.Y = float.Parse(callData.GetParamId(2));
                m_RelativeCenter.Z = float.Parse(callData.GetParamId(3));
                m_DurationTime = long.Parse(callData.GetParamId(4));
            }
            if (num >= 6) {
                m_FinishOnCollide = callData.GetParamId(5) == "true";
            }
            if (num >= 7) {
                m_SingleHit = callData.GetParamId(6) == "true";
            }
            m_RealStartTime = m_StartTime;
        }

        private Vector3 m_RelativeCenter = Vector3.Zero;
        private long m_DurationTime = 1000;
        private bool m_FinishOnCollide = false;
        private bool m_SingleHit = false;

        private long m_RealStartTime = 0;

        private bool m_IsStarted = false;
        private Vector3 m_LastPos = Vector3.Zero;
        private HashSet<int> m_Targets = new HashSet<int>();
    }
}