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
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Actors with the \"ClonesProducedUnits\" trait will produce a free duplicate of me.")]
	public class CloneableInfo : TraitInfo<Cloneable>
	{
		[FieldLoader.Require]
		[Desc("This unit's cloneable type is:")]
		public readonly string[] Types = { };
	}

	public class Cloneable { }
}
