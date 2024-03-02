using Celeste64.TAS.Input;
using Celeste64.TAS.Util;
using ImGuiNET;
using NativeFileDialogSharp;

namespace Celeste64.TAS;

public static class InfoHUD
{
    private static bool editingCustomTemplate = false;
    private static Vec3? lastPlayerPosition = null;

    [EnableRun]
    private static void Start()
    {
        lastPlayerPosition = null;
    }

    public static void Update()
    {
        if (!Manager.IsPaused() && Game.Scene is World world)
        {
            var player = world.Get<Player>();
            if (player != null)
            {
                lastPlayerPosition = player.Position;
            } else
            {
                lastPlayerPosition = null;
            }
        }
    }

    public static void RenderGUI()
    {
        var io = ImGui.GetIO();
        io.FontGlobalScale = Save.Instance.InfoHudFontSize;

        ImGui.SetNextWindowSizeConstraints(new Vec2(200f, 200f), new Vec2(float.PositiveInfinity, float.PositiveInfinity));
        ImGui.Begin("Info HUD", ImGuiWindowFlags.MenuBar);

        if (ImGui.BeginMenuBar()) {
            if (ImGui.BeginMenu("File"))
            {
                ImGui.Text($"Current: {InputController.TasFilePath}");

                if (ImGui.MenuItem("Open"))
                {
                    var result = Dialog.FileOpen("tas", Directory.GetCurrentDirectory());
                    if (result.IsOk)
                        InputController.StudioTasFilePath = result.Path;
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Settings"))
            {
                bool showInputs = Save.Instance.InfoHudShowInputs;
                bool showWorld = Save.Instance.InfoHudShowWorld;
                bool showCustom = Save.Instance.InfoHudShowCustom;
                int decimals = Save.Instance.InfoHudDecimals;
                float fontSize = Save.Instance.InfoHudFontSize;

                ImGui.Checkbox("Show Input Display", ref showInputs);
                ImGui.Checkbox("Show World Information", ref showWorld);
                ImGui.Checkbox("Show Custom Info", ref showCustom);
                ImGui.InputInt("Decimal Points", ref decimals);
                ImGui.InputFloat("Font Size", ref fontSize, step: 0.1f);

                Save.Instance.InfoHudShowInputs = showInputs;
                Save.Instance.InfoHudShowWorld = showWorld;
                Save.Instance.InfoHudShowCustom = showCustom;
                Save.Instance.InfoHudDecimals = Math.Max(decimals, 1);
                Save.Instance.InfoHudFontSize = Math.Max(fontSize, 0.1f);
                ImGui.EndMenu();
            }
            ImGui.EndMenuBar();
        }

        if (Save.Instance.InfoHudShowInputs)
        {
            var controller = Manager.Controller;
            var inputs = controller.Inputs;
            if (Manager.Running && controller.CurrentFrameInTas >= 0 && controller.CurrentFrameInTas < inputs.Count)
            {
                InputFrame? current = controller.Current;
                if (controller.CurrentFrameInTas >= 1 && current != controller.Previous) {
                    current = controller.Previous;
                }

                var previous = current!.Prev;
                var next = current.Next;

                int maxLine = Math.Max(current.Line, Math.Max(previous?.Line ?? 0, next?.Line ?? 0)) + 1;
                int linePadLeft = maxLine.ToString().Length;

                int maxFrames = Math.Max(current.Frames, Math.Max(previous?.Frames ?? 0, next?.Frames ?? 0));
                int framesPadLeft = maxFrames.ToString().Length;

                string FormatInputFrame(InputFrame inputFrame) => $"{(inputFrame.Line + 1).ToString().PadLeft(linePadLeft)}: {string.Empty.PadLeft(framesPadLeft - inputFrame.Frames.ToString().Length)}{inputFrame}";

                if (previous != null) ImGui.Text(FormatInputFrame(previous));

                string currentStr = FormatInputFrame(current);
                int currentFrameLength = controller.CurrentFrameInInput.ToString().Length;
                int inputWidth = currentStr.Length + currentFrameLength + 2;
                inputWidth = Math.Max(inputWidth, 20);
                ImGui.Text( $"{currentStr.PadRight(inputWidth - currentFrameLength)}{controller.CurrentFrameInInputForHud}");

                if (next != null) ImGui.Text(FormatInputFrame(next));

                ImGui.Text(string.Empty);
            }
        }

        if (Save.Instance.InfoHudShowWorld && Game.Scene is World world)
        {
            var player = world.Get<Player>();
            if (player != null)
            {
                ImGui.Text($"Pos: {player.Position.X.ToFormattedString(Save.Instance.InfoHudDecimals)} {player.Position.Y.ToFormattedString(Save.Instance.InfoHudDecimals)} {player.Position.Z.ToFormattedString(Save.Instance.InfoHudDecimals)}");
                ImGui.Text($"Spd: {player.Velocity.X.ToFormattedString(Save.Instance.InfoHudDecimals)} {player.Velocity.Y.ToFormattedString(Save.Instance.InfoHudDecimals)} {player.Velocity.Z.ToFormattedString(Save.Instance.InfoHudDecimals)}");
                if (lastPlayerPosition == null)
                    ImGui.Text($"Vel: {player.Velocity.X.ToFormattedString(Save.Instance.InfoHudDecimals)} {player.Velocity.Y.ToFormattedString(Save.Instance.InfoHudDecimals)} {player.Velocity.Z.ToFormattedString(Save.Instance.InfoHudDecimals)}");
                else
                    ImGui.Text($"Vel: {((player.Position.X - lastPlayerPosition.Value.X) / Time.Delta).ToFormattedString(Save.Instance.InfoHudDecimals)} {((player.Position.Y - lastPlayerPosition.Value.Y) / Time.Delta).ToFormattedString(Save.Instance.InfoHudDecimals)} {((player.Position.Z - lastPlayerPosition.Value.Z) / Time.Delta).ToFormattedString(Save.Instance.InfoHudDecimals)}");
                ImGui.Text(string.Empty);

                List<string> statues = new();
                if (player.tPlatformVelocityStorage > 0)
                {
                    Vec3 add = player.platformVelocity;

                    add.Z = Calc.Clamp(add.Z, 0, 180);
                    if (add.XY().LengthSquared() > 300 * 300)
                        add = add.WithXY(add.XY().Normalized() * 300);

                    statues.Add($"PlatformSpeed({add})");
                }

                if (player.tCoyote > 0)
                    statues.Add($"Coyote({player.tCoyote.ToFrames()})@{player.coyoteZ.ToFormattedString(Save.Instance.InfoHudDecimals)}");
                if (player.tClimbCooldown > 0)
                    statues.Add($"ClimbCD({player.tClimbCooldown.ToFrames()})");
                if (player.tDashCooldown > 0)
                    statues.Add($"DashCD({player.tDashCooldown.ToFrames()})");
                if (player.tNoDashJump > 0)
                    statues.Add($"DashJumpCD({player.tNoDashJump.ToFrames()})");
                if (player.tNoSkidJump > 0)
                    statues.Add($"SkidJumpCD({player.tNoSkidJump.ToFrames()})");

                // Taken from player.TryClimb()
                bool canClimb = player.ClimbCheckAt(Vec3.Zero, out var wall);
                if (!canClimb && player.Velocity.Z > 0 && !player.onGround && player.stateMachine.State != Player.States.Climbing)
                    canClimb = player.ClimbCheckAt(Vec3.UnitZ * 4, out wall);

                if (canClimb)
                    statues.Add($"CanClimb");
                if (world.SolidWallCheckClosestToNormal(player.SolidWaistTestPos, Player.ClimbCheckDist, -new Vec3(player.targetFacing, 0), out _))
                    statues.Add($"CanWallJump");
                statues.Add($"St{player.stateMachine.State?.ToString() ?? string.Empty}");

                ImGui.TextWrapped(string.Join("  ", statues));

                string timerStr = (int) Save.CurrentRecord.Time.TotalHours > 0
                    ? $"{((int) Save.CurrentRecord.Time.TotalHours):00}:{Save.CurrentRecord.Time.Minutes:00}:{Save.CurrentRecord.Time.Seconds:00}:{Save.CurrentRecord.Time.Milliseconds:000}"
                    : $"{Save.CurrentRecord.Time.Minutes:00}:{Save.CurrentRecord.Time.Seconds:00}:{Save.CurrentRecord.Time.Milliseconds:000}";
                ImGui.Text($"[{Save.Instance.LevelID}] Timer: {timerStr}({((float)Save.CurrentRecord.Time.TotalSeconds).ToFrames()})");
            }

            ImGui.Text(string.Empty);
        }

        if (Save.Instance.InfoHudShowCustom)
        {
            if (!editingCustomTemplate)
            {
                ImGui.Text(CustomInfo.GetInfo());
                editingCustomTemplate = ImGui.Button("Edit Custom Info Template");
            }
            else
            {
                string template = Save.Instance.InfoHudShowCustomTemplate;
                ImGui.InputTextMultiline("##Edit Template", ref template, 32767, new Vec2(300.0f * Save.Instance.InfoHudFontSize, 200.0f * Save.Instance.InfoHudFontSize));
                Save.Instance.InfoHudShowCustomTemplate = template;

                editingCustomTemplate = !ImGui.Button("Save Custom Info Template");
            }

        }

        ImGui.End();
    }

    public static int ToFrames(this float seconds)
    {
        return (int) Math.Ceiling(seconds / Time.Delta);
    }
}
