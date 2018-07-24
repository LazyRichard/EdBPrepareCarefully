﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public static class PresetSaver {
        //
        // Static Methods
        //
        public static void SaveToFile(PrepareCarefully data, string presetName) {
            bool problem = false;
            try {
                // Verify that all pawns have non-null identifiers.
                foreach (CustomPawn customPawn in data.Pawns) {
                    if (customPawn.Id == null) {
                        customPawn.GenerateId();
                    }
                }
                foreach (var g in data.RelationshipManager.ParentChildGroups) {
                    foreach (var parent in g.Parents) {
                        if (parent.Pawn != null && parent.Pawn.Id == null) {
                            parent.Pawn.GenerateId();
                        }
                        foreach (var child in g.Children) {
                            if (child.Pawn != null && child.Pawn.Id == null) {
                                child.Pawn.GenerateId();
                            }
                        }
                    }
                }

                SaveRecordPresetV4 preset = new SaveRecordPresetV4();
                preset.mods = GenText.ToCommaList(Enumerable.Select<ModContentPack, string>(LoadedModManager.RunningMods, (Func<ModContentPack, string>)(mod => mod.Name)), true);
                foreach (CustomPawn customPawn in data.Pawns) {
                    SaveRecordPawnV4 pawn = new SaveRecordPawnV4(customPawn);
                    preset.pawns.Add(pawn);
                }
                foreach (var g in data.RelationshipManager.ParentChildGroups) {
                    foreach (var parent in g.Parents) {
                        if (parent.Pawn.Hidden) {
                            if (parent.Pawn != null) {
                                SaveRecordPawnV4 pawn = new SaveRecordPawnV4(parent.Pawn);
                                preset.pawns.Add(pawn);
                            }
                            else {
                                Log.Warning("Prepare Carefully found an empty pawn in a parent child relationship while saving the preset.  Skipping that pawn.");
                            }
                        }
                        foreach (var child in g.Children) {
                            if (child.Pawn.Hidden) {
                                if (child.Pawn != null) {
                                    SaveRecordPawnV4 pawn = new SaveRecordPawnV4(child.Pawn);
                                    preset.pawns.Add(pawn);
                                }
                                else {
                                    Log.Warning("Prepare Carefully found an empty pawn in a parent child relationship while saving the preset.  Skipping that pawn.");
                                }
                            }
                        }
                    }
                }
                foreach (var r in data.RelationshipManager.Relationships) {
                    if (r.source != null && r.target != null && r.def != null && r.source.Id != null && r.target.Id != null) {
                        SaveRecordRelationshipV3 s = new SaveRecordRelationshipV3(r);
                        preset.relationships.Add(s);
                    }
                    else {
                        problem = true;
                        Log.Warning("Prepare Carefully found an invalid custom relationship when saving a preset: " + presetName);
                        if (r.target != null && r.source != null) {
                            Log.Warning("  Relationship = { source = " + r.source.Id + ", target = " + r.target.Id + ", relationship = " + r.def + "}");
                        }
                        else {
                            Log.Warning("  Relationship = { source = " + r.source + ", target = " + r.target + ", relationship = " + r.def + "}");
                        }
                    }
                }
                foreach (var g in data.RelationshipManager.ParentChildGroups) {
                    if (g.Children.Count == 0 || (g.Parents.Count == 0 && g.Children.Count == 1)) {
                        continue;
                    }
                    SaveRecordParentChildGroupV3 group = new SaveRecordParentChildGroupV3();
                    group.parents = new List<string>();
                    group.children = new List<string>();
                    foreach (var p in g.Parents) {
                        if (p.Pawn == null) {
                            problem = true;
                            Log.Warning("Prepare Carefully found an invalid parent/child relationship when saving the preset: " + presetName);
                            continue;
                        }
                        else {
                            group.parents.Add(p.Pawn.Id);
                        }
                    }
                    foreach (var p in g.Children) {
                        if (p.Pawn == null) {
                            problem = true;
                            Log.Warning("Prepare Carefully found an invalid parent/child relationship when saving the preset: " + presetName);
                            continue;
                        }
                        else {
                            group.children.Add(p.Pawn.Id);
                        }
                    }
                    preset.parentChildGroups.Add(group);
                }
                foreach (var e in data.Equipment) {
                    SaveRecordEquipmentV3 record = new SaveRecordEquipmentV3(e);
                    preset.equipment.Add(record);
                }

                // Start saving.
                Scribe.saver.InitSaving(PresetFiles.FilePathForSavedPreset(presetName), "preset");
                preset.ExposeData();
            }
            catch (Exception e) {
                PrepareCarefully.Instance.State.AddError("EdB.PC.Dialog.Preset.Error.SaveFailed".Translate());
                Log.Error("Failed to save preset file");
                throw e;
            }
            finally {
                Scribe.saver.FinalizeSaving();
                Scribe.mode = LoadSaveMode.Inactive;
                if (problem) {
                    PrepareCarefully.Instance.State.AddError("EdB.PC.Dialog.Preset.Error.PartialSaveFailure".Translate());
                }
            }
        }
    }
}
