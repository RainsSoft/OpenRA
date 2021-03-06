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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public sealed class PaletteReference
	{
		readonly float index;
		readonly HardwarePalette hardwarePalette;

		public readonly string Name;
		public IPalette Palette { get; internal set; }
		public float TextureIndex { get { return index / hardwarePalette.Height; } }
		public float TextureMidIndex { get { return (index + 0.5f) / hardwarePalette.Height; } }

		public PaletteReference(string name, int index, IPalette palette, HardwarePalette hardwarePalette)
		{
			Name = name;
			Palette = palette;
			this.index = index;
			this.hardwarePalette = hardwarePalette;
		}
	}
}
