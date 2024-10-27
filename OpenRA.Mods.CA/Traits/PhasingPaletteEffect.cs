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

  [Desc("Phasing start colour.")]
  public readonly Color StartColor = Color.Transparent;

  [Desc("Phasing end colour.")]
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

  float redDiff;
  float greenDiff;
  float blueDiff;
  float alpha;

  Dictionary<string, MutablePalette[]> MasterPalette = new Dictionary<string, MutablePalette[]>();
  List<string> playernames = new List<string>();

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

        if (info.ShadowIndex != null && info.ShadowIndex == j)
        {
          MasterPalette[playerName][pulseTick].SetColor(j, origColor);
          continue;
        }

        // mix in the start color
        origColor = Color.FromArgb(
          (int) ( (float) origColor.R * (1 - info.StartColor.A/255f) + (float) info.StartColor.R * info.StartColor.A/255f),
          (int) ( (float) origColor.G * (1 - info.StartColor.A/255f) + (float) info.StartColor.G * info.StartColor.A/255f),
          (int) ( (float) origColor.B * (1 - info.StartColor.A/255f) + (float) info.StartColor.B * info.StartColor.A/255f)
        );

        // mix in the end color
        alpha = info.EndColor.A/255f;
        redDiff = (info.EndColor.R - origColor.R)*alpha / info.PulseDuration;
        greenDiff = (info.EndColor.G - origColor.G)*alpha / info.PulseDuration;
        blueDiff = (info.EndColor.B - origColor.B)*alpha / info.PulseDuration;

        int R = (int)(origColor.R + (redDiff * pulseTick)).Clamp(0, 255);
        int G = (int)(origColor.G + (greenDiff * pulseTick)).Clamp(0, 255);
        int B = (int)(origColor.B + (blueDiff * pulseTick)).Clamp(0, 255);

        var colorA = Color.FromArgb((byte)255, R, G, B);

        MasterPalette[playerName][pulseTick].SetColor(j, colorA);
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
