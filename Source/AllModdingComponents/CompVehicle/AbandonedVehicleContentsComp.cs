using System;

using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld.Planet;
namespace CompVehicle
{
    //WIP
	public class AbandonedVehicleContentsComp : WorldObjectComp, IThingHolder
	{
		public ThingOwner contents;
		private static List<Thing> tmpContents = new List<Thing>();
		private static List<string> tmpContentsStr = new List<string>();

		public AbandonedVehicleContentsComp()
		{
			this.contents = new ThingOwner<Thing>(this);
		}
		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return this.contents;
		}
		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Deep.Look<ThingOwner>(ref this.contents, "contents", new object[]
			{
				this
			});
		}


	}
}
