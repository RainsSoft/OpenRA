#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class PlaneInfo : AircraftInfo, IMoveInfo
	{
		public readonly WAngle MaximumPitch = WAngle.FromDegrees(10);

		public override object Create(ActorInitializer init) { return new Plane(init, this); }
	}

	public class Plane : Aircraft, IResolveOrder, IMove, ITick, ISync
	{
		public readonly PlaneInfo Info;
		[Sync] public WPos RTBPathHash;
		Actor self;
		public bool IsMoving { get { return self.CenterPosition.Z > 0; } set { } }

		public Plane(ActorInitializer init, PlaneInfo info)
			: base(init, info)
		{
			self = init.Self;
			Info = info;
		}

		public override WVec GetRepulsionForce()
		{
			var repulsionForce = base.GetRepulsionForce();
			if (repulsionForce == WVec.Zero)
				return WVec.Zero;

			var currentDir = FlyStep(Facing);
			var length = currentDir.HorizontalLength * repulsionForce.HorizontalLength;
			if (length == 0)
				return WVec.Zero;

			var dot = WVec.Dot(currentDir, repulsionForce) / length;

			// avoid stalling the plane
			return dot >= 0 ? repulsionForce : WVec.Zero;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				var cell = self.World.Map.Clamp(order.TargetLocation);
				var explored = self.Owner.Shroud.IsExplored(cell);

				if (!explored && !Info.MoveIntoShroud)
					return;

				if (!order.Queued)
					UnReserve();

				var target = Target.FromCell(self.World, cell);
				self.SetTargetLine(target, Color.Green);
				self.QueueActivity(order.Queued, new FlyAndContinueWithCirclesWhenIdle(self, target));
			}
			else if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor))
					return;

				if (!order.Queued)
					UnReserve();

				self.SetTargetLine(Target.FromOrder(self.World, order), Color.Green);
				self.QueueActivity(order.Queued, Util.SequenceActivities(new ReturnToBase(self, order.TargetActor), new ResupplyAircraft(self)));
			}
			else if (order.OrderString == "Stop")
			{
				UnReserve();
				self.CancelActivity();
			}
			else if (order.OrderString == "ReturnToBase")
			{
				var airfield = ReturnToBase.ChooseAirfield(self, true);
				if (airfield == null)
					return;

				UnReserve();
				self.CancelActivity();
				self.SetTargetLine(Target.FromActor(airfield), Color.Green);
				self.QueueActivity(new ReturnToBase(self, airfield));
				self.QueueActivity(new ResupplyAircraft(self));
			}
			else
			{
				// Game.Debug("Unreserve due to unhandled order: {0}".F(order.OrderString));
				UnReserve();
			}
		}

		public Activity MoveTo(CPos cell, int nearEnough) { return Util.SequenceActivities(new Fly(self, Target.FromCell(self.World, cell)), new FlyCircle(self)); }
		public Activity MoveTo(CPos cell, Actor ignoredActor) { return Util.SequenceActivities(new Fly(self, Target.FromCell(self.World, cell)), new FlyCircle(self)); }
		public Activity MoveWithinRange(Target target, WDist range) { return Util.SequenceActivities(new Fly(self, target, WDist.Zero, range), new FlyCircle(self)); }
		public Activity MoveWithinRange(Target target, WDist minRange, WDist maxRange)
		{
			return Util.SequenceActivities(new Fly(self, target, minRange, maxRange), new FlyCircle(self));
		}

		public Activity MoveFollow(Actor self, Target target, WDist minRange, WDist maxRange) { return new FlyFollow(self, target, minRange, maxRange); }
		public CPos NearestMoveableCell(CPos cell) { return cell; }

		public Activity MoveIntoWorld(Actor self, CPos cell, SubCell subCell = SubCell.Any) { return new Fly(self, Target.FromCell(self.World, cell)); }
		public Activity VisualMove(Actor self, WPos fromPos, WPos toPos)
		{
			return Util.SequenceActivities(new CallFunc(() => SetVisualPosition(self, fromPos)), new Fly(self, Target.FromPos(toPos)));
		}

		public Activity MoveToTarget(Actor self, Target target) { return new Fly(self, target, WDist.FromCells(3), WDist.FromCells(5)); }
		public Activity MoveIntoTarget(Actor self, Target target) { return new Land(self, target); }
	}
}
