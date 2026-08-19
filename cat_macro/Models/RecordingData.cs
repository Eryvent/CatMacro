using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CatMacro.Models
{
    public class RecordingData
    {
        [JsonProperty("version")]
        public string Version { get; set; } = "1.0";

        [JsonProperty("name")]
        public string Name { get; set; } = "Untitled Recording";

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonProperty("duration")]
        public long Duration { get; set; }

        [JsonProperty("actions")]
        public List<MacroAction> Actions { get; set; } = new();

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static RecordingData? FromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<RecordingData>(json);
            }
            catch { return null; }
        }
    }

    public abstract class MacroAction
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "";

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        public abstract string GetDescription();
    }

    public class KeyPressAction : MacroAction
    {
        [JsonProperty("key")]
        public int KeyCode { get; set; }

        [JsonProperty("keyName")]
        public string KeyName { get; set; } = "";

        public KeyPressAction() { }
        public KeyPressAction(int keyCode, string keyName)
        {
            KeyCode = keyCode;
            KeyName = keyName;
            Type = "KeyPress";
        }

        public override string GetDescription() => $"Press {KeyName}";
    }

    public class KeyReleaseAction : MacroAction
    {
        [JsonProperty("key")]
        public int KeyCode { get; set; }

        [JsonProperty("keyName")]
        public string KeyName { get; set; } = "";

        public KeyReleaseAction() { }
        public KeyReleaseAction(int keyCode, string keyName)
        {
            KeyCode = keyCode;
            KeyName = keyName;
            Type = "KeyRelease";
        }

        public override string GetDescription() => $"Release {KeyName}";
    }

    public class MouseMoveAction : MacroAction
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        public MouseMoveAction() { }
        public MouseMoveAction(int x, int y)
        {
            X = x;
            Y = y;
            Type = "MouseMove";
        }

        public override string GetDescription() => $"Mouse Move to ({X}, {Y})";
    }

    public class MouseDownAction : MacroAction
    {
        [JsonProperty("button")]
        public string Button { get; set; } = "Left";

        public MouseDownAction() { }
        public MouseDownAction(string button = "Left")
        {
            Button = button;
            Type = "MouseDown";
        }

        public override string GetDescription() => $"{Button} Button Down";
    }

    public class MouseUpAction : MacroAction
    {
        [JsonProperty("button")]
        public string Button { get; set; } = "Left";

        public MouseUpAction() { }
        public MouseUpAction(string button = "Left")
        {
            Button = button;
            Type = "MouseUp";
        }

        public override string GetDescription() => $"{Button} Button Up";
    }

    public class MouseWheelAction : MacroAction
    {
        [JsonProperty("delta")]
        public int Delta { get; set; }

        public MouseWheelAction() { }
        public MouseWheelAction(int delta)
        {
            Delta = delta;
            Type = "MouseWheel";
        }

        public override string GetDescription() => $"Mouse Wheel {(Delta > 0 ? "Up" : "Down")}";
    }

    public class WaitAction : MacroAction
    {
        [JsonProperty("duration")]
        public int Duration { get; set; }

        public WaitAction() { }
        public WaitAction(int duration)
        {
            Duration = duration;
            Type = "Wait";
        }

        public override string GetDescription() => $"Wait {Duration} ms";
    }
}
