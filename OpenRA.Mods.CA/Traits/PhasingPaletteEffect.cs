#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
 public class PhasingPaletteEffectInfo : TraitInfo
 {
  [Desc("The palette to apply this effect to.")]
  [PaletteReference]
  [FieldLoader.Require]
  public readonly string PaletteName;

  [Desc("The name of the palette to base off.")]
  public readonly string BasePalette = "playerts";

  [Desc("Remap these indices to player colors.")]
  public readonly int[] RemapIndex = Array.Empty<int>();

  [Desc("Start colour.")]
  [FieldLoader.Require]
  public readonly Color StartColor;

  [Desc("End colour.")]
  [FieldLoader.Require]
  public readonly Color EndColor;

  [Desc("Number of ticks to pulse before reversing.")]
  public readonly int PulseDuration = 25;

  [Desc("Number of ticks to wait on start/end colours.")]
  public readonly int PulseDelay = 0;

  public readonly int? ShadowIndex = 4;

  [Desc("Allow palette modifiers to change the palette.")]
  public readonly bool AllowModifiers = true;

  public override object Create(ActorInitializer init) { return new PhasingPaletteEffect(this); }
 }

 public class PhasingPaletteEffect : IPaletteModifier, ITick, ILoadsPlayerPalettes
 {
  readonly PhasingPaletteEffectInfo info;

  int ticks;
  bool incrementing;

  //int redDiffStart;
  //int greenDiffStart;
  //int blueDiffStart;

  int redDiffEnd;
  int greenDiffEnd;
  int blueDiffEnd;
  float alphaDiffEnd;

  Dictionary<string, MutablePalette[]> MasterPalette = new Dictionary<string, MutablePalette[]>();
  List<string> playernames = new List<string>();
  //PlayerColorRemap remap;

  void ILoadsPlayerPalettes.LoadPlayerPalettes(WorldRenderer wr, string playerName, Color color, bool replaceExisting)
  {
    var basePalette = wr.Palette(info.BasePalette + playerName).Palette;

    MasterPalette[playerName] = new MutablePalette[info.PulseDuration+1];

    for (int pulseTick = 0; pulseTick <= info.PulseDuration; pulseTick++)
    {
      MasterPalette[playerName][pulseTick] = new MutablePalette(basePalette);

      for (int j = 1; j < Palette.Size; j++)
      {
        var origColor = Color.FromArgb((int)basePalette[j]);

        alphaDiffEnd = info.EndColor.A / 255f;
        redDiffEnd = (info.EndColor.R - origColor.R) / info.PulseDuration;
        greenDiffEnd = (info.EndColor.G - origColor.G) / info.PulseDuration;
        blueDiffEnd = (info.EndColor.B - origColor.B) / info.PulseDuration;

        var red = (origColor.R + (redDiffEnd*alphaDiffEnd * pulseTick)).Clamp(0, 255);
        var green = (origColor.G + (greenDiffEnd*alphaDiffEnd * pulseTick)).Clamp(0, 255);
        var blue = (origColor.B + (blueDiffEnd*alphaDiffEnd * pulseTick)).Clamp(0, 255);

        var colorA = Color.FromLinear((byte)255, red / 255f, green / 255f, blue / 255f);

        MasterPalette[playerName][pulseTick].SetColor(j, colorA);

        if (info.ShadowIndex != null && info.ShadowIndex == j)
        MasterPalette[playerName][pulseTick].SetColor(j, origColor);
      }
    }
    var finalPalette =  new ImmutablePalette(MasterPalette[playerName][0]);

    wr.AddPalette(info.PaletteName + playerName, finalPalette, info.AllowModifiers, replaceExisting);
    playernames.Add(playerName);
  }

  public PhasingPaletteEffect(PhasingPaletteEffectInfo info)
  {
   this.info = info;
   ticks = 0;
   incrementing = true;
  }

  public void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> b)
  {
   var pulseTick = (ticks - info.PulseDelay).Clamp(0, info.PulseDuration);

   foreach(var playerName in playernames) {
     var p = b[info.PaletteName + playerName];

     for (int j = 1; j < Palette.Size; j++)
      {
        if (info.ShadowIndex != null && info.ShadowIndex == j)
         continue;

        p.SetColor(j, MasterPalette[playerName][pulseTick].GetColor(j));
      }
   }
  }

  void ITick.Tick(Actor self)
  {
   if (incrementing)
    ticks++;
   else
    ticks--;

   if (ticks == info.PulseDuration + (info.PulseDelay * 2) || ticks == 0)
    incrementing = !incrementing;
  }
 }
}
