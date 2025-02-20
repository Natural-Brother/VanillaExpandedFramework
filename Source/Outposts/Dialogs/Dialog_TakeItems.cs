using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Outposts
{
    public class Dialog_TakeItems : Window
    {
        private readonly Vector2 BottomButtonSize = new(160f, 40f);
        private readonly Caravan caravan;
        private readonly Outpost outpost;
        private TransferableOneWayWidget itemsTransfer;
        private List<TransferableOneWay> transferables;

        public Dialog_TakeItems(Outpost outpost, Caravan caravan)
        {
            outpost.CheckNoDestroyedOrNoStack();
            this.outpost = outpost;
            this.caravan = caravan;
        }

        public override Vector2 InitialSize => new(1024f, UI.screenHeight - 100f);

        public override float Margin => 17f;

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);
            var rect = inRect.AtZero();
            rect.yMin += 30f;
            DoBottomButtons(rect);
            itemsTransfer.OnGUI(rect);
            GUI.EndGroup();
        }

        private void DoBottomButtons(Rect rect)
        {
            var rect2 = new Rect(rect.width - BottomButtonSize.x, rect.height - 40f, BottomButtonSize.x, BottomButtonSize.y);
            if (Widgets.ButtonText(rect2, "Outposts.Take".Translate()))
            {
                foreach (var transferable in transferables)
                    while (transferable.HasAnyThing && transferable.CountToTransfer > 0)
                    {
                        var thing = transferable.things.Pop();
                        if (thing.stackCount <= transferable.CountToTransfer)
                        {
                            transferable.AdjustBy(-thing.stackCount);
                            thing.holdingOwner?.Remove(thing);
                            caravan.AddPawnOrItem(outpost.TakeItem(thing), true);
                        }
                        else
                        {
                            caravan.AddPawnOrItem(thing.SplitOff(transferable.CountToTransfer), true);
                            transferable.AdjustTo(0);
                            transferable.things.Add(thing);
                        }
                    }

                Close();
            }

            if (Widgets.ButtonText(new Rect(0f, rect2.y, BottomButtonSize.x, BottomButtonSize.y), "CancelButton".Translate())) Close();

            if (Widgets.ButtonText(new Rect(rect.width / 2f - BottomButtonSize.x, rect2.y, BottomButtonSize.x, BottomButtonSize.y), "ResetButton".Translate()))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                CalculateAndRecacheTransferables();
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            CalculateAndRecacheTransferables();
        }

        private void CalculateAndRecacheTransferables()
        {
            transferables = new List<TransferableOneWay>();
            foreach (var t in outpost.Things)
            {
                var transferableOneWay = TransferableUtility.TransferableMatching(t, transferables, TransferAsOneMode.PodsOrCaravanPacking);
                if (transferableOneWay == null)
                {
                    transferableOneWay = new TransferableOneWay();
                    transferables.Add(transferableOneWay);
                }

                if (transferableOneWay.things.Contains(t))
                {
                    Log.Error("Tried to add the same thing twice to TransferableOneWay: " + t);
                    return;
                }

                transferableOneWay.things.Add(t);
            }

            itemsTransfer = new TransferableOneWayWidget(transferables, outpost.Name, caravan.Name, "FormCaravanColonyThingCountTip".Translate());
        }
    }
}