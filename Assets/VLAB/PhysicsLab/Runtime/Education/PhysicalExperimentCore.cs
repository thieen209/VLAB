using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLAB.PhysicsLab.Education
{
    public enum PhysicalActionKind
    {
        Grabbed,
        Released,
        ParameterAdjusted,
        Snapped,
        Detached,
        TriggerPressed,
        MeasurementRecorded,
        SlidingStarted,
        CollisionOccurred
    }

    public readonly struct PhysicalLabEvent
    {
        public PhysicalLabEvent(Component source, string objectId, PhysicalActionKind kind, double value = 0d, string unit = "")
        {
            Source = source;
            ObjectId = objectId ?? string.Empty;
            Kind = kind;
            Value = value;
            Unit = unit ?? string.Empty;
        }

        public Component Source { get; }
        public string ObjectId { get; }
        public PhysicalActionKind Kind { get; }
        public double Value { get; }
        public string Unit { get; }
        public bool IsLive => Source != null;
    }

    public readonly struct LiveMeasurementSample
    {
        public LiveMeasurementSample(Component source, string name, double value, string unit, string parameterSignature)
        {
            Source = source;
            Name = name ?? string.Empty;
            Value = value;
            Unit = unit ?? string.Empty;
            ParameterSignature = string.IsNullOrWhiteSpace(parameterSignature) ? "physical-state" : parameterSignature;
            Timestamp = Time.timeAsDouble;
        }

        public Component Source { get; }
        public string Name { get; }
        public double Value { get; }
        public string Unit { get; }
        public string ParameterSignature { get; }
        public double Timestamp { get; }
        public bool IsValid => Source != null && !string.IsNullOrWhiteSpace(Name) && !double.IsNaN(Value) && !double.IsInfinity(Value);
    }

    public sealed class PhysicalExperimentStep
    {
        public PhysicalExperimentStep(PhysicalActionKind action, string objectId, string cue)
        {
            Action = action;
            ObjectId = objectId ?? string.Empty;
            Cue = cue ?? string.Empty;
        }

        public PhysicalActionKind Action { get; }
        public string ObjectId { get; }
        public string Cue { get; }

        public bool Matches(PhysicalLabEvent labEvent) => labEvent.IsLive
            && labEvent.Kind == Action
            && (string.IsNullOrEmpty(ObjectId) || labEvent.ObjectId.IndexOf(ObjectId, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    public sealed class PhysicalExperimentDefinition
    {
        public PhysicalExperimentDefinition(string id, params PhysicalExperimentStep[] steps)
        {
            Id = id;
            Steps = steps ?? Array.Empty<PhysicalExperimentStep>();
        }

        public string Id { get; }
        public IReadOnlyList<PhysicalExperimentStep> Steps { get; }
    }

    public static class PhysicalExperimentDefinitionCatalog
    {
        private static readonly IReadOnlyDictionary<string, PhysicalExperimentDefinition> Definitions =
            new Dictionary<string, PhysicalExperimentDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                ["PHY_01"] = new PhysicalExperimentDefinition("PHY_01",
                    Step(PhysicalActionKind.Grabbed, "Pendulum_Bob", "Giữ và kéo quả nặng lệch nhẹ khỏi vị trí cân bằng."),
                    Step(PhysicalActionKind.Released, "Pendulum_Bob", "Thả quả nặng, không đẩy thêm."),
                    Step(PhysicalActionKind.MeasurementRecorded, "Pendulum_Bob", "Chờ cảm biến ghi đủ một chu kỳ thực.")),
                ["PHY_02"] = new PhysicalExperimentDefinition("PHY_02",
                    Step(PhysicalActionKind.ParameterAdjusted, "Projectile_Launcher", "Xoay trực tiếp nòng phóng đến góc mong muốn."),
                    Step(PhysicalActionKind.Snapped, "Projectile", "Đặt viên bi vào đúng ổ nạp trên máy phóng."),
                    Step(PhysicalActionKind.TriggerPressed, "Projectile_Launcher", "Nhấn cò trên thân máy phóng."),
                    Step(PhysicalActionKind.MeasurementRecorded, "Projectile", "Quan sát vị trí bi chạm bàn.")),
                ["PHY_03"] = new PhysicalExperimentDefinition("PHY_03",
                    Step(PhysicalActionKind.Snapped, "Mass", "Đặt quả cân lên khối ma sát."),
                    Step(PhysicalActionKind.Grabbed, "Spring_Force_Meter", "Cầm lực kế và kéo theo phương ngang."),
                    Step(PhysicalActionKind.SlidingStarted, "Friction_Block", "Tiếp tục kéo đều khi vật bắt đầu trượt."),
                    Step(PhysicalActionKind.MeasurementRecorded, "Spring_Force_Meter", "Giữ chuyển động ổn định để ghi lực thực.")),
                ["PHY_04"] = new PhysicalExperimentDefinition("PHY_04",
                    Step(PhysicalActionKind.Snapped, "Physics_Photogate", "Gắn hai cổng quang vào các mốc trên ray."),
                    Step(PhysicalActionKind.Released, "Air_Track_Glider", "Đẩy nhẹ rồi thả xe trượt."),
                    Step(PhysicalActionKind.MeasurementRecorded, "Digital_Timer", "Đợi xe đi qua cổng và đọc đồng hồ.")),
                ["PHY_05"] = new PhysicalExperimentDefinition("PHY_05",
                    Step(PhysicalActionKind.Snapped, "Mass", "Gắn quả cân vào đầu dưới lò xo."),
                    Step(PhysicalActionKind.Grabbed, "Mass", "Kéo quả cân xuống một đoạn nhỏ."),
                    Step(PhysicalActionKind.Released, "Mass", "Thả quả cân, không đẩy thêm."),
                    Step(PhysicalActionKind.MeasurementRecorded, "Physics_Coil_Spring", "Chờ hệ ghi chu kỳ dao động thực.")),
                ["PHY_06"] = new PhysicalExperimentDefinition("PHY_06",
                    Step(PhysicalActionKind.Snapped, "Collision", "Gắn đầu va chạm phù hợp lên xe."),
                    Step(PhysicalActionKind.Released, "Air_Track_Glider", "Đẩy và thả xe 1 dọc theo ray."),
                    Step(PhysicalActionKind.CollisionOccurred, "Air_Track_Glider", "Quan sát va chạm thật giữa hai xe."),
                    Step(PhysicalActionKind.MeasurementRecorded, "Momentum", "Chờ hệ ghi vận tốc và động lượng sau va chạm."))
            };

        public static PhysicalExperimentDefinition Get(string id) =>
            id != null && Definitions.TryGetValue(id, out var definition) ? definition : null;

        private static PhysicalExperimentStep Step(PhysicalActionKind action, string objectId, string cue) =>
            new PhysicalExperimentStep(action, objectId, cue);
    }

    public sealed class PhysicalExperimentProgress
    {
        private readonly PhysicalExperimentDefinition definition;

        public PhysicalExperimentProgress(PhysicalExperimentDefinition definition)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public event Action<int> StepAdvanced;
        public int StepIndex { get; private set; }
        public bool IsCoreComplete => StepIndex >= definition.Steps.Count;
        public string CurrentCue => IsCoreComplete ? "Lặp lại phép đo để hoàn thiện bảng kết quả." : definition.Steps[StepIndex].Cue;
        public bool CanObserve(PhysicalLabEvent labEvent) => !IsCoreComplete && definition.Steps[StepIndex].Matches(labEvent);

        public bool Observe(PhysicalLabEvent labEvent)
        {
            if (!CanObserve(labEvent))
            {
                return false;
            }
            StepIndex++;
            StepAdvanced?.Invoke(StepIndex);
            return true;
        }

        public void Reset()
        {
            StepIndex = 0;
            StepAdvanced?.Invoke(StepIndex);
        }
    }

    public sealed class PhysicalTrialRecorder
    {
        private readonly List<LiveMeasurementSample> samples = new List<LiveMeasurementSample>();
        private readonly int requiredTrials;

        public PhysicalTrialRecorder(int requiredTrials)
        {
            this.requiredTrials = Math.Max(1, requiredTrials);
        }

        public IReadOnlyList<LiveMeasurementSample> Samples => samples;
        public int TrialCount => samples.Count;
        public int RequiredTrials => requiredTrials;
        public bool HasEnoughTrials => TrialCount >= requiredTrials;

        public bool TryRecord(LiveMeasurementSample sample)
        {
            if (!sample.IsValid)
            {
                return false;
            }
            samples.Add(sample);
            return true;
        }

        public void Clear() => samples.Clear();
    }
}
