using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace WaymarkStudio.Windows;

internal class StudioWindow : Window, IDisposable
{
    private readonly Vector2 iconButtonSize = new(30, 30);
    bool isHoverPreview = false;
    bool wasHoverPreview = false;
    string popupRename = "";
    int deleteIndex = -1;

    internal StudioWindow()
        : base("Waymark Studio", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new(310, 440);
        SizeCondition = ImGuiCond.Once;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(250, 330),
        };
    }

    public void Dispose() { }

    public unsafe override void Draw()
    {
        isHoverPreview = false;
        if (ImGui.BeginTable("StudioTable", 1, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.SizingFixedSame))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (ImGui.CollapsingHeader("Draft", ImGuiTreeNodeFlags.DefaultOpen))
                using (ImRaii.Disabled(!Plugin.WaymarkManager.IsSupportedZone()))
                    DrawDraftSection();

            ImGui.Spacing();

            if (ImGui.CollapsingHeader("Guide", ImGuiTreeNodeFlags.DefaultOpen))
                using (ImRaii.Disabled(!Plugin.WaymarkManager.IsSupportedZone()))
                    DrawGuideSection();

            ImGui.Spacing();
            ImGui.EndTable();
        }
        ImGui.SameLine();
        DrawSavedPresets();

        if (wasHoverPreview && !isHoverPreview)
            Plugin.WaymarkManager.ClearHoverPreview();
        wasHoverPreview = isHoverPreview;
    }

    internal void DrawDraftSection()
    {
        ImGui.Checkbox("Place real marker if possible", ref Plugin.Config.PlaceRealIfPossible);
        ImGui.Checkbox("Snap to grid", ref Plugin.Config.SnapXZToGrid);

        using (ImRaii.Disabled(!Plugin.WaymarkManager.IsSupportedZone()))
        {
            WaymarkButton(Waymark.A); ImGui.SameLine();
            WaymarkButton(Waymark.B); ImGui.SameLine();
            WaymarkButton(Waymark.C); ImGui.SameLine();
            WaymarkButton(Waymark.D); ImGui.SameLine();
            using (ImRaii.Disabled(!Plugin.WaymarkManager.showGuide))
            {
                var guide = Plugin.WaymarkManager.circleGuide;
                if (CustomTextureIconButton("circle_card", iconButtonSize))
                {
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.A, guide.North);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.B, guide.East);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.C, guide.South);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.D, guide.West);
                }

                HoverTooltip("Place Circles on guide cardinals");
                /*
                ImGui.SameLine();
                if (CustomTextureIconButton("circle_intercard", _iconSize))
                {
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.A, guide.NorthWest);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.B, guide.NorthEast);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.C, guide.SouthEast);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.D, guide.SouthWest);
                }
                HoverTooltip("Place Circles on guide intercardinals");
                */
            }

            WaymarkButton(Waymark.One); ImGui.SameLine();
            WaymarkButton(Waymark.Two); ImGui.SameLine();
            WaymarkButton(Waymark.Three); ImGui.SameLine();
            WaymarkButton(Waymark.Four); ImGui.SameLine();
            using (ImRaii.Disabled(!Plugin.WaymarkManager.showGuide))
            {
                var guide = Plugin.WaymarkManager.circleGuide;
                if (CustomTextureIconButton("square_intercard", iconButtonSize))
                {
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.One, guide.NorthWest);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.Two, guide.NorthEast);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.Three, guide.SouthEast);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.Four, guide.SouthWest);
                }
                HoverTooltip("Place Squares on guide intercardinals");
                /*
                ImGui.SameLine();
                if (CustomTextureIconButton("square_card", _iconSize))
                {
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.One, guide.North);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.Two, guide.East);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.Three, guide.South);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.Four, guide.West);
                }
                HoverTooltip("Place Squares on guide cardinals");
                */
            }

            if (TextureIconButton(61502, iconButtonSize))
            {
                Plugin.WaymarkManager.ClearPlaceholders();
            }
            HoverTooltip("Clear Draft");
            ImGui.SameLine();
            if (TextureIconButton(60026, iconButtonSize))
            {
                Plugin.WaymarkManager.ClearPlaceholders();
                Plugin.WaymarkManager.NativeClearWaymarks();
            }
            HoverTooltip("Clear All");
        }
        using (ImRaii.Disabled(Plugin.WaymarkManager.placeholders.Count == 0))
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Save, "Save Draft"))
            {
                var preset = Plugin.WaymarkManager.Preset;
                preset.Name += $" {Plugin.Storage.CountPresetsForTerritoryId(Plugin.WaymarkManager.territoryId) + 1}";
                Plugin.Config.SavedPresets.Add(preset);
                Plugin.Config.Save();
            }
            HoverTooltip("Save current draft to saved presets");
        }
        using (ImRaii.Disabled(Plugin.WaymarkManager.placeholders.Count == 0
            || !Plugin.WaymarkManager.IsSafeToPlaceWaymarks()))
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.MapMarkedAlt, "Place Draft"))
            {
                Plugin.WaymarkManager.SafePlacePreset(Plugin.WaymarkManager.Preset);
            }
            HoverTooltip("Replace draft markers with real markers\nTBD how existing waymarks should be treated");
        }
        using (ImRaii.Disabled(Plugin.WaymarkManager.Waymarks.Count == 0))
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.MapMarkerAlt, "Import markers"))
            {
                foreach ((Waymark w, Vector3 p) in Plugin.WaymarkManager.Waymarks)
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(w, p);
                Plugin.WaymarkManager.NativeClearWaymarks();
            }
            HoverTooltip("Replace real markers with draft markers");
        }
    }

    internal void DrawGuideSection()
    {
        if (Plugin.WaymarkManager.showGuide && ImGuiComponents.IconButtonWithText(FontAwesomeIcon.EyeSlash, "Hide Guide"))
        {
            Plugin.WaymarkManager.showGuide = false;
        }
        else if (!Plugin.WaymarkManager.showGuide)
        {
            if (Plugin.WaymarkManager.circleGuide.center == Vector3.Zero)
            {
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.LocationCrosshairs, "Place Guide"))
                {
                    Plugin.WaymarkManager.showGuide = true;
                    Plugin.Overlay.StartMouseWorldPosSelecting("circleGuide");
                }
            }
            else if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Eye, "Show Guide"))
            {
                Plugin.WaymarkManager.showGuide = true;
            }
        }

        ImGui.TextUnformatted("Position:");
        ImGui.SetNextItemWidth(125f);
        ImGui.SameLine();
        ImGui.InputFloat3("##position", ref Plugin.WaymarkManager.circleGuide.center, "%.1f");
        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.MousePointer))
        {
            Plugin.WaymarkManager.showGuide = true;
            Plugin.Overlay.StartMouseWorldPosSelecting("circleGuide");
        }
        switch (Plugin.Overlay.MouseWorldPosSelection("circleGuide", ref Plugin.WaymarkManager.circleGuide.center))
        {
            case PctOverlay.SelectionResult.Canceled:
                Plugin.WaymarkManager.showGuide = false;
                break;
        }

        ImGui.TextUnformatted("Radius:");
        ImGui.SetNextItemWidth(120f);
        ImGui.SameLine();
        ImGui.SliderInt("##radius", ref Plugin.WaymarkManager.circleGuide.Radius, 1, 20);

        ImGui.TextUnformatted("Spokes:");
        ImGui.SetNextItemWidth(120f);
        ImGui.SameLine();
        ImGui.SliderInt("##spokes", ref Plugin.WaymarkManager.circleGuide.Spokes, 0, 16);

        ImGui.TextUnformatted("Rings:");
        ImGui.SetNextItemWidth(120f);
        ImGui.SameLine();
        ImGui.SliderInt("##rings", ref Plugin.WaymarkManager.circleGuide.Rings, 1, 10);

        ImGui.TextUnformatted("Rotation:");
        ImGui.SetNextItemWidth(120f);
        ImGui.SameLine();
        ImGui.DragInt("##rotation", ref Plugin.WaymarkManager.circleGuide.RotationDegrees, 15, -180, 180);
    }

    internal void DrawSavedPresets()
    {
        if (ImGui.BeginTable("PresetsTable", 2, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.SizingFixedSame | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text($"{Plugin.WaymarkManager.mapName}");
            ImGui.Text("Saved Presets");

            var presets = Plugin.Config.SavedPresets;
            deleteIndex = -1;
            int i;
            for (i = 0; i < presets.Count; i++)
            {
                var preset = presets[i];
                if (preset.TerritoryId == Plugin.WaymarkManager.territoryId)
                {
                    DrawPresetRow(i, preset);
                }
            }
            if (deleteIndex >= 0)
            {
                presets.RemoveAt(deleteIndex);
                Plugin.Config.Save();
            }

            if (Plugin.WaymarkManager.contentFinderId > 0)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text($"\nNative Presets");
                foreach ((var j, var nativePreset) in Plugin.Storage.NativePresets(Plugin.WaymarkManager.contentFinderId))
                {
                    DrawPresetRow(i++, nativePreset.ToPreset($"{j + 1}. Game Preset"), isReadOnly: true);
                }
            }
            ImGui.EndTable();
        }
    }

    internal void DrawPresetRow(int i, WaymarkPreset preset, bool isReadOnly = false)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        using (ImRaii.Disabled(preset.TerritoryId != Plugin.WaymarkManager.territoryId))
        {
            using (ImRaii.Disabled(!Plugin.WaymarkManager.IsSafeToPlaceWaymarks()))
            {
                Vector2 buttonSize = new(200, ImGui.GetFrameHeight());
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.MapMarkedAlt, preset.Name + "##" + i, defaultColor: new(), size: buttonSize))
                {
                    Plugin.WaymarkManager.SafePlacePreset(preset);
                }
                isHoverPreview |= HoverWaymarkPreview(preset);
                HoverTooltip(() =>
                {
                    TextActiveWaymarks(preset);
                    ImGui.TextUnformatted(preset.Time.ToLocalTime().ToString());
                });
            }
            ImGui.TableNextColumn();
            if (ImGuiComponents.IconButton($"draft_preset##{i}", FontAwesomeIcon.MapMarkerAlt))
            {
                Plugin.WaymarkManager.SetPlaceholderPreset(preset);
            }
            isHoverPreview |= HoverWaymarkPreview(preset);
            HoverTooltip("Load as draft");
        }
        if (!isReadOnly)
        {
            ImGui.SameLine();
            if (ImGuiComponents.IconButton($"edit_preset##{i}", FontAwesomeIcon.Edit))
            {
                popupRename = preset.Name;
                ImGui.OpenPopup($"edit_popup##{i}");
            }
            HoverTooltip("Edit name");
            if (ImGui.BeginPopup($"edit_popup##{i}"))
            {
                ImGui.SetNextItemWidth(200f);
                var result = ImGui.InputText("##preset_rename", ref popupRename, 50, ImGuiInputTextFlags.EnterReturnsTrue);

                if (ImGuiComponents.IconButton(FontAwesomeIcon.Check) || result)
                {
                    if (popupRename.Length > 0)
                    {
                        preset.Name = popupRename;
                        Plugin.Config.Save();
                    }
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Times) || result)
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            ImGui.SameLine();
            if (ImGuiComponents.IconButton($"delete_preset##{i}", FontAwesomeIcon.TrashAlt))
            {
                deleteIndex = i;
            }
            HoverTooltip("Delete preset");
        }
    }

    internal void WaymarkButton(Waymark w)
    {
        if (TextureIconButton(Waymarks.GetIconId(w), iconButtonSize))
        {
            Plugin.Overlay.StartMouseWorldPosSelecting(w);
        }
        Vector3 pos = Plugin.WaymarkManager.placeholders.GetValueOrDefault(w);
        switch (Plugin.Overlay.MouseWorldPosSelection(w, ref pos))
        {
            case PctOverlay.SelectionResult.Canceled:
                Plugin.WaymarkManager.ClearWaymarkPlaceholder(w);
                break;
            case PctOverlay.SelectionResult.Selected:
                if (Plugin.Config.PlaceRealIfPossible
                    && Plugin.WaymarkManager.SafePlaceWaymark(w, pos))
                {
                    Plugin.WaymarkManager.ClearWaymarkPlaceholder(w);
                    break;
                }
                goto case PctOverlay.SelectionResult.SelectingValid;
            case PctOverlay.SelectionResult.SelectingValid:
                Plugin.WaymarkManager.PlaceWaymarkPlaceholder(w, pos);
                break;
            case PctOverlay.SelectionResult.SelectingInvalid:
                Plugin.WaymarkManager.ClearWaymarkPlaceholder(w);
                break;
        }
    }

    internal bool TextureIconButton(uint iconId, Vector2 size)
    {
        var wrap = Plugin.TextureProvider.GetFromGameIcon(iconId).GetWrapOrEmpty();
        if (wrap != null)
            return ImGui.ImageButton(wrap.ImGuiHandle, size, Vector2.Zero, Vector2.One, 1, Vector4.Zero);
        else
            return ImGui.Button("##" + iconId, size);
    }

    internal bool CustomTextureIconButton(string name, Vector2 size)
    {
        var wrap = Plugin.TextureProvider.GetFromFile(GetCustomImagePath(name)).GetWrapOrEmpty();
        if (wrap != null)
            return ImGui.ImageButton(wrap.ImGuiHandle, size, Vector2.Zero, Vector2.One, 1, Vector4.Zero);
        else
            return ImGui.Button("##" + name, size);
    }
    private static string GetCustomImagePath(string name)
    {
        return Path.Combine(Plugin.Interface.AssemblyLocation.Directory?.FullName!, "res", $"{name}.png");
    }

    internal static void HoverTooltip(string text)
    {
        HoverTooltip(() => ImGui.TextUnformatted(text));
    }

    internal static void HoverTooltip(Action action)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            DisplayTooltip(action);
        }
    }

    private static void DisplayTooltip(Action action)
    {
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
        action();
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }

    internal static bool HoverWaymarkPreview(WaymarkPreset preset)
    {
        if (ImGui.IsItemHovered())
        {
            Plugin.WaymarkManager.SetHoverPreview(preset);
            return true;
        }
        return false;
    }

    internal void TextActiveWaymarks(WaymarkPreset preset)
    {
        foreach (Waymark w in Enum.GetValues<Waymark>())
        {
            ImGui.PushStyleColor(ImGuiCol.Text, preset.MarkerPositions.ContainsKey(w) ? Waymarks.GetColor(w) : 0x70FFFFFF);
            ImGui.Text(Waymarks.GetName(w));
            ImGui.SameLine();
            ImGui.PopStyleColor();
        }
        ImGui.NewLine();
    }
}