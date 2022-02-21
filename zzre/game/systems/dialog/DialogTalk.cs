﻿using System;
using System.Numerics;
using zzio;
using zzio.db;
using zzio.vfs;

namespace zzre.game.systems
{
    public partial class DialogTalk : ui.BaseScreen<components.ui.DialogTalk, messages.DialogTalk>
    {
        private static readonly components.ui.ElementId IDExit = new(1);
        private static readonly components.ui.ElementId IDContinue = new(2);
        private static readonly components.ui.ElementId IDYes = new(3);
        private static readonly components.ui.ElementId IDNo = new(4);

        private readonly MappedDB db;
        private readonly IResourcePool resourcePool;
        private readonly IDisposable resetUIDisposable;

        public DialogTalk(ITagContainer diContainer) : base(diContainer, isBlocking: false)
        {
            db = diContainer.GetTag<MappedDB>();
            resourcePool = diContainer.GetTag<IResourcePool>();
            resetUIDisposable = World.Subscribe<messages.DialogResetUI>(HandleResetUI);
            OnElementDown += HandleElementDown;
        }

        public override void Dispose()
        {
            base.Dispose();
            resetUIDisposable.Dispose();
        }

        private void HandleResetUI(in messages.DialogResetUI message)
        {
            foreach (var entity in Set.GetEntities())
                entity.Dispose();
        }

        protected override void HandleOpen(in messages.DialogTalk message)
        {
            // TODO: Fix talk text layout by overriding line height per label

            message.DialogEntity.Set(components.DialogState.Talk);

            var wasAlreadyOpen = Set.Count > 0;
            World.Publish(new messages.DialogResetUI(message.DialogEntity));
            var uiWorld = ui.GetTag<DefaultEcs.World>();
            var uiEntity = World.CreateEntity();
            uiEntity.Set(new components.Parent(message.DialogEntity));
            uiEntity.Set(new components.ui.DialogTalk(message.DialogEntity));

            preload.CreateDialogBackground(uiEntity, animateOverlay: !wasAlreadyOpen, out var bgRect);
            CreateTalkLabel(uiEntity, message.DialogUID, bgRect);
            var npcEntity = message.DialogEntity.Get<components.DialogNPC>().Entity;
            var faceWidth = TryCreateFace(uiEntity, npcEntity, bgRect);
            CreateNameLabel(uiEntity, npcEntity, bgRect, faceWidth);

            var talkLabels = message.DialogEntity.Get<components.DialogTalkLabels>();
            if (talkLabels == components.DialogTalkLabels.Exit)
                CreateSingleButton(uiEntity, new UID(0xF7DFDC21), IDExit, bgRect);
            else if (talkLabels == components.DialogTalkLabels.Continue)
                CreateSingleButton(uiEntity, new UID(0xCABAD411), IDContinue, bgRect);
            else
                CreateYesNoButtons(uiEntity, bgRect);
        }

        private const float MaxTextWidth = 400f;
        private const float TextOffsetX = 55f;
        private const float TextOffsetY = 195f;
        private void CreateTalkLabel(DefaultEcs.Entity parent, UID dialogUID, Rect bgRect)
        {
            var text = db.GetDialog(dialogUID).Text;
            if (text.Length > 0 && text[0] >= 'A' && text[0] <= 'Z' && false)
                text = $"{{8*{text[0]}}}{text[1..]}"; // use the ridiculous font for the first letter

            var entity = preload.CreateAnimatedLabel(
                parent,
                Vector2.Zero,
                text,
                preload.Fnt003,
                wrapLines: 400f);
            var tileSheet = entity.Get<rendering.TileSheet>();
            var textHeight = tileSheet.GetTextHeight(text);
            ref var labelRect = ref entity.Get<Rect>();
            labelRect = new Rect(
                bgRect.Min.X + TextOffsetX,
                bgRect.Min.Y + TextOffsetY - textHeight / 2,
                MaxTextWidth,
                textHeight);
        }

