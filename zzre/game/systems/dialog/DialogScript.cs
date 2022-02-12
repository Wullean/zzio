﻿using System;
using System.Linq;
using DefaultEcs.Command;
using DefaultEcs.System;
using zzio;
using zzio.scn;

namespace zzre.game.systems
{
    public partial class DialogScript : BaseScript
    {
        public enum SpecialInventoryCheck
        {
            HasFivePixies = 0,
            HasAFairy,
            HasAtLeastNFairies,
            HasFairyOfClass
        }

        public enum SceneObjectType
        {
            Platforms = 0,
            Items
        }

        public enum SubGameType
        {
            ChestPuzzle = 0,
            ElfGame
        }

        private readonly UI ui;
        private readonly Scene scene;
        private readonly Game game;
        private readonly EntityCommandRecorder recorder;
        private readonly IDisposable startDialogDisposable;
        private readonly IDisposable removedDisposable;

        private DefaultEcs.Entity dialogEntity;
        private EntityRecord RecordDialogEntity() => recorder.Record(dialogEntity);
        private DefaultEcs.Entity NPCEntity => dialogEntity.Get<components.DialogNPC>().Entity;

        public DialogScript(ITagContainer diContainer) : base(diContainer, CreateEntityContainer)
        {
            World.SetMaxCapacity<components.DialogState>(1);
            ui = diContainer.GetTag<UI>();
            scene = diContainer.GetTag<Scene>();
            game = diContainer.GetTag<Game>();
            recorder = diContainer.GetTag<EntityCommandRecorder>();
            startDialogDisposable = World.Subscribe<messages.StartDialog>(HandleStartDialog);
            removedDisposable = World.SubscribeComponentRemoved<components.DialogState>(HandleDialogStateRemoved);
        }

        public override void Dispose()
        {
            base.Dispose();
            startDialogDisposable.Dispose();
        }

        private void HandleStartDialog(in messages.StartDialog message)
        {
            if (dialogEntity.IsAlive)
                throw new InvalidOperationException("A dialog is already open");

            dialogEntity = World.CreateEntity();
            var dialogEntityRecord = RecordDialogEntity();
            dialogEntityRecord.Set(components.DialogState.NextScriptOp);
            dialogEntityRecord.Set(new components.DialogNPC(message.NpcEntity));
            dialogEntityRecord.Set(new components.ScriptExecution(GetScriptSource(message)));

            World.Publish(default(messages.ui.GameScreenOpened));
            World.Publish(messages.LockPlayerControl.Forever);
        }

        private void HandleDialogStateRemoved(in DefaultEcs.Entity _, in components.DialogState __)
        {
            World.Publish(default(messages.ui.GameScreenClosed));
            World.Publish(messages.LockPlayerControl.Unlock);
        }

        [WithPredicate]
        private bool ShouldContinueScript(in components.DialogState state) => state == components.DialogState.NextScriptOp;

        [Update]
        private void Update(in DefaultEcs.Entity entity, ref components.ScriptExecution execution)
        {
            if (!Continue(entity, ref execution))
                recorder.Record(dialogEntity).Dispose();
        }

        private void Say(DefaultEcs.Entity entity, UID uid, bool silent)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void Choice(DefaultEcs.Entity entity, int targetLabel, UID uid)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void WaitForUser(DefaultEcs.Entity entity)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void SetCamera(DefaultEcs.Entity entity, int triggerArg)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void ChangeWaypoint(DefaultEcs.Entity entity, int fromWpId, int toWpId)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void Fight(DefaultEcs.Entity entity, int stage, bool canFlee)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void ChangeDatabase(DefaultEcs.Entity entity, UID uid)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void RemoveNpc(DefaultEcs.Entity entity)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void CatchWizform(DefaultEcs.Entity entity)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void KillPlayer(DefaultEcs.Entity entity)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void TradingCurrency(DefaultEcs.Entity entity, UID uid)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void TradingCard(DefaultEcs.Entity entity, int price, UID uid)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void GivePlayerCards(DefaultEcs.Entity entity, int count, int type, int id)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void SetupGambling(DefaultEcs.Entity entity, int count, int type, int id)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private bool IfPlayerHasCards(DefaultEcs.Entity entity, int count, int type, int id)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
            return false;
        }

