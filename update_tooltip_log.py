from pathlib import Path

path = Path("OpenRA.Mods.CA/Widgets/Logic/Ingame/ProductionTooltipLogicCA.cs")
text = path.read_text()
old = "\t\t\t\tvar tooltipExtrasInfos = resolvedActor.TraitInfos<TooltipExtrasInfo>().ToArray();\r\n\t\t\t\tvar tooltipExtras = tooltipExtrasInfos.FirstOrDefault(info => info.IsStandard) ?? tooltipExtrasInfos.FirstOrDefault();\r\n\r\n\t\t\t\tif (\"" + "" + "tooltipExtras != null)"
