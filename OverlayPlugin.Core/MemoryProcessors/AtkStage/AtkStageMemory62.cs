using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage;

using AtkStage = FFXIVClientStructs.FFXIV.Component.GUI.AtkStage;

internal interface IAtkStageMemory62 : IAtkStageMemory;

internal class AtkStageMemory62 : AtkStageMemory, IAtkStageMemory62 {
	public virtual Version GetVersion() => new(6, 2);

	public unsafe IntPtr GetAddonAddress(string name) {
		var atkStage = AtkStage.Instance();
		if (atkStage == null) return IntPtr.Zero;
		var unitMgr = atkStage->RaptureAtkUnitManager;
		if (unitMgr == null) return IntPtr.Zero;
		var addon = unitMgr->GetAddonByName(name);
		if (addon == null) return IntPtr.Zero;
		return (IntPtr)addon;
	}

	private static readonly Dictionary<string, Type> AddonMap = new() {
		// These addon entries are confirmed from the FFXIVClientStructs repos
		{
			"_ActionCross", typeof(AddonActionCross)
		}, {
			"_ActionBar01", typeof(AddonActionBarX)
		}, {
			"_ActionBar02", typeof(AddonActionBarX)
		}, {
			"_ActionDoubleCrossL", typeof(AddonActionDoubleCrossBase)
		}, {
			"_ActionDoubleCrossR", typeof(AddonActionDoubleCrossBase)
		}, {
			"_CastBar", typeof(AddonCastBar)
		}, {
			"CharacterInspect", typeof(AddonCharacterInspect)
		}, {
			"ChatLogPanel_0", typeof(AddonChatLogPanel)
		}, {
			"ChatLogPanel_1", typeof(AddonChatLogPanel)
		}, {
			"ChatLogPanel_2", typeof(AddonChatLogPanel)
		}, {
			"ChatLogPanel_3", typeof(AddonChatLogPanel)
		}, {
			"ItemSearchResult", typeof(AddonItemSearchResult)
		}, {
			"_PartyList", typeof(AddonPartyList)
		}, {
			"Macro", typeof(AddonMacro)
		}, {
			"Teleport", typeof(AddonTeleport)
		},

		// These addons are guessed based on patterns
		{
			"_ActionBar", typeof(AddonActionBarX)
		}, {
			"_ActionBar03", typeof(AddonActionBarX)
		}, {
			"_ActionBar04", typeof(AddonActionBarX)
		}, {
			"_ActionBar05", typeof(AddonActionBarX)
		}, {
			"_ActionBar06", typeof(AddonActionBarX)
		}, {
			"_ActionBar07", typeof(AddonActionBarX)
		}, {
			"_ActionBar08", typeof(AddonActionBarX)
		}, {
			"_ActionBar09", typeof(AddonActionBarX)
		}, {
			"_ActionBarEx", typeof(AddonActionBarX)
		},

		// These addons are guessed based on names matching up or based on github code search
		{
			"AOZNotebook", typeof(AddonAOZNotebook)
		}, {
			"ChocoboBreedTraining", typeof(AddonChocoboBreedTraining)
		}, {
			"ContentsFinder", typeof(AddonContentsFinder)
		}, {
			"ContentsFinderConfirm", typeof(AddonContentsFinderConfirm)
		}, {
			"ContextIconMenu", typeof(AddonContextIconMenu)
		}, {
			"ContextMenu", typeof(AddonContextMenu)
		}, {
			"_EnemyList", typeof(AddonEnemyList)
		}, {
			"_Exp", typeof(AddonExp)
		}, {
			"FateReward", typeof(AddonFateReward)
		}, {
			"FieldMarker", typeof(AddonFieldMarker)
		}, {
			"Gathering", typeof(AddonGathering)
		}, {
			"GatheringMasterpiece", typeof(AddonGatheringMasterpiece)
		}, {
			"GrandCompanySupplyReward", typeof(AddonGrandCompanySupplyReward)
		}, {
			"GuildLeve", typeof(AddonGuildLeve)
		}, {
			"_HudLayoutScreen", typeof(AddonHudLayoutScreen)
		}, {
			"_HudLayoutWindow", typeof(AddonHudLayoutWindow)
		}, {
			"ItemInspectionList", typeof(AddonItemInspectionList)
		}, {
			"ItemInspectionResult", typeof(AddonItemInspectionResult)
		}, {
			"JournalDetail", typeof(AddonJournalDetail)
		}, {
			"JournalResult", typeof(AddonJournalResult)
		}, {
			"LotteryDaily", typeof(AddonLotteryDaily)
		}, {
			"MaterializeDialog", typeof(AddonMaterializeDialog)
		}, {
			"MateriaRetrieveDialog", typeof(AddonMateriaRetrieveDialog)
		}, {
			"NamePlate", typeof(AddonNamePlate)
		}, {
			"NeedGreed", typeof(AddonNeedGreed)
		}, {
			"RaceChocoboResult", typeof(AddonRaceChocoboResult)
		}, {
			"RecipeNote", typeof(AddonRecipeNote)
		}, {
			"ReconstructionBox", typeof(AddonReconstructionBox)
		}, {
			"RelicNoteBook", typeof(AddonRelicNoteBook)
		}, {
			"Repair", typeof(AddonRepair)
		}, {
			"Request", typeof(AddonRequest)
		}, {
			"RetainerList", typeof(AddonRetainerList)
		}, {
			"RetainerSell", typeof(AddonRetainerSell)
		}, {
			"RetainerTaskAsk", typeof(AddonRetainerTaskAsk)
		}, {
			"RetainerTaskList", typeof(AddonRetainerTaskList)
		}, {
			"RetainerTaskResult", typeof(AddonRetainerTaskResult)
		}, {
			"SalvageDialog", typeof(AddonSalvageDialog)
		}, {
			"SalvageItemSelector", typeof(AddonSalvageItemSelector)
		}, {
			"SatisfactionSupply", typeof(AddonSatisfactionSupply)
		}, {
			"SelectIconString", typeof(AddonSelectIconString)
		}, {
			"SelectOk", typeof(AddonSelectOk)
		}, {
			"SelectString", typeof(AddonSelectString)
		},
		// Both of these seem to exist in memory somehow??
		{
			"SelectYesno", typeof(AddonSelectYesno)
		}, {
			"_SelectYesNo", typeof(AddonSelectYesno)
		}, {
			"ShopCardDialog", typeof(AddonShopCardDialog)
		}, {
			"Synthesis", typeof(AddonSynthesis)
		}, {
			"Talk", typeof(AddonTalk)
		}, {
			"WeeklyBingo", typeof(AddonWeeklyBingo)
		}, {
			"WeeklyPuzzle", typeof(AddonWeeklyPuzzle)
		}
	};

	public T? GetAddon<T>() where T : struct {
		var name = AddonMap.FirstOrDefault(x => x.Value == typeof(T)).Key;
		return (T?)GetAddon(name);
	}

	public object? GetAddon(string name) {
		if (!AddonMap.TryGetValue(name, out var addonType) || !IsValid()) return null;
		var ptr = GetAddonAddress(name);
		if (ptr == nint.Zero) return null;
		var addon = Marshal.PtrToStructure(ptr, addonType);
		return addon;
	}
}