        private bool IfPlayerHasSpecials(DefaultEcs.Entity entity, SpecialInventoryCheck specialType, int arg)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
            return false;
        }

        private bool IfTriggerIsActive(DefaultEcs.Entity entity, int triggerI)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
            return false;
        }

        private void RemovePlayerCards(DefaultEcs.Entity entity, int count, int type, int id)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void LockUserInput(DefaultEcs.Entity entity, int mode)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void ModifyTrigger(DefaultEcs.Entity entity, int enableTrigger, int id, int triggerI)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void PlayAnimation(DefaultEcs.Entity entity, AnimationType animation)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void NpcWizformEscapes(DefaultEcs.Entity entity)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void Talk(DefaultEcs.Entity entity, UID uid)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void ChafferWizforms(UID uid, UID uid2, UID uid3)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void DeployMeAtTrigger(int triggerI)
        {
            World.Publish(new messages.CreaturePlaceToTrigger(NPCEntity, triggerI, orientByTrigger: true, moveToGround: true));
            NPCEntity.Get<components.NPCMovement>().CurWaypointId = -1;
        }

        private void DeployPlayerAtTrigger(int triggerI)
        {
            World.Publish(new messages.CreaturePlaceToTrigger(game.PlayerEntity, triggerI, orientByTrigger: true, moveToGround: true));
        }

        private void DeployNPCAtTrigger(int triggerI, UID uid)
        {
            var otherNpc = World.GetEntities()
                .With((in zzio.db.NpcRow dbRow) => dbRow.Uid == uid)
                .AsEnumerable()
                .FirstOrDefault();
            if (!otherNpc.IsAlive)
                return;

            otherNpc.Get<components.NPCMovement>().CurWaypointId = -1;
            var isFairyNpc = otherNpc.Get<components.NPCType>() == components.NPCType.Flying;
            World.Publish(new messages.CreaturePlaceToTrigger(game.PlayerEntity, triggerI, orientByTrigger: true, moveToGround: !isFairyNpc));

            if (isFairyNpc)
                Console.WriteLine("Warning: DeployNPCAtTrigger not implemented for fairy NPCs"); // TODO: Implement DeployNPCAtTrigger for fairy NPCs
        }

        private void Delay(DefaultEcs.Entity entity, int duration)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void RemoveWizforms(DefaultEcs.Entity entity)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private bool IfNPCModifierHasValue(DefaultEcs.Entity entity, int value)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
            return false;
        }

        private void SetNPCModifier(DefaultEcs.Entity entity, int scene, int triggerI, int value)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private bool IfPlayerIsClose(DefaultEcs.Entity entity, int maxDistSqr)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
            return false;
        }

        private bool IfNumberOfNpcsIs(DefaultEcs.Entity entity, int count, UID uid)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
            return false;
        }

        private void StartEffect(DefaultEcs.Entity entity, int effectType, int triggerI)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void SetTalkLabels(DefaultEcs.Entity entity, int labelYes, int labelNo, int mode)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void TradeWizform(DefaultEcs.Entity entity, int id)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void CreateDynamicItems(DefaultEcs.Entity entity, int id, int count, int triggerI)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void PlayVideo(DefaultEcs.Entity entity, int id)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void RemoveNpcAtTrigger(int triggerI)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void Revive(DefaultEcs.Entity entity)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private bool IfTriggerIsEnabled(int triggerI)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
            return false;
        }

        private void PlaySound(DefaultEcs.Entity entity, int id)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void PlayInArena(DefaultEcs.Entity entity, int arg)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void EndActorEffect(DefaultEcs.Entity entity)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void CreateSceneObjects(DefaultEcs.Entity entity, SceneObjectType objectType)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void RemoveBehavior(DefaultEcs.Entity entity, int id)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void UnlockDoor(DefaultEcs.Entity entity, int id, bool isMetalDoor)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void EndGame(DefaultEcs.Entity entity)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void SubGame(DefaultEcs.Entity entity, SubGameType subGameType, int size, int labelExit)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void PlayPlayerAnimation(DefaultEcs.Entity entity, AnimationType animation)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void PlayAmyVoice(string v)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void CreateDynamicModel(DefaultEcs.Entity entity)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void DeploySound(DefaultEcs.Entity entity, int id, int triggerI)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }

        private void GivePlayerPresent(DefaultEcs.Entity entity)
        {
            var curMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Console.WriteLine($"Warning: unimplemented dialog instruction \"{curMethod!.Name}\"");
        }
    }
}