        private const string BaseFacePath = "resources/bitmaps/faces/";
        private float? TryCreateFace(DefaultEcs.Entity parent, DefaultEcs.Entity npcEntity, Rect bgRect)
        {
            var npcBodyEntity = npcEntity.Get<components.ActorParts>().Body;
            var npcModelName = npcBodyEntity.Get<resources.ClumpInfo>().Name
                .Replace(".dff", "", StringComparison.OrdinalIgnoreCase);
            var hasFace = resourcePool.FindFile($"{BaseFacePath}{npcModelName}.bmp") != null;

            if (!hasFace)
                return null;
            var faceEntity = preload.CreateImage(
                parent,
                bgRect.Min + Vector2.One * 20f,
                $"faces/{npcModelName}",
                renderOrder: 0);
            return faceEntity.Get<Rect>().Size.X;
        }

        private const float NameOffsetY = 35f;
        private void CreateNameLabel(DefaultEcs.Entity parent, DefaultEcs.Entity npcEntity, Rect bgRect, float? faceWidth)
        {
            var npcName = npcEntity.Get<NpcRow>().Name;
            var entity = preload.CreateLabel(
                parent,
                Vector2.Zero,
                npcName,
                preload.Fnt001);
            var tileSheet = entity.Get<rendering.TileSheet>();
            ref var rect = ref entity.Get<Rect>();
            rect = new Rect(
                faceWidth.HasValue
                    ? bgRect.Min.X + faceWidth.Value + 25
                    : bgRect.Center.X - tileSheet.GetUnformattedWidth(npcName) / 2,
                bgRect.Min.Y + NameOffsetY,
                0f, 0f);
        }

        private const float YesNoButtonOffsetX = 4f;
        private const float ButtonOffsetY = -50f;
        private void CreateSingleButton(DefaultEcs.Entity parent, UID textUID, components.ui.ElementId elementId, Rect bgRect)
        {
            preload.CreateButton(
                parent,
                elementId,
                new(bgRect.Center.X, bgRect.Max.Y + ButtonOffsetY),
                textUID,
                new(0, 1),
                preload.Btn000,
                preload.Fnt000,
                out _,
                btnAlign: new(components.ui.Alignment.Center, components.ui.Alignment.Min));

            // TODO: Set cursor position in dialog talk
        }

        private void CreateYesNoButtons(DefaultEcs.Entity parent, Rect bgRect)
        {
            preload.CreateButton(
                parent,
                IDYes,
                new(bgRect.Center.X + YesNoButtonOffsetX, bgRect.Max.Y + ButtonOffsetY),
                new UID(0xB2153621),
                new(0, 1),
                preload.Btn000,
                preload.Fnt000,
                out _,
                btnAlign: new(components.ui.Alignment.Max, components.ui.Alignment.Min));

            preload.CreateButton(
                parent,
                IDNo,
                new(bgRect.Center.X + YesNoButtonOffsetX, bgRect.Max.Y + ButtonOffsetY),
                new UID(0x2F5B3621),
                new(0, 1),
                preload.Btn000,
                preload.Fnt000,
                out _);
        }

        private void HandleElementDown(DefaultEcs.Entity clickedEntity, components.ui.ElementId clickedId)
        {
            var talkEntity = Set.GetEntities()[0];
            var dialogEntity = talkEntity.Get<components.ui.DialogTalk>().DialogEntity;
            if (clickedId == IDContinue || clickedId == IDExit)
            {
                // TODO: Play sound sample on dialog talk button clicked
                dialogEntity.Set(components.DialogState.NextScriptOp);
            }
            if (clickedId == IDYes || clickedId == IDNo)
            {
                var talkLabels = dialogEntity.Get<components.DialogTalkLabels>();
                int targetLabel = clickedId == IDYes ? talkLabels.LabelYes : talkLabels.LabelNo;
                ref var scriptExec = ref dialogEntity.Get<components.ScriptExecution>();
                scriptExec.CurrentI = scriptExec.LabelTargets[targetLabel];
                dialogEntity.Set(components.DialogState.NextScriptOp);
            }
        }

        protected override void Update(float timeElapsed, in DefaultEcs.Entity entity, ref components.ui.DialogTalk component)
        {
        }
    }
}