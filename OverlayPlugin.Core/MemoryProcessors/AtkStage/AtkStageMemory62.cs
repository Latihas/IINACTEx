using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage;

using AtkStage = FFXIVClientStructs.FFXIV.Component.GUI.AtkStage;

internal interface IAtkStageMemory62 : IAtkStageMemory;

internal class AtkStageMemory62(TinyIoCContainer container) : AtkStageMemory, IAtkStageMemory62 {
	public virtual Version GetVersion() => new(6, 2);

	public unsafe IntPtr GetAddonAddress(string name) {
		var atkStage = AtkStage.Instance();
		if (atkStage == null)
			return nint.Zero;

		var unitMgr = atkStage->RaptureAtkUnitManager;
		if (unitMgr == null)
			return nint.Zero;

		var addon = unitMgr->GetAddonByName(name);
		if (addon == null)
			return nint.Zero;

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


		// These addons are known to exist but not mapped yet:
		// (double entries are intentional, they appear twice in the list in game memory)
		/**
		Achievement
		ActionDetail
		ActionMenu
		AddonContextMenuTitle
		AddonContextSub
		AdventureNoteBook
		AetherCurrent
		AreaMap
		ArmouryBoard
		Character
		CharacterStatus
		ChatLog
		CircleFinder
		CircleList
		ConfigKeybind
		ConfigSystem
		ContactList
		ContentsInfo
		ContentsNote
		ContentsReplaySetting
		CountDownSettingDialog
		CrossWorldLinkshell
		Currency
		CursorAddon
		CursorLocation
		Dawn
		DawnStory
		DragDropS
		Emote
		FadeBack
		FadeMiddle
		FateProgress
		Filter
		FilterSystem
		FishGuide2
		FishingNote
		FreeCompany
		FreeCompanyTopics
		GSInfoGeneral
		GatheringNote
		GoldSaucerInfo
		HousingMenu
		HowToList
		Hud
		HudLayout
		Inventory
		InventoryCrystalGrid
		InventoryCrystalGrid
		InventoryEventGrid0
		InventoryEventGrid0E
		InventoryEventGrid1
		InventoryEventGrid1E
		InventoryEventGrid2
		InventoryEventGrid2E
		InventoryExpansion
		InventoryGrid
		InventoryGrid0
		InventoryGrid0E
		InventoryGrid1
		InventoryGrid1E
		InventoryGrid2E
		InventoryGrid3E
		InventoryGridCrystal
		InventoryLarge
		ItemDetail
		JobHudWHM
		JobHudWHM0
		Journal
		JournalDetail
		JournalDetail
		LicenseViewer
		LinkShell
		LoadingTips
		LookingForGroup
		Marker
		McGuffin
		MinionNoteBook
		MiragePrismPrismItemDetail
		MonsterNote
		MountNoteBook
		MountSpeed
		NowLoading
		OperationGuide
		Orchestrion
		OrnamentNoteBook
		PlayGuide
		PvpProfile
		PvpProfileColosseum
		QuestRedoHud
		RecommendList
		ScenarioTree
		ScreenFrameSystem
		ScreenLog
		Social
		SocialList
		SupportDesk
		Tooltip
		VVDFinder
		WebLauncher
		_ActionContents
		_AllianceList1
		_AllianceList2
		_AreaText
		_AreaText
		_BagWidget
		_BattleTalk
		_ContentGauge
		_DTR
		_FlyText
		_FocusTargetInfo
		_Image
		_Image
		_Image3
		_Image3
		_LimitBreak
		_LocationTitle
		_LocationTitleShort
		_MainCommand
		_MainCross
		_MiniTalk
		_Money
		_NaviMap
		_Notification
		_ParameterWidget
		_PoisonText
		_PoisonText
		_PopUpText
		_ScreenInfoBack
		_ScreenInfoFront
		_ScreenText
		_Status
		_StatusCustom0
		_StatusCustom1
		_StatusCustom2
		_StatusCustom3
		_TargetCursor
		_TargetCursorGround
		_TargetInfo
		_TargetInfoBuffDebuff
		_TargetInfoCastBar
		_TargetInfoMainTarget
		_TextChain
		_TextClassChange
		_TextError
		_ToDoList
		_WideText
		_WideText
		*/
	};

	public T? GetAddon<T>() where T : struct {
		var name = AddonMap.FirstOrDefault(x => x.Value == typeof(T)).Key;
		return (T?)GetAddon(name);
	}

	public object? GetAddon(string name) {
		if (!AddonMap.TryGetValue(name, out var addonType) || !IsValid())
			return null;

		var ptr = GetAddonAddress(name);
		if (ptr == nint.Zero) return null;
		var addon = Marshal.PtrToStructure(ptr, addonType);
		return addon;
	}
}