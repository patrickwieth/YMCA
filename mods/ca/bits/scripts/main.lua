WorldLoaded = function()
  WorldLoadedCommanderTree()
  WorldLoadedDropship()
  if (Player.GetPlayer("Multi0").HasPrerequisites({"tow"}))
  then
    WorldLoadedToW()
  end
end

Tick = function()
	TickGeneralsPowers()
  TickDropship()
  if (Player.GetPlayer("Multi0").HasPrerequisites({"tow"}))
  then
    TickTugOfWar()
  end
end